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
        /// Status: 0=Uploaded, 1=OcrComplete, 2=EvaluationComplete, 3=OcrFailed, 4=EvaluationFailed
        /// </summary>
        public SubmissionStatus Status { get; set; } = SubmissionStatus.Uploaded;

        // OCR Results
        [Column(TypeName = "nvarchar(max)")]
        public string? ExtractedText { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? ExtractedTextJson { get; set; }

        [MaxLength(500)]
        public string? ExtractedTextBlobPath { get; set; }

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
    /// 0 = Uploaded (waiting for OCR)
    /// 1 = OCR Complete (waiting for evaluation)
    /// 2 = Evaluation Complete (done)
    /// 3 = OCR Failed
    /// 4 = Evaluation Failed
    /// </summary>
    public enum SubmissionStatus
    {
        /// <summary>Uploaded - Answer sheet received, awaiting OCR (Status = 0)</summary>
        Uploaded = 0,
        /// <summary>OCR Complete - Text extraction done, awaiting evaluation (Status = 1)</summary>
        OcrComplete = 1,
        /// <summary>Evaluation Complete - AI scoring finished successfully (Status = 2)</summary>
        EvaluationComplete = 2,
        /// <summary>OCR Failed - Error during text extraction (Status = 3)</summary>
        OcrFailed = 3,
        /// <summary>Evaluation Failed - Error during AI evaluation (Status = 4)</summary>
        EvaluationFailed = 4
    }
}
