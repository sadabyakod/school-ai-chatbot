namespace SchoolAiChatbotBackend.Models
{
    public class Embedding
    {
        public int Id { get; set; }
        public int SyllabusChunkId { get; set; }
        public SyllabusChunk SyllabusChunk { get; set; }
        public string VectorJson { get; set; } // Store embedding as JSON array
        public int SchoolId { get; set; }
        public School School { get; set; }
    }
}