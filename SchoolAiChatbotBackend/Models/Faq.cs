namespace SchoolAiChatbotBackend.Models
{
    public class Faq
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public string Category { get; set; } = "General";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int SchoolId { get; set; }
        public School School { get; set; }
    }
}