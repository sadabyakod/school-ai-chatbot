using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolAiChatbotBackend.Models
{
    /// <summary>
    /// File chunk model - matches Azure Functions FileChunks table
    /// Created by Azure Functions blob trigger, read by ASP.NET Core for RAG
    /// </summary>
    [Table("FileChunks")]
    public class FileChunk
    {
        [Key]
        public int Id { get; set; }

        public int FileId { get; set; }

        [Required]
        public string ChunkText { get; set; } = string.Empty;

        public int ChunkIndex { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? Subject { get; set; }

        [StringLength(50)]
        public string? Grade { get; set; }

        [StringLength(200)]
        public string? Chapter { get; set; }

        // Navigation property
        [ForeignKey("FileId")]
        public virtual UploadedFile? UploadedFile { get; set; }
    }
}
