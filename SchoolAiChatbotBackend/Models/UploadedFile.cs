using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolAiChatbotBackend.Models
{
    /// <summary>
    /// Uploaded file model - matches Azure Functions UploadedFiles table
    /// Created by Azure Functions blob trigger
    /// </summary>
    [Table("UploadedFiles")]
    public class UploadedFile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string BlobUrl { get; set; } = string.Empty;

        [Required]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [StringLength(200)]
        public string? UploadedBy { get; set; }

        [StringLength(100)]
        public string? Subject { get; set; }

        [StringLength(50)]
        public string? Grade { get; set; }

        [StringLength(50)]
        public string? Medium { get; set; }

        [StringLength(200)]
        public string? Chapter { get; set; }

        public int TotalChunks { get; set; } = 0;

        [StringLength(50)]
        public string Status { get; set; } = "Pending";
    }
}
