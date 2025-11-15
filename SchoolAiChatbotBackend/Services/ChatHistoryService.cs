using Microsoft.EntityFrameworkCore;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// SQL-backed chat history service
    /// Replaces in-memory ConversationMemory from Azure Functions migration
    /// </summary>
    public interface IChatHistoryService
    {
        Task<ChatHistory> SaveChatHistoryAsync(string userId, string sessionId, string message, string reply, string? contextUsed, int contextCount);
        Task<List<ChatHistory>> GetChatHistoryBySessionAsync(string userId, string sessionId, int limit = 10);
        Task<List<string>> GetUserChatSessionsAsync(string userId, int limit = 20);
        Task<int> DeleteOldHistoryAsync(TimeSpan olderThan);
        Task<ChatHistory?> GetLastMessageAsync(string userId, string sessionId);
    }

    public class ChatHistoryService : IChatHistoryService
    {
        private readonly AppDbContext _dbContext;

        public ChatHistoryService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Save a chat exchange to the database
        /// </summary>
        public async Task<ChatHistory> SaveChatHistoryAsync(
            string userId, 
            string sessionId, 
            string message, 
            string reply, 
            string? contextUsed, 
            int contextCount)
        {
            var chatHistory = new ChatHistory
            {
                UserId = userId,
                SessionId = sessionId,
                Message = message,
                Reply = reply,
                ContextUsed = contextUsed,
                ContextCount = contextCount,
                Timestamp = DateTime.UtcNow
            };

            _dbContext.ChatHistories.Add(chatHistory);
            await _dbContext.SaveChangesAsync();

            return chatHistory;
        }

        /// <summary>
        /// Retrieve chat history for a specific session (most recent first)
        /// </summary>
        public async Task<List<ChatHistory>> GetChatHistoryBySessionAsync(
            string userId, 
            string sessionId, 
            int limit = 10)
        {
            return await _dbContext.ChatHistories
                .Where(c => c.UserId == userId && c.SessionId == sessionId)
                .OrderByDescending(c => c.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Get all session IDs for a user (most recent first)
        /// </summary>
        public async Task<List<string>> GetUserChatSessionsAsync(string userId, int limit = 20)
        {
            return await _dbContext.ChatHistories
                .Where(c => c.UserId == userId)
                .Select(c => c.SessionId)
                .Distinct()
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Delete chat history older than specified time span
        /// Returns number of records deleted
        /// </summary>
        public async Task<int> DeleteOldHistoryAsync(TimeSpan olderThan)
        {
            var cutoffDate = DateTime.UtcNow - olderThan;
            var oldRecords = await _dbContext.ChatHistories
                .Where(c => c.Timestamp < cutoffDate)
                .ToListAsync();

            _dbContext.ChatHistories.RemoveRange(oldRecords);
            await _dbContext.SaveChangesAsync();

            return oldRecords.Count;
        }

        /// <summary>
        /// Get the most recent message in a session (for context continuation)
        /// </summary>
        public async Task<ChatHistory?> GetLastMessageAsync(string userId, string sessionId)
        {
            return await _dbContext.ChatHistories
                .Where(c => c.UserId == userId && c.SessionId == sessionId)
                .OrderByDescending(c => c.Timestamp)
                .FirstOrDefaultAsync();
        }
    }
}
