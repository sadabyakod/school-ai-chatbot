using System.ComponentModel.DataAnnotations;

namespace SchoolAiChatbotBackend.Models
{
    /// <summary>
    /// Request DTO for chat endpoint
    /// </summary>
    public class ChatAskRequest
    {
        [Required(ErrorMessage = "Question is required.")]
        public string Question { get; set; } = string.Empty;

        public string? SessionId { get; set; }
    }

    /// <summary>
    /// Response DTO for study notes rating
    /// </summary>
    public class RateNoteRequest
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }
    }

    /// <summary>
    /// Response DTO for file upload
    /// </summary>
    public class FileUploadResponse
    {
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public int? FileId { get; set; }
        public int? ChunksCreated { get; set; }
        public string? BlobUrl { get; set; }
    }

    /// <summary>
    /// Request DTO for blob storage service
    /// </summary>
    public class BlobUploadRequest
    {
        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public byte[] FileContent { get; set; } = Array.Empty<byte>();

        public string? Subject { get; set; }
        public string? Grade { get; set; }
        public string? Chapter { get; set; }
        public string? UploadedBy { get; set; }
    }
}
