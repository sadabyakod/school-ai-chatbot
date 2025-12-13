using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolAiChatbotBackend.Models
{
    /// <summary>
    /// Model Question Paper - stores uploaded question papers by class and subject
    /// </summary>
    [Table("ModelQuestionPapers")]
    public class ModelQuestionPaper
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

        /// <summary>
        /// Subject name (e.g., "Mathematics", "Science", "English")
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Grade/Class (e.g., "10", "12", "PUC-2")
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Grade { get; set; } = string.Empty;

        /// <summary>
        /// Medium of instruction (e.g., "English", "Kannada", "Hindi")
        /// </summary>
        [StringLength(50)]
        public string Medium { get; set; } = "English";

        /// <summary>
        /// Academic year (e.g., "2024-25", "2023-24")
        /// </summary>
        [StringLength(20)]
        public string? AcademicYear { get; set; }

        /// <summary>
        /// Chapter or unit name (optional, for chapter-wise papers)
        /// </summary>
        [StringLength(200)]
        public string? Chapter { get; set; }

        /// <summary>
        /// Type of paper: "Annual", "Quarterly", "Half-Yearly", "Model", "Previous Year"
        /// </summary>
        [StringLength(50)]
        public string PaperType { get; set; } = "Model";

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; set; } = 0;

        /// <summary>
        /// Content type (e.g., "application/pdf")
        /// </summary>
        [StringLength(100)]
        public string? ContentType { get; set; }
    }
}
