using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SchoolAiChatbotBackend.DTOs
{
    /// <summary>
    /// Summary information for a single student's submission to an exam
    /// </summary>
    public class ExamSubmissionDto
    {
        public string ExamId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public SubmissionType SubmissionType { get; set; }
        public DateTime? McqSubmittedAt { get; set; }
        public DateTime? WrittenSubmittedAt { get; set; }
        public DateTime LatestSubmissionTime { get; set; }
        public double? TotalScore { get; set; }
        public double? TotalMaxScore { get; set; }
        public double? Percentage { get; set; }
        public SubmissionStatusType Status { get; set; }
    }

    /// <summary>
    /// Detailed information for a single student's submission including all answers
    /// </summary>
    public class ExamSubmissionDetailDto
    {
        public string ExamId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string ExamTitle { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? GradeLevel { get; set; }
        public string? Chapter { get; set; }
        
        // MCQ Details
        public bool HasMcqSubmission { get; set; }
        public DateTime? McqSubmittedAt { get; set; }
        public int? McqScore { get; set; }
        public int? McqTotalMarks { get; set; }
        public List<McqAnswerDetailDto> McqAnswers { get; set; } = new();
        
        // Written Details
        public bool HasWrittenSubmission { get; set; }
        public DateTime? WrittenSubmittedAt { get; set; }
        public string? WrittenSubmissionId { get; set; }
        public SubmissionStatusType WrittenStatus { get; set; }
        public double? SubjectiveScore { get; set; }
        public double? SubjectiveTotalMarks { get; set; }
        public List<SubjectiveEvaluationDetailDto> SubjectiveEvaluations { get; set; } = new();
        
        // Overall
        public double? GrandScore { get; set; }
        public double? GrandTotalMarks { get; set; }
        public double? Percentage { get; set; }
        public string? LetterGrade { get; set; }
        public bool? Passed { get; set; }
    }

    /// <summary>
    /// MCQ answer detail for analytics
    /// </summary>
    public class McqAnswerDetailDto
    {
        public string QuestionId { get; set; } = string.Empty;
        public string SelectedOption { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int MarksAwarded { get; set; }
    }

    /// <summary>
    /// Subjective evaluation detail for analytics
    /// </summary>
    public class SubjectiveEvaluationDetailDto
    {
        public string QuestionId { get; set; } = string.Empty;
        public int QuestionNumber { get; set; }
        public double EarnedMarks { get; set; }
        public double MaxMarks { get; set; }
        public bool IsFullyCorrect { get; set; }
        public string ExpectedAnswer { get; set; } = string.Empty;
        public string StudentAnswerEcho { get; set; } = string.Empty;
        public string OverallFeedback { get; set; } = string.Empty;
        public List<StepAnalysisDto> StepAnalysis { get; set; } = new();
    }

    /// <summary>
    /// Student's exam history entry
    /// </summary>
    public class StudentExamHistoryDto
    {
        public string ExamId { get; set; } = string.Empty;
        public string ExamTitle { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? GradeLevel { get; set; }
        public string? Chapter { get; set; }
        public DateTime AttemptedAt { get; set; }
        public double? Score { get; set; }
        public double? TotalMarks { get; set; }
        public double? Percentage { get; set; }
        public SubmissionStatusType Status { get; set; }
        public SubmissionType SubmissionType { get; set; }
    }

    /// <summary>
    /// Summary statistics for an exam
    /// </summary>
    public class ExamSummaryDto
    {
        public string ExamId { get; set; } = string.Empty;
        public string ExamTitle { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int TotalSubmissions { get; set; }
        public int CompletedSubmissions { get; set; }
        public int PendingEvaluations { get; set; }
        public int PartialSubmissions { get; set; }
        public double? AverageScore { get; set; }
        public double? MinScore { get; set; }
        public double? MaxScore { get; set; }
        public double? AveragePercentage { get; set; }
        public Dictionary<SubmissionStatusType, int> StatusBreakdown { get; set; } = new();
        public Dictionary<SubmissionType, int> SubmissionTypeBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Paginated list response
    /// </summary>
    public class PaginatedListDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }

    /// <summary>
    /// Type of submission
    /// </summary>
    public enum SubmissionType
    {
        None,
        MCQOnly,
        WrittenOnly,
        Both
    }

    /// <summary>
    /// Overall submission status
    /// </summary>
    public enum SubmissionStatusType
    {
        NotStarted,
        PartiallyCompleted,
        PendingEvaluation,
        Evaluating,
        Completed,
        Failed
    }
}
