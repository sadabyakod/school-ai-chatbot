using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolAiChatbotBackend.Models
{
    /// <summary>
    /// Database entity for storing generated exams.
    /// The exam content is stored as JSON to preserve the full structure.
    /// </summary>
    [Table("GeneratedExams")]
    public class GeneratedExam
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Unique exam identifier (e.g., "Karnataka_2nd_PUC_Math_Model_Paper_2024-25")
        /// </summary>
        [Required]
        [StringLength(200)]
        public string ExamId { get; set; } = string.Empty;

        /// <summary>
        /// Subject of the exam (e.g., "Mathematics")
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Grade level (e.g., "2nd PUC")
        /// </summary>
        [StringLength(50)]
        public string? Grade { get; set; }

        /// <summary>
        /// Chapter or topic (e.g., "Matrices", "Full Syllabus")
        /// </summary>
        [StringLength(200)]
        public string? Chapter { get; set; }

        /// <summary>
        /// Difficulty level (e.g., "Easy", "Medium", "Hard")
        /// </summary>
        [StringLength(50)]
        public string? Difficulty { get; set; }

        /// <summary>
        /// Total marks for the exam
        /// </summary>
        public int TotalMarks { get; set; }

        /// <summary>
        /// Duration in minutes
        /// </summary>
        public int DurationMinutes { get; set; }

        /// <summary>
        /// Full exam content stored as JSON
        /// Includes all questions, options, answers, etc.
        /// </summary>
        [Required]
        public string ExamContentJson { get; set; } = string.Empty;

        /// <summary>
        /// When the exam was generated
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional: Who generated the exam (student ID, teacher ID, etc.)
        /// </summary>
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Whether this exam is still active/available
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
