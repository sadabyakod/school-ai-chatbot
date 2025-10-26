namespace SchoolAiChatbotBackend.Models
{
    public class School
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string ContactInfo { get; set; }
        public string PhoneNumber { get; set; } = "";
        public string Email { get; set; } = "";
        public string Website { get; set; } = "";
        public string FeeStructure { get; set; }
        public string Timetable { get; set; }
        public string Holidays { get; set; }
        public string Events { get; set; }
    }
}