using System;
using System.Collections.Generic;

namespace SchoolAiChatbotBackend.Models
{
    /// <summary>
    /// Represents extracted MCQ answers from uploaded answer sheets
    /// </summary>
    public class McqExtraction
    {
        public string McqExtractionId { get; set; } = Guid.NewGuid().ToString();
        public string WrittenSubmissionId { get; set; } = string.Empty;
        public string ExamId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public List<ExtractedMcqAnswer> ExtractedAnswers { get; set; } = new();
        public ExtractionStatus Status { get; set; } = ExtractionStatus.Pending;
        public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
        public string? RawOcrText { get; set; }
    }

    /// <summary>
    /// Represents a single extracted MCQ answer
    /// </summary>
    public class ExtractedMcqAnswer
    {
        public int QuestionNumber { get; set; }
        public string ExtractedOption { get; set; } = string.Empty;
        public double Confidence { get; set; } = 0.0;
    }

    /// <summary>
    /// Status of MCQ extraction process
    /// </summary>
    public enum ExtractionStatus
    {
        Pending,
        Processing,
        Completed,
        Failed
    }

    /// <summary>
    /// Result of MCQ answer matching and evaluation
    /// </summary>
    public class McqEvaluationFromSheet
    {
        public string McqEvaluationId { get; set; } = Guid.NewGuid().ToString();
        public string McqExtractionId { get; set; } = string.Empty;
        public string ExamId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public List<McqAnswerEvaluation> Evaluations { get; set; } = new();
        public int TotalScore { get; set; }
        public int TotalMarks { get; set; }
        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Evaluation result for a single MCQ question from answer sheet
    /// </summary>
    public class McqAnswerEvaluation
    {
        public string QuestionId { get; set; } = string.Empty;
        public int QuestionNumber { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public string CorrectAnswer { get; set; } = string.Empty;
        public string StudentAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int Marks { get; set; }
        public int MarksAwarded { get; set; }
        public bool WasExtracted { get; set; } // True if extracted from sheet, false if not found
    }
}
