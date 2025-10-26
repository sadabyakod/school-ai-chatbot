namespace SchoolAiChatbotBackend.Models
{
    public class ChatRequest
    {
        public int UserId { get; set; }
        public int SchoolId { get; set; }
        public string Message { get; set; }
        public string Language { get; set; } // "en" or "kn"

        // New property to validate if the input is a question
        public bool IsQuestion => Message?.Trim().EndsWith("?") ?? false;
    }
}