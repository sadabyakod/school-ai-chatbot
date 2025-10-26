namespace SchoolAiChatbotBackend.Models
{
    public class Faq
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public int SchoolId { get; set; }
        public School School { get; set; }
    }
}