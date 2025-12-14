using Microsoft.EntityFrameworkCore;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// Study Notes Generation Service
    /// Uses SQL-based RAG to generate comprehensive study notes
    /// Migrated from Azure Functions GenerateStudyNotes feature
    /// </summary>
    public interface IStudyNotesService
    {
        Task<StudyNote> GenerateStudyNotesAsync(string userId, string topic, string? subject = null, string? grade = null, string? chapter = null);
        Task<List<StudyNote>> GetUserStudyNotesAsync(string userId, int limit = 20);
        Task<StudyNote?> GetStudyNoteByIdAsync(int id);
        Task<bool> RateStudyNoteAsync(int id, int rating);
        Task<StudyNote?> UpdateStudyNoteAsync(int id, string userId, string updatedContent);
        Task<string?> ShareStudyNoteAsync(int id, string userId);
        Task<bool> UnshareStudyNoteAsync(int id, string userId);
        Task<StudyNote?> GetSharedStudyNoteAsync(string shareToken);
    }

    public class StudyNotesService : IStudyNotesService
    {
        private readonly IRAGService _ragService;
        private readonly IOpenAIService _openAIService;
        private readonly AppDbContext _dbContext;
        private readonly Microsoft.Extensions.Logging.ILogger<StudyNotesService> _logger;

        public StudyNotesService(
            IRAGService ragService,
            IOpenAIService openAIService,
            AppDbContext dbContext,
            Microsoft.Extensions.Logging.ILogger<StudyNotesService> logger)
        {
            _ragService = ragService;
            _openAIService = openAIService;
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Generate comprehensive study notes using SQL-based RAG and AI
        /// </summary>
        public async Task<StudyNote> GenerateStudyNotesAsync(
            string userId,
            string topic,
            string? subject = null,
            string? grade = null,
            string? chapter = null)
        {
            try
            {
                _logger.LogInformation("Generating study notes for topic: {Topic}, subject: {Subject}, grade: {Grade}",
                    topic, subject ?? "any", grade ?? "any");

                // Step 1: Search for relevant content using SQL-based RAG
                var relevantChunks = await _ragService.FindRelevantChunksAsync(
                    topic,
                    topK: 10,
                    subject: subject,
                    grade: grade
                );

                // Step 2: Further filter by chapter if provided
                if (!string.IsNullOrWhiteSpace(chapter))
                {
                    relevantChunks = relevantChunks
                        .Where(c => c.Chapter != null && c.Chapter.Contains(chapter, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Step 3: Build context from chunks
                var contextText = await _ragService.BuildContextTextAsync(relevantChunks);

                // Step 4: Create AI prompt for study notes generation
                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine("### ROLE: You are an expert educational content creator specializing in study materials.");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("### TASK: Create comprehensive, well-structured study notes in markdown format.");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("### REQUIREMENTS:");
                promptBuilder.AppendLine("- Use clear headings (##) and subheadings (###)");
                promptBuilder.AppendLine("- Include key concepts, definitions, and explanations");
                promptBuilder.AppendLine("- Add bullet points and numbered lists for easy reading");
                promptBuilder.AppendLine("- Highlight important terms using **bold** or *italic*");
                promptBuilder.AppendLine("- Include examples where applicable");
                promptBuilder.AppendLine("- Add mnemonics or memory aids when helpful");
                promptBuilder.AppendLine("- End with practice questions or review points");
                promptBuilder.AppendLine("- Make it engaging and student-friendly");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("### TOPIC:");
                promptBuilder.AppendLine(topic);

                if (!string.IsNullOrWhiteSpace(subject))
                    promptBuilder.AppendLine($"Subject: {subject}");
                if (!string.IsNullOrWhiteSpace(grade))
                    promptBuilder.AppendLine($"Grade Level: {grade}");
                if (!string.IsNullOrWhiteSpace(chapter))
                    promptBuilder.AppendLine($"Chapter: {chapter}");

                promptBuilder.AppendLine();
                promptBuilder.AppendLine("### SOURCE CONTENT:");
                promptBuilder.AppendLine(contextText);
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("### GENERATED STUDY NOTES:");

                var prompt = promptBuilder.ToString();

                // Step 5: Generate study notes using AI
                var generatedNotes = await _openAIService.GetChatCompletionAsync(prompt);

                // Step 6: Save to database
                var sourceChunksJson = JsonSerializer.Serialize(
                    relevantChunks.Select(c => new
                    {
                        c.Id,
                        c.Subject,
                        c.Grade,
                        c.Chapter,
                        c.ChunkIndex,
                        Preview = c.ChunkText.Length > 100 ? c.ChunkText.Substring(0, 100) + "..." : c.ChunkText
                    }).ToList()
                );

                var studyNote = new StudyNote
                {
                    UserId = userId,
                    Topic = topic,
                    GeneratedNotes = generatedNotes,
                    SourceChunks = sourceChunksJson,
                    Subject = subject,
                    Grade = grade,
                    Chapter = chapter,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.StudyNotes.Add(studyNote);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully generated and saved study note {NoteId} for topic: {Topic}",
                    studyNote.Id, topic);

                return studyNote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating study notes for topic: {Topic}", topic);
                throw;
            }
        }

        /// <summary>
        /// Retrieve study notes for a user (most recent first)
        /// </summary>
        public async Task<List<StudyNote>> GetUserStudyNotesAsync(string userId, int limit = 20)
        {
            return await _dbContext.StudyNotes
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Get a specific study note by ID
        /// </summary>
        public async Task<StudyNote?> GetStudyNoteByIdAsync(int id)
        {
            return await _dbContext.StudyNotes.FindAsync(id);
        }

        /// <summary>
        /// Rate a study note (1-5 stars)
        /// </summary>
        public async Task<bool> RateStudyNoteAsync(int id, int rating)
        {
            if (rating < 1 || rating > 5)
                return false;

            var note = await _dbContext.StudyNotes.FindAsync(id);
            if (note == null)
                return false;

            note.Rating = rating;
            await _dbContext.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Update/edit study note content
        /// </summary>
        public async Task<StudyNote?> UpdateStudyNoteAsync(int id, string userId, string updatedContent)
        {
            try
            {
                var note = await _dbContext.StudyNotes.FindAsync(id);

                if (note == null || note.UserId != userId)
                {
                    _logger.LogWarning("Study note {NoteId} not found or user {UserId} not authorized", id, userId);
                    return null;
                }

                note.GeneratedNotes = updatedContent;
                note.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully updated study note {NoteId}", id);
                return note;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating study note {NoteId}", id);
                throw;
            }
        }

        /// <summary>
        /// Share a study note and generate shareable token
        /// </summary>
        public async Task<string?> ShareStudyNoteAsync(int id, string userId)
        {
            try
            {
                var note = await _dbContext.StudyNotes.FindAsync(id);

                if (note == null || note.UserId != userId)
                {
                    _logger.LogWarning("Study note {NoteId} not found or user {UserId} not authorized", id, userId);
                    return null;
                }

                // Generate unique share token if not already shared
                if (string.IsNullOrEmpty(note.ShareToken))
                {
                    note.ShareToken = Guid.NewGuid().ToString("N");
                }

                note.IsShared = true;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully shared study note {NoteId} with token {Token}", id, note.ShareToken);
                return note.ShareToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sharing study note {NoteId}", id);
                throw;
            }
        }

        /// <summary>
        /// Unshare a study note (revoke public access)
        /// </summary>
        public async Task<bool> UnshareStudyNoteAsync(int id, string userId)
        {
            try
            {
                var note = await _dbContext.StudyNotes.FindAsync(id);

                if (note == null || note.UserId != userId)
                {
                    _logger.LogWarning("Study note {NoteId} not found or user {UserId} not authorized", id, userId);
                    return false;
                }

                note.IsShared = false;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully unshared study note {NoteId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsharing study note {NoteId}", id);
                throw;
            }
        }

        /// <summary>
        /// Get a shared study note by its share token (public access)
        /// </summary>
        public async Task<StudyNote?> GetSharedStudyNoteAsync(string shareToken)
        {
            try
            {
                var note = await _dbContext.StudyNotes
                    .FirstOrDefaultAsync(n => n.ShareToken == shareToken && n.IsShared);

                if (note == null)
                {
                    _logger.LogWarning("Shared study note with token {Token} not found", shareToken);
                }

                return note;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shared study note with token {Token}", shareToken);
                throw;
            }
        }
    }
}
