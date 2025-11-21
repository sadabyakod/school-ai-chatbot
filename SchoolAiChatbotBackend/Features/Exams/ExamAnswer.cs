using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolAiChatbotBackend.Features.Exams
{
    /// <summary>
    /// Represents a student's answer to a question in an exam attempt
    /// </summary>
    [Table("ExamAnswers")]
    public class ExamAnswer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ExamAttemptId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        public int? SelectedOptionId { get; set; }

        [Required]
        public bool IsCorrect { get; set; }

        public int? TimeTakenSeconds { get; set; }

        // Navigation properties
        [ForeignKey("ExamAttemptId")]
        public virtual ExamAttempt ExamAttempt { get; set; } = null!;

        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; } = null!;
    }
}
