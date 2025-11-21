using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolAiChatbotBackend.Features.Exams
{
    /// <summary>
    /// Defines an exam template that can be used to create exam attempts
    /// </summary>
    [Table("ExamTemplates")]
    public class ExamTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(300)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Chapter { get; set; }

        [Required]
        public int TotalQuestions { get; set; }

        [Required]
        public int DurationMinutes { get; set; }

        /// <summary>
        /// If true, exam adapts difficulty based on student performance
        /// </summary>
        public bool AdaptiveEnabled { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(200)]
        public string? CreatedBy { get; set; }

        // Navigation property
        public virtual ICollection<ExamAttempt> ExamAttempts { get; set; } = new List<ExamAttempt>();
    }
}
