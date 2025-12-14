using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SchoolAiChatbotBackend.Models
{
    /// <summary>
    /// Result of AI evaluation for a single subjective question
    /// </summary>
    public class SubjectiveEvaluationResult
    {
        public string EvaluationId { get; set; } = Guid.NewGuid().ToString();
        public string WrittenSubmissionId { get; set; } = string.Empty;
        public string QuestionId { get; set; } = string.Empty;
        public int QuestionNumber { get; set; }

        [JsonPropertyName("earnedMarks")]
        public double EarnedMarks { get; set; }

        [JsonPropertyName("maxMarks")]
        public double MaxMarks { get; set; }

        [JsonPropertyName("isFullyCorrect")]
        public bool IsFullyCorrect { get; set; }

        [JsonPropertyName("expectedAnswer")]
        public string ExpectedAnswer { get; set; } = string.Empty;

        [JsonPropertyName("studentAnswerEcho")]
        public string StudentAnswerEcho { get; set; } = string.Empty;

        [JsonPropertyName("stepAnalysis")]
        public List<StepAnalysis> StepAnalysis { get; set; } = new();

        [JsonPropertyName("overallFeedback")]
        public string OverallFeedback { get; set; } = string.Empty;

        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
    }

    public class StepAnalysis
    {
        [JsonPropertyName("step")]
        public int Step { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("isCorrect")]
        public bool IsCorrect { get; set; }

        [JsonPropertyName("marksAwarded")]
        public double MarksAwarded { get; set; }

        [JsonPropertyName("maxMarksForStep")]
        public double MaxMarksForStep { get; set; }

        [JsonPropertyName("feedback")]
        public string Feedback { get; set; } = string.Empty;
    }
}
