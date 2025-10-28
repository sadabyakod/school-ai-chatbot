using System;
using System.Collections.Generic;

namespace SchoolAiChatbotBackend.TempModels;

public partial class SyllabusChunks
{
    public int Id { get; set; }

    public string Subject { get; set; } = null!;

    public string Grade { get; set; } = null!;

    public string Chapter { get; set; } = null!;

    public string ChunkText { get; set; } = null!;

    public int? UploadedFileId { get; set; }

    public string? PineconeVectorId { get; set; }

    public virtual ICollection<Embeddings> Embeddings { get; set; } = new List<Embeddings>();

    public virtual UploadedFiles? UploadedFile { get; set; }
}
