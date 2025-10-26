using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolAiChatbotBackend.Data
{
    public class UploadedFile
    {
        [Key]
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public int EmbeddingDimension { get; set; }
        public string EmbeddingVector { get; set; } = string.Empty; // Store as JSON string or comma-separated floats
    }
}
