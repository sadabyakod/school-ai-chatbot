using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SchoolAiChatbotBackend.Services
{
    // Lightweight stub implementation for Claude Sonnet 4.5
    public class ClaudeChatService : IChatService
    {
        private readonly string _apiKey;
        public ClaudeChatService(IConfiguration config)
        {
            // Expecting a configuration value for Claude (if any). Keep optional.
            _apiKey = config["Claude:ApiKey"] ?? string.Empty;
        }

        public Task<List<float>> GetEmbeddingAsync(string text)
        {
            // Claude embedding endpoint isn't implemented here. Fallback to empty list to avoid breaking callers.
            return Task.FromResult(new List<float>());
        }

        public Task<string> GetChatCompletionAsync(string prompt, string language)
        {
            // For now, return a placeholder indicating Claude would answer here.
            var reply = language == "kn" ? "(Claude reply in Kannada - placeholder)" : "(Claude reply - placeholder)";
            return Task.FromResult(reply);
        }
    }
}
