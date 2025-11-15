using System;

namespace SchoolAiChatbotBackend.Models
{
    /// <summary>
    /// Generated study notes from syllabus content using AI
    /// Migrated from Azure Functions GenerateStudyNotes feature
    /// </summary>
    public class StudyNote
    {
        public int Id { get; set; }
        
        /// <summary>
        /// User identifier who requested the notes
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Topic or subject of the study notes
        /// </summary>
        public string Topic { get; set; } = string.Empty;
        
        /// <summary>
        /// AI-generated study notes content (markdown format)
        /// </summary>
        public string GeneratedNotes { get; set; } = string.Empty;
        
        /// <summary>
        /// Serialized JSON array of source chunk IDs used to generate notes
        /// </summary>
        public string? SourceChunks { get; set; }
        
        /// <summary>
        /// When the notes were generated
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Subject/grade/chapter metadata
        /// </summary>
        public string? Subject { get; set; }
        public string? Grade { get; set; }
        public string? Chapter { get; set; }
        
        /// <summary>
        /// Foreign key to authenticated user (nullable)
        /// </summary>
        public int? AuthenticatedUserId { get; set; }
        public User? User { get; set; }
        
        /// <summary>
        /// Optional user rating/feedback (1-5 stars)
        /// </summary>
        public int? Rating { get; set; }
    }
}
