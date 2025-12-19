using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolAiChatbotBackend.Models
{
    /// <summary>
    /// Represents a student's uploaded written answers for subjective questions
    /// Maps to WrittenSubmissions table in SQL database
    /// Matches SmartStudyFunc Azure Function schema exactly
    /// </summary>
    [Table("WrittenSubmissions")]
    public class WrittenSubmission
    {
        [Key]
        [Column("Id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Alias for Id to maintain backward compatibility with existing code
        /// </summary>
        [NotMapped]
        public string WrittenSubmissionId
        {
            get => Id.ToString();
            set => Id = Guid.TryParse(value, out var guid) ? guid : Guid.NewGuid();
        }

        [Required]
        [MaxLength(100)]
        public string ExamId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string StudentId { get; set; } = string.Empty;

        /// <summary>
        /// JSON array of blob paths stored in database
        /// </summary>
        [Column("FilePaths", TypeName = "nvarchar(max)")]
        public string FilePathsJson { get; set; } = "[]";

        /// <summary>
        /// In-memory access to file paths (not mapped to DB)
        /// </summary>
        [NotMapped]
        public List<string> FilePaths
        {
            get => string.IsNullOrEmpty(FilePathsJson)
                ? new List<string>()
                : System.Text.Json.JsonSerializer.Deserialize<List<string>>(FilePathsJson) ?? new List<string>();
            set => FilePathsJson = System.Text.Json.JsonSerializer.Serialize(value ?? new List<string>());
        }

        /// <summary>
    /// JSON array of MCQ answers submitted with the answer sheet
    /// Format: [{"questionId": "Q1", "selectedOption": "A", "isCorrect": true, "marksAwarded": 1}]
    /// </summary>
    [Column("McqAnswers", TypeName = "nvarchar(max)")]
    public string? McqAnswersJson { get; set; }

    /// <summary>
    /// In-memory access to MCQ answers (not mapped to DB)
    /// Stored in format: { "questionEvaluations": [...] }
    /// </summary>
    [NotMapped]
    public List<McqAnswerDto>? McqAnswers
    {
        get
        {
            if (string.IsNullOrEmpty(McqAnswersJson))
                return null;
            
            try
            {
                // Try to parse as the new format with questionEvaluations wrapper
                var wrapper = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(McqAnswersJson);
                if (wrapper.TryGetProperty("questionEvaluations", out var evaluations))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<McqAnswerDto>>(evaluations.GetRawText());
                }
                // Fallback to direct array for backward compatibility
                return System.Text.Json.JsonSerializer.Deserialize<List<McqAnswerDto>>(McqAnswersJson);
            }
            catch
            {
                return null;
            }
        }
        set
        {
            if (value == null)
            {
                McqAnswersJson = null;
            }
            else
            {
                // Store in the new format with questionEvaluations wrapper
                var wrapper = new { questionEvaluations = value };
                McqAnswersJson = System.Text.Json.JsonSerializer.Serialize(wrapper);
            }
        }
    }

    /// <summary>
    /// MCQ score (if MCQ answers were provided with answer sheet)
    /// </summary>
    [Column("McqScore", TypeName = "decimal(10,2)")]
    public decimal? McqScore { get; set; }

    /// <summary>
    /// MCQ total marks (if MCQ answers were provided with answer sheet)
    /// </summary>
    [Column("McqTotalMarks", TypeName = "decimal(10,2)")]
    public decimal? McqTotalMarks { get; set; }

    /// <summary>
    /// Current status of the submission processing
    /// 0 = Uploaded, 1 = OcrProcessing, 2 = Evaluating, 3 = ResultsReady, 4 = Error
    /// </summary>
    [Column("Status")]
    public SubmissionStatus Status { get; set; } = SubmissionStatus.PendingEvaluation;

    /// <summary>
    /// Extracted text from OCR processing
    /// </summary>
    [Column("ExtractedText", TypeName = "nvarchar(max)")]
    public string? ExtractedText { get; set; }

    /// <summary>
    /// Azure Blob Storage path to the evaluation result JSON file
    /// Populated when Status = Completed (2)
    /// </summary>
    [MaxLength(500)]
    public string? EvaluationResultBlobPath { get; set; }

        /// <summary>
        /// Alias for ExtractedText to maintain backward compatibility
        /// </summary>
        [NotMapped]
        public string? OcrText
        {
            get => ExtractedText;
            set => ExtractedText = value;
        }

        // Evaluation Results
        [Column(TypeName = "decimal(10,2)")]
        public decimal? TotalScore { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? MaxPossibleScore { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Percentage { get; set; }

        [MaxLength(10)]
        public string? Grade { get; set; }

        // Error Tracking
        [Column(TypeName = "nvarchar(max)")]
        public string? ErrorMessage { get; set; }

        public int RetryCount { get; set; } = 0;

        // Timestamps
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? OcrStartedAt { get; set; }
        public DateTime? OcrCompletedAt { get; set; }
        public DateTime? EvaluationStartedAt { get; set; }
        public DateTime? EvaluatedAt { get; set; }

        // Performance metrics
        public long? OcrProcessingTimeMs { get; set; }
        public long? EvaluationProcessingTimeMs { get; set; }
    }

    /// <summary>
    /// Submission status matching SQL schema exactly
    /// 0 = Uploaded/PendingEvaluation (waiting for OCR)
    /// 1 = OCR Complete/OcrProcessing (waiting for evaluation)
    /// 2 = Evaluation Complete/Completed (done)
    /// 3 = OCR Failed
    /// 4 = Evaluation Failed/Failed
    /// </summary>
    public enum SubmissionStatus
    {
        /// <summary>Uploaded - Answer sheet received, awaiting OCR (Status = 0)</summary>
        /// <summary>Status 0: Uploaded - Processing will start soon</summary>
        Uploaded = 0,
        /// <summary>Alias for Uploaded</summary>
        PendingEvaluation = 0,
        
        /// <summary>Status 1: Reading your answer sheet (OCR in progress)</summary>
        OcrProcessing = 1,
        /// <summary>Alias for OcrProcessing</summary>
        OcrComplete = 1,
        
        /// <summary>Status 2: Evaluating your answers (AI evaluation in progress)</summary>
        Evaluating = 2,
        /// <summary>Alias for Evaluating</summary>
        EvaluationComplete = 2,
        
        /// <summary>Status 3: Results Ready - Evaluation completed successfully</summary>
        ResultsReady = 3,
        /// <summary>Alias for ResultsReady</summary>
        Completed = 3,
        
        /// <summary>Status 4: Error occurred during processing</summary>
        Error = 4,
        /// <summary>Alias for Error</summary>
        Failed = 4,
        /// <summary>Alias for Error (legacy)</summary>
        OcrFailed = 4,
        /// <summary>Alias for Error (legacy)</summary>
        EvaluationFailed = 4
    }

    /// <summary>
    /// MCQ answer evaluation for answer sheet submission
    /// Matches the required JSON format: questionEvaluations array
    /// Supports both camelCase (from mobile) and PascalCase JSON properties
    /// </summary>
    public class McqAnswerDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("questionNumber")]
        public int QuestionNumber { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("questionText")]
        public string QuestionText { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("extractedAnswer")]
        public string ExtractedAnswer { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("modelAnswer")]
        public string ModelAnswer { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("maxScore")]
        public decimal MaxScore { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("awardedScore")]
        public decimal AwardedScore { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("feedback")]
        public string Feedback { get; set; } = string.Empty;
        
        // Primary properties for mobile app (camelCase JSON)
        [System.Text.Json.Serialization.JsonPropertyName("questionId")]
        public string QuestionId { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("selectedOption")]
        public string SelectedOption { get; set; } = string.Empty;
    }
}
