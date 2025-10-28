using System;
using System.Collections.Generic;

namespace SchoolAiChatbotBackend.TempModels;

public partial class UploadedFiles
{
    public int Id { get; set; }

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public DateTime UploadDate { get; set; }

    public int EmbeddingDimension { get; set; }

    public string EmbeddingVector { get; set; } = null!;

    public virtual ICollection<SyllabusChunks> SyllabusChunks { get; set; } = new List<SyllabusChunks>();
}
