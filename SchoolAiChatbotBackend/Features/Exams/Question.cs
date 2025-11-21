using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolAiChatbotBackend.Features.Exams
{
    /// <summary>
    /// Represents a question that can be used in exams
    /// </summary>
    [Table("Questions")]
    public class Question
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Chapter { get; set; }

        [StringLength(300)]
        public string? Topic { get; set; }

        [Required]
        [StringLength(2000)]
        public string Text { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Explanation { get; set; }

        /// <summary>
        /// Difficulty level: Easy, Medium, Hard
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Difficulty { get; set; } = "Medium";

        /// <summary>
        /// Question type: MultipleChoice, TrueFalse, ShortAnswer
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "MultipleChoice";

        /// <summary>
        /// Reference to source file if question was generated from uploaded content
        /// </summary>
        public int? SourceFileId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
        public virtual ICollection<ExamAnswer> ExamAnswers { get; set; } = new List<ExamAnswer>();
    }
}
