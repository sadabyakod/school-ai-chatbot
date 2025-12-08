using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolAiChatbotBackend.Models
{
    /// <summary>
    /// Entity representing a stored marking rubric for a subjective question.
    /// Each subjective question in an exam has its own rubric with steps and marks.
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
        /// JSON serialized List of StepRubricItem
        /// Example: [{"StepNumber":1,"Description":"Identify formula","Marks":1},...]
        /// </summary>
        [Required]
        public string StepsJson { get; set; } = "[]";

        /// <summary>
        /// When the rubric was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional: The expected/model answer for reference
        /// </summary>
        public string? ModelAnswer { get; set; }

        /// <summary>
        /// Optional: Question text for reference
        /// </summary>
        public string? QuestionText { get; set; }
    }
}
