using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolAiChatbotBackend.Models
{
    /// <summary>
    /// Entity representing a stored marking rubric reference for a subjective question.
    /// Each subjective question in an exam has its own rubric stored in blob storage.
    /// 
    /// IMPORTANT: The RubricBlobPath is the ONLY source of truth for rubric content.
    /// During evaluation, ALWAYS load the frozen rubric from blob storage.
    /// 
    /// DEPRECATION NOTICE:
    /// - StepsJson, ModelAnswer, QuestionText are DEPRECATED and kept only for backward compatibility.
    /// - New code should NOT use these fields. Use GetFrozenRubricFromBlobAsync() instead.
    /// - These fields will be removed in a future migration.
    /// </summary>
    [Table("SubjectiveRubrics")]
    public class SubjectiveRubric
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The exam this rubric belongs to
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ExamId { get; set; } = string.Empty;

        /// <summary>
        /// The question ID within the exam (e.g., "B1", "C2", "D3")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string QuestionId { get; set; } = string.Empty;

        /// <summary>
        /// Total marks for this question (sum of all step marks)
        /// </summary>
        public int TotalMarks { get; set; }

        /// <summary>
        /// [DEPRECATED] JSON serialized List of StepRubricItem.
        /// DO NOT USE FOR EVALUATION - use RubricBlobPath instead.
        /// Kept for backward compatibility only.
        /// </summary>
        [Required]
        [Obsolete("Use RubricBlobPath and GetFrozenRubricFromBlobAsync() for evaluation")]
        public string StepsJson { get; set; } = "[]";

        /// <summary>
        /// When the rubric was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// [DEPRECATED] The expected/model answer.
        /// DO NOT USE - this is a security risk (solution leakage).
        /// Kept for backward compatibility only.
        /// </summary>
        [Obsolete("Use RubricBlobPath - storing model answer in SQL is a security risk")]
        public string? ModelAnswer { get; set; }

        /// <summary>
        /// [DEPRECATED] Question text for reference.
        /// Use RubricBlobPath instead.
        /// </summary>
        [Obsolete("Use RubricBlobPath and GetFrozenRubricFromBlobAsync()")]
        public string? QuestionText { get; set; }

        /// <summary>
        /// Path/URL to the frozen rubric JSON stored in blob storage (modalquestions-rubrics container).
        /// THIS IS THE CANONICAL SOURCE OF TRUTH FOR EVALUATION.
        /// Format: paper-{examId}/question-{questionId}.json
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string RubricBlobPath { get; set; } = string.Empty;
    }
}
