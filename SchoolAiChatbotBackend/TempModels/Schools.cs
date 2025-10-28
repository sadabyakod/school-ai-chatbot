using System;
using System.Collections.Generic;

namespace SchoolAiChatbotBackend.TempModels;

public partial class Schools
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string ContactInfo { get; set; } = null!;

    public string FeeStructure { get; set; } = null!;

    public string Timetable { get; set; } = null!;

    public string Holidays { get; set; } = null!;

    public string Events { get; set; } = null!;

    public virtual ICollection<ChatLogs> ChatLogs { get; set; } = new List<ChatLogs>();

    public virtual ICollection<Embeddings> Embeddings { get; set; } = new List<Embeddings>();

    public virtual ICollection<Faqs> Faqs { get; set; } = new List<Faqs>();

    public virtual ICollection<Users> Users { get; set; } = new List<Users>();
}
