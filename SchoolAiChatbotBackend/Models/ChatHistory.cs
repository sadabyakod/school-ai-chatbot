using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolAiChatbotBackend.Models
{
    /// <summary>
    /// SQL-backed chat history for persistent conversation tracking
    /// Replaces in-memory ConversationMemory from ChatController
    /// </summary>
    public class ChatHistory
    {
        public int Id { get; set; }
        
        /// <summary>
        /// User identifier (can be IP address or authenticated user ID)
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Session/conversation identifier for grouping related messages
        /// </summary>
        public string SessionId { get; set; } = string.Empty;
        
        /// <summary>
        /// User's question or message
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// AI-generated response
        /// </summary>
        public string Reply { get; set; } = string.Empty;
        
        /// <summary>
        /// When the exchange occurred
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Serialized JSON of syllabus chunks used for context (optional)
        /// </summary>
        public string? ContextUsed { get; set; }
        
        /// <summary>
        /// Number of context chunks retrieved from RAG
        /// </summary>
        public int ContextCount { get; set; }
        
        /// <summary>
        /// Foreign key to User table (nullable for anonymous users)
        /// </summary>
        public int? AuthenticatedUserId { get; set; }
        public User? User { get; set; }

        /// <summary>
        /// Optional tag for the session (e.g., "Math Homework", "Exam Prep")
        /// </summary>
        [MaxLength(100)]
        public string? Tag { get; set; }
    }
}
