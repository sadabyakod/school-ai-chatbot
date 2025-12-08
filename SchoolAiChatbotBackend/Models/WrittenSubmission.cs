using System;
using System.Collections.Generic;

namespace SchoolAiChatbotBackend.Models
{
    /// <summary>
    /// Represents a student's uploaded written answers for subjective questions
    /// </summary>
    public class WrittenSubmission
    {
        public string WrittenSubmissionId { get; set; } = string.Empty;
        public string ExamId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public List<string> FilePaths { get; set; } = new();
        public SubmissionStatus Status { get; set; } = SubmissionStatus.PendingEvaluation;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EvaluatedAt { get; set; }
        public string? OcrText { get; set; }
    }

    public enum SubmissionStatus
    {
        PendingEvaluation,
        OcrProcessing,
        Evaluating,
        Completed,
        Failed
    }
}
