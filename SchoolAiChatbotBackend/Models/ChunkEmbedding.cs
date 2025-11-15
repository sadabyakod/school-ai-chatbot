using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolAiChatbotBackend.Models
{
    /// <summary>
    /// Chunk embedding model - matches Azure Functions ChunkEmbeddings table
    /// Created by Azure Functions, read by ASP.NET Core for RAG similarity search
    /// </summary>
    [Table("ChunkEmbeddings")]
    public class ChunkEmbedding
    {
        [Key]
        public int Id { get; set; }

        public int ChunkId { get; set; }

        /// <summary>
        /// JSON array of embedding values (1536 dimensions for text-embedding-3-small)
        /// </summary>
        [Required]
        public string EmbeddingVector { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("ChunkId")]
        public virtual FileChunk? FileChunk { get; set; }
    }
}
