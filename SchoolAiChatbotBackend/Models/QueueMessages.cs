using System.Text.Json.Serialization;

namespace SchoolAiChatbotBackend.Models;

/// <summary>
/// Queue message for written submission processing
/// Sent to Azure Queue Storage, processed by Azure Functions
/// MUST match SmartStudyFunc.Models.WrittenSubmissionProcessingMessage exactly!
/// </summary>
public class WrittenSubmissionQueueMessage
{
    /// <summary>
    /// Unique submission ID (GUID format required by Azure Function)
    /// </summary>
    [JsonPropertyName("writtenSubmissionId")]
    public Guid WrittenSubmissionId { get; set; }

    /// <summary>
    /// Exam ID this submission belongs to
    /// </summary>
    [JsonPropertyName("examId")]
    public string ExamId { get; set; } = string.Empty;

    /// <summary>
    /// Student who submitted
    /// </summary>
    [JsonPropertyName("studentId")]
    public string StudentId { get; set; } = string.Empty;

    /// <summary>
    /// Blob storage URLs for uploaded answer sheet files
    /// </summary>
    [JsonPropertyName("filePaths")]
    public List<string> FilePaths { get; set; } = new();

    /// <summary>
    /// When the submission was uploaded
    /// </summary>
    [JsonPropertyName("submittedAt")]
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// Processing priority (1=normal, 2=high)
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Number of retry attempts (for poison message handling)
    /// </summary>
    [JsonPropertyName("retryCount")]
    public int RetryCount { get; set; } = 0;
}

/// <summary>
/// Queue names used by the application
/// </summary>
public static class QueueNames
{
    /// <summary>
    /// Queue for written submission processing (OCR + AI evaluation)
    /// </summary>
    public const string WrittenSubmissionProcessing = "written-submission-processing";

    /// <summary>
    /// Queue for file cleanup operations
    /// </summary>
    public const string FileCleanup = "file-cleanup";
}
