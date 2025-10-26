using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SchoolAiChatbotBackend.Data;

namespace SchoolAiChatbotBackend.Models
{
    public class SyllabusChunk
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string ChunkText { get; set; } = string.Empty;

        [Required]
        public string Chapter { get; set; } = string.Empty;

        [ForeignKey("UploadedFile")]
        public int UploadedFileId { get; set; }

        [Required]
        public string PineconeVectorId { get; set; } = string.Empty;

        public UploadedFile? UploadedFile { get; set; }
    }
}