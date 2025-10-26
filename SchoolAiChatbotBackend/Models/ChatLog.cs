using System;

namespace SchoolAiChatbotBackend.Models
{
    public class ChatLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string Message { get; set; }
        public string Response { get; set; }
        public DateTime Timestamp { get; set; }
        public int SchoolId { get; set; }
        public School School { get; set; }
    }
}