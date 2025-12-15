using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolAiChatbotBackend.Models
{
    /// <summary>
    /// Per-question evaluation result stored in WrittenQuestionEvaluations table
    /// Matches SmartStudyFunc Azure Function schema exactly
    /// </summary>
    [Table("WrittenQuestionEvaluations")]
    public class WrittenQuestionEvaluation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid WrittenSubmissionId { get; set; }

        [Required]
        [MaxLength(100)]
        public string QuestionId { get; set; } = string.Empty;

        public int QuestionNumber { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string ExtractedAnswer { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string ModelAnswer { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal MaxScore { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal AwardedScore { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Feedback { get; set; } = string.Empty;

        /// <summary>
        /// JSON containing: KeywordsMatched, MissingKeywords, Strengths, StepWiseBreakdown
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? RubricBreakdown { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Strengths { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Improvements { get; set; }

        [Column(TypeName = "decimal(5,4)")]
        public decimal? Confidence { get; set; }

        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("WrittenSubmissionId")]
        public virtual WrittenSubmission? WrittenSubmission { get; set; }
    }
}
