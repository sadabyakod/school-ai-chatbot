using System;
using System.Collections.Generic;

namespace SchoolAiChatbotBackend.TempModels;

public partial class Users
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int SchoolId { get; set; }

    public string LanguagePreference { get; set; } = null!;

    public virtual ICollection<ChatLogs> ChatLogs { get; set; } = new List<ChatLogs>();

    public virtual Schools School { get; set; } = null!;
}
