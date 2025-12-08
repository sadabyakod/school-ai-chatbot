using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SchoolAiChatbotBackend.DTOs
{
    /// <summary>
    /// Request to submit MCQ answers
    /// </summary>
    public class SubmitMcqRequest
    {
        [Required]
        public string ExamId { get; set; } = string.Empty;
        
        [Required]
        public string StudentId { get; set; } = string.Empty;
        
        [Required]
        public List<McqAnswerDto> Answers { get; set; } = new();
    }

    public class McqAnswerDto
    {
        [Required]
        public string QuestionId { get; set; } = string.Empty;
        
        [Required]
        public string SelectedOption { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response from MCQ submission
    /// </summary>
    public class McqSubmissionResponse
    {
        public string McqSubmissionId { get; set; } = string.Empty;
        public int Score { get; set; }
        public int TotalMarks { get; set; }
        public double Percentage { get; set; }
        public List<McqResultDto> Results { get; set; } = new();
    }

    public class McqResultDto
    {
        public string QuestionId { get; set; } = string.Empty;
        public string SelectedOption { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int MarksAwarded { get; set; }
    }

    /// <summary>
    /// Request to upload written answers
    /// </summary>
    public class UploadWrittenRequest
    {
        [Required]
        public string ExamId { get; set; } = string.Empty;
        
        [Required]
        public string StudentId { get; set; } = string.Empty;
        
        [Required]
        public List<IFormFile> Files { get; set; } = new();
    }

    /// <summary>
    /// Response from written upload
    /// </summary>
    public class UploadWrittenResponse
    {
        public string WrittenSubmissionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Consolidated exam result
    /// </summary>
    public class ConsolidatedExamResult
    {
        public string ExamId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string ExamTitle { get; set; } = string.Empty;
        
        // MCQ Results
        public int McqScore { get; set; }
        public int McqTotalMarks { get; set; }
        public List<McqResultDto> McqResults { get; set; } = new();
        
        // Subjective Results
        public double SubjectiveScore { get; set; }
        public double SubjectiveTotalMarks { get; set; }
        public List<SubjectiveResultDto> SubjectiveResults { get; set; } = new();
        
        // Grand Total
        public double GrandScore { get; set; }
        public double GrandTotalMarks { get; set; }
        public double Percentage { get; set; }
        public string Grade { get; set; } = string.Empty;
        public bool Passed { get; set; }
        
        public string? EvaluatedAt { get; set; }
    }

    public class SubjectiveResultDto
    {
        public string QuestionId { get; set; } = string.Empty;
        public int QuestionNumber { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public double EarnedMarks { get; set; }
        public double MaxMarks { get; set; }
        public bool IsFullyCorrect { get; set; }
        public string ExpectedAnswer { get; set; } = string.Empty;
        public string StudentAnswerEcho { get; set; } = string.Empty;
        public List<StepAnalysisDto> StepAnalysis { get; set; } = new();
        public string OverallFeedback { get; set; } = string.Empty;
    }

    public class StepAnalysisDto
    {
        public int Step { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public double MarksAwarded { get; set; }
        public double MaxMarksForStep { get; set; }
        public string Feedback { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to normalize OCR text
    /// </summary>
    public class NormalizeOcrRequest
    {
        [Required]
        public string OcrText { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of OCR normalization
    /// </summary>
    public class MathNormalizationResult
    {
        public string NormalizedAnswer { get; set; } = string.Empty;
        public string OriginalText { get; set; } = string.Empty;
        public bool WasModified { get; set; }
    }

    // ==================== RUBRIC DTOs ====================

    /// <summary>
    /// DTO for a single step in the marking rubric
    /// </summary>
    public class StepRubricItemDto
    {
        /// <summary>
        /// Step number (1-based)
        /// </summary>
        public int StepNumber { get; set; }

        /// <summary>
        /// Description of what the student should demonstrate
        /// </summary>
        [Required]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Marks allocated for this step
        /// </summary>
        [Range(1, 100)]
        public int Marks { get; set; }
    }

    /// <summary>
    /// Request to create/save a rubric for a subjective question
    /// </summary>
    public class RubricCreateRequest
    {
        [Required]
        public string ExamId { get; set; } = string.Empty;

        [Required]
        public string QuestionId { get; set; } = string.Empty;

        /// <summary>
        /// Total marks for this question (should equal sum of step marks)
        /// </summary>
        [Range(1, 100)]
        public int TotalMarks { get; set; }

        /// <summary>
        /// The marking steps with allocated marks
        /// </summary>
        [Required]
        public List<StepRubricItemDto> Steps { get; set; } = new();

        /// <summary>
        /// Optional: The question text for reference
        /// </summary>
        public string? QuestionText { get; set; }

        /// <summary>
        /// Optional: The model/expected answer
        /// </summary>
        public string? ModelAnswer { get; set; }
    }

    /// <summary>
    /// Response containing rubric details
    /// </summary>
    public class RubricResponseDto
    {
        public int Id { get; set; }
        public string ExamId { get; set; } = string.Empty;
        public string QuestionId { get; set; } = string.Empty;
        public int TotalMarks { get; set; }
        public List<StepRubricItemDto> Steps { get; set; } = new();
        public string? QuestionText { get; set; }
        public string? ModelAnswer { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Request to batch save rubrics for multiple questions in an exam
    /// </summary>
    public class BatchRubricCreateRequest
    {
        [Required]
        public string ExamId { get; set; } = string.Empty;

        [Required]
        public List<QuestionRubricDto> Rubrics { get; set; } = new();
    }

    /// <summary>
    /// Rubric for a single question (used in batch creation)
    /// </summary>
    public class QuestionRubricDto
    {
        [Required]
        public string QuestionId { get; set; } = string.Empty;

        [Range(1, 100)]
        public int TotalMarks { get; set; }

        [Required]
        public List<StepRubricItemDto> Steps { get; set; } = new();

        public string? QuestionText { get; set; }
        public string? ModelAnswer { get; set; }
    }
}
