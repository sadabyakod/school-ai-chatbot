using System;
using System.Collections.Generic;

namespace SchoolAiChatbotBackend.Models
{
    /// <summary>
    /// Represents a student's MCQ submission
    /// </summary>
    public class McqSubmission
    {
        public string McqSubmissionId { get; set; } = Guid.NewGuid().ToString();
        public string ExamId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public List<McqAnswer> Answers { get; set; } = new();
        public int Score { get; set; }
        public int TotalMarks { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }

    public class McqAnswer
    {
        public string QuestionId { get; set; } = string.Empty;
        public string SelectedOption { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int MarksAwarded { get; set; }
    }
}
