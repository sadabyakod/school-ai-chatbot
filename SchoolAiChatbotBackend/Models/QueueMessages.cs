namespace SchoolAiChatbotBackend.Models;

/// <summary>
/// Queue message for written submission processing
/// Sent to Azure Queue Storage, processed by Azure Functions
/// </summary>
public class WrittenSubmissionQueueMessage
{
    /// <summary>
    /// Unique submission ID
    /// </summary>
    public string WrittenSubmissionId { get; set; } = string.Empty;

    /// <summary>
    /// Exam ID this submission belongs to
    /// </summary>
    public string ExamId { get; set; } = string.Empty;

    /// <summary>
    /// Student who submitted
    /// </summary>
    public string StudentId { get; set; } = string.Empty;

    /// <summary>
    /// Blob storage URLs for uploaded answer sheet files
    /// </summary>
    public List<string> FilePaths { get; set; } = new();

    /// <summary>
    /// When the submission was uploaded
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// Processing priority (normal, high)
    /// </summary>
    public string Priority { get; set; } = "normal";

    /// <summary>
    /// Number of retry attempts (for poison message handling)
    /// </summary>
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
