using System.Collections.Generic;
using System.Threading.Tasks;

namespace SchoolAiChatbotBackend.Services
{
    public interface IChatService
    {
        Task<string> GetChatCompletionAsync(string prompt, string language);
        Task<List<float>> GetEmbeddingAsync(string text);
    }
}
