using System;
using System.Collections.Generic;

namespace SchoolAiChatbotBackend.TempModels;

public partial class Embeddings
{
    public int Id { get; set; }

    public int SyllabusChunkId { get; set; }

    public string VectorJson { get; set; } = null!;

    public int SchoolId { get; set; }

    public virtual Schools School { get; set; } = null!;

    public virtual SyllabusChunks SyllabusChunk { get; set; } = null!;
}
