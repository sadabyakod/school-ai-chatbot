using System;
using System.Collections.Generic;

namespace SchoolAiChatbotBackend.TempModels;

public partial class Faqs
{
    public int Id { get; set; }

    public string Question { get; set; } = null!;

    public string Answer { get; set; } = null!;

    public int SchoolId { get; set; }

    public virtual Schools School { get; set; } = null!;
}
