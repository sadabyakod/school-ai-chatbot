using OpenAI;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SchoolAiChatbotBackend.Services
{
    public class OpenAiChatService : IChatService
    {
        private readonly string _apiKey;
        public OpenAiChatService(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<string> GetChatCompletionAsync(string prompt, string language)
        {
            var fullPrompt = $"You are a helpful school assistant. Reply in {(language == "kn" ? "Kannada" : "English")}\nUser: {prompt}";
            using var httpClient = new System.Net.Http.HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            var payload = new
            {
                model = "gpt-3.5-turbo-instruct", // fallback to a known completions model
                prompt = fullPrompt,
                max_tokens = 256
            };
            var content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.openai.com/v1/completions", content);
            if (!response.IsSuccessStatusCode)
                return $"(OpenAI error: {response.StatusCode})";
            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var text = doc.RootElement.GetProperty("choices")[0].GetProperty("text").GetString();
            return text?.Trim() ?? "(No response)";
        }

        public async Task<List<float>> GetEmbeddingAsync(string text)
        {
            using var httpClient = new System.Net.Http.HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            var payload = new
            {
                input = text,
                model = "text-embedding-ada-002"
            };
            var content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.openai.com/v1/embeddings", content);
            if (!response.IsSuccessStatusCode)
                throw new System.Exception($"OpenAI embedding error: {response.StatusCode}");
            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var embeddingArray = doc.RootElement.GetProperty("data")[0].GetProperty("embedding");
            var embedding = new List<float>();
            // Parse numeric values as double then cast to float for robustness
            foreach (var v in embeddingArray.EnumerateArray())
            {
                if (v.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    var d = v.GetDouble();
                    embedding.Add((float)d);
                }
                else
                {
                    // Fallback: try to get as string then parse
                    var s = v.GetString();
                    if (float.TryParse(s, out var fv))
                        embedding.Add(fv);
                    else
                        embedding.Add(0f);
                }
            }
            return embedding;
        }
    }
}