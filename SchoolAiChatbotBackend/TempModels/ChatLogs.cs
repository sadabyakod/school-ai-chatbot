using System;
using System.Collections.Generic;

namespace SchoolAiChatbotBackend.TempModels;

public partial class ChatLogs
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Message { get; set; } = null!;

    public string Response { get; set; } = null!;

    public DateTime Timestamp { get; set; }

    public int SchoolId { get; set; }

    public virtual Schools School { get; set; } = null!;

    public virtual Users User { get; set; } = null!;
}
