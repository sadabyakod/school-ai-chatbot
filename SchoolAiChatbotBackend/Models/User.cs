namespace SchoolAiChatbotBackend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; } // Student, Parent, Teacher, Admin
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int SchoolId { get; set; }
        public School School { get; set; }
        public string LanguagePreference { get; set; }
    }
}