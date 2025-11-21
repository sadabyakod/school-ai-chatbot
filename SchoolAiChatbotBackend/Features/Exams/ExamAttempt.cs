using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolAiChatbotBackend.Features.Exams
{
    /// <summary>
    /// Represents a student's attempt at an exam
    /// </summary>
    [Table("ExamAttempts")]
    public class ExamAttempt
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        public int ExamTemplateId { get; set; }

        [Required]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? ScorePercent { get; set; }

        public int? CorrectCount { get; set; }

        public int? WrongCount { get; set; }

        /// <summary>
        /// Status: InProgress, Completed, Abandoned
        /// </summary>
        [StringLength(50)]
        public string Status { get; set; } = "InProgress";

        // Navigation properties
        [ForeignKey("ExamTemplateId")]
        public virtual ExamTemplate ExamTemplate { get; set; } = null!;

        public virtual ICollection<ExamAnswer> ExamAnswers { get; set; } = new List<ExamAnswer>();
    }
}
