using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolAiChatbotBackend.Features.Exams
{
    /// <summary>
    /// Represents an answer option for a multiple choice question
    /// </summary>
    [Table("QuestionOptions")]
    public class QuestionOption
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [Required]
        [StringLength(1000)]
        public string OptionText { get; set; } = string.Empty;

        [Required]
        public bool IsCorrect { get; set; }

        // Navigation property
        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; } = null!;
    }
}
