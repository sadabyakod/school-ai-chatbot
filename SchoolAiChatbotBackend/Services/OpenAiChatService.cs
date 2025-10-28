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
            try
            {
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                // Use the modern Chat Completions API with GPT-4 or GPT-3.5-turbo
                var payload = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = $"You are a knowledgeable, friendly school tutor. Always provide accurate, helpful educational content in {(language == "kn" ? "Kannada" : "English")}. Keep responses clear and student-friendly."
                        },
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    },
                    max_tokens = 500,
                    temperature = 0.7,
                    top_p = 1.0,
                    frequency_penalty = 0.0,
                    presence_penalty = 0.0
                };

                var content = new System.Net.Http.StringContent(
                    System.Text.Json.JsonSerializer.Serialize(payload), 
                    System.Text.Encoding.UTF8, 
                    "application/json"
                );

                var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new System.Exception($"OpenAI API error ({response.StatusCode}): {errorContent}");
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() == 0)
                {
                    return "No response generated from AI service.";
                }

                var message = choices[0].GetProperty("message");
                var responseText = message.GetProperty("content").GetString();
                
                return responseText?.Trim() ?? "Empty response from AI service.";
            }
            catch (System.Exception ex)
            {
                // Log the error but return a user-friendly message
                throw new System.Exception($"Failed to get AI response: {ex.Message}");
            }
        }

        public async Task<List<float>> GetEmbeddingAsync(string text)
        {
            try
            {
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                
                var payload = new
                {
                    input = text,
                    model = "text-embedding-3-small" // Updated to newer, more efficient model
                };
                
                var content = new System.Net.Http.StringContent(
                    System.Text.Json.JsonSerializer.Serialize(payload), 
                    System.Text.Encoding.UTF8, 
                    "application/json"
                );
                
                var response = await httpClient.PostAsync("https://api.openai.com/v1/embeddings", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new System.Exception($"OpenAI embedding error ({response.StatusCode}): {errorContent}");
                }
                
                var json = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                
                var data = doc.RootElement.GetProperty("data");
                if (data.GetArrayLength() == 0)
                {
                    throw new System.Exception("No embedding data returned from OpenAI");
                }
                
                var embeddingArray = data[0].GetProperty("embedding");
                var embedding = new List<float>();
                
                foreach (var v in embeddingArray.EnumerateArray())
                {
                    if (v.ValueKind == System.Text.Json.JsonValueKind.Number)
                    {
                        embedding.Add((float)v.GetDouble());
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
            catch (System.Exception ex)
            {
                throw new System.Exception($"Failed to get embedding: {ex.Message}");
            }
        }
    }
}