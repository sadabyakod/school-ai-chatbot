using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// Unified OpenAI Service supporting both Azure OpenAI and standard OpenAI
    /// Provides chat completion and embedding generation
    /// Compatible with Azure Functions configuration keys
    /// </summary>
    public interface IOpenAIService
    {
        Task<string> GetChatCompletionAsync(string prompt, string language = "en");
        Task<List<float>> GetEmbeddingAsync(string text);
    }

    public class OpenAIService : IOpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAIService> _logger;
        private readonly bool _useAzureOpenAI;
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _chatDeployment;
        private readonly string _embeddingDeployment;
        private readonly bool _useRealEmbeddings;

        public OpenAIService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<OpenAIService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            // Check if Azure OpenAI is configured
            var azureEndpoint = _configuration["AzureOpenAI:Endpoint"];
            var azureApiKey = _configuration["AzureOpenAI:ApiKey"];

            if (!string.IsNullOrWhiteSpace(azureEndpoint) && !string.IsNullOrWhiteSpace(azureApiKey))
            {
                _useAzureOpenAI = true;
                _endpoint = azureEndpoint.TrimEnd('/');
                _apiKey = azureApiKey;
                _chatDeployment = _configuration["AzureOpenAI:ChatDeployment"] ?? "gpt-4";
                _embeddingDeployment = _configuration["AzureOpenAI:EmbeddingDeployment"] ?? "text-embedding-3-small";
                
                _logger.LogInformation("Using Azure OpenAI with endpoint: {Endpoint}", _endpoint);
            }
            else
            {
                _useAzureOpenAI = false;
                _apiKey = _configuration["OpenAI:ApiKey"] ?? 
                          _configuration["OPENAI_API_KEY"] ?? 
                          Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? 
                          throw new InvalidOperationException("OpenAI API key not configured");
                _endpoint = "https://api.openai.com/v1";
                _chatDeployment = "gpt-4";
                _embeddingDeployment = "text-embedding-3-small";
                
                _logger.LogInformation("Using standard OpenAI API");
            }

            _useRealEmbeddings = bool.TryParse(
                _configuration["USE_REAL_EMBEDDINGS"] ?? "true", 
                out var useReal) && useReal;
        }

        /// <summary>
        /// Get chat completion from OpenAI or Azure OpenAI
        /// </summary>
        public async Task<string> GetChatCompletionAsync(string prompt, string language = "en")
        {
            try
            {
                var requestBody = new
                {
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful AI assistant for students." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 1000,
                    temperature = 0.7
                };

                string url;
                if (_useAzureOpenAI)
                {
                    // Azure OpenAI format: {endpoint}/openai/deployments/{deployment}/chat/completions?api-version=2024-08-01-preview
                    url = $"{_endpoint}/openai/deployments/{_chatDeployment}/chat/completions?api-version=2024-08-01-preview";
                }
                else
                {
                    // Standard OpenAI format
                    url = $"{_endpoint}/chat/completions";
                }

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(requestBody),
                        Encoding.UTF8,
                        "application/json")
                };

                if (_useAzureOpenAI)
                {
                    request.Headers.Add("api-key", _apiKey);
                }
                else
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                }

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI API error: {Status} - {Content}", response.StatusCode, responseContent);
                    return "I'm having trouble generating a response right now. Please try again.";
                }

                var jsonResponse = JsonDocument.Parse(responseContent);
                var messageContent = jsonResponse.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return messageContent ?? "No response generated.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI chat completion");
                return "An error occurred while processing your request.";
            }
        }

        /// <summary>
        /// Get embedding vector from OpenAI or Azure OpenAI
        /// </summary>
        public async Task<List<float>> GetEmbeddingAsync(string text)
        {
            if (!_useRealEmbeddings)
            {
                _logger.LogWarning("Real embeddings disabled. Returning mock embedding.");
                return GenerateMockEmbedding(text);
            }

            try
            {
                var requestBody = new
                {
                    input = text,
                    model = _embeddingDeployment
                };

                string url;
                if (_useAzureOpenAI)
                {
                    // Azure OpenAI format: {endpoint}/openai/deployments/{deployment}/embeddings?api-version=2024-08-01-preview
                    url = $"{_endpoint}/openai/deployments/{_embeddingDeployment}/embeddings?api-version=2024-08-01-preview";
                }
                else
                {
                    // Standard OpenAI format
                    url = $"{_endpoint}/embeddings";
                }

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(requestBody),
                        Encoding.UTF8,
                        "application/json")
                };

                if (_useAzureOpenAI)
                {
                    request.Headers.Add("api-key", _apiKey);
                }
                else
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                }

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI Embedding API error: {Status} - {Content}", response.StatusCode, responseContent);
                    return GenerateMockEmbedding(text);
                }

                var jsonResponse = JsonDocument.Parse(responseContent);
                var embeddingArray = jsonResponse.RootElement
                    .GetProperty("data")[0]
                    .GetProperty("embedding");

                var embedding = new List<float>();
                foreach (var value in embeddingArray.EnumerateArray())
                {
                    embedding.Add((float)value.GetDouble());
                }

                _logger.LogInformation("Generated embedding with {Dimensions} dimensions", embedding.Count);
                return embedding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding");
                return GenerateMockEmbedding(text);
            }
        }

        /// <summary>
        /// Generate a simple mock embedding for testing (1536 dimensions for text-embedding-3-small)
        /// </summary>
        private List<float> GenerateMockEmbedding(string text)
        {
            var random = new Random(text.GetHashCode());
            var embedding = new List<float>();
            
            for (int i = 0; i < 1536; i++)
            {
                embedding.Add((float)(random.NextDouble() * 2 - 1)); // Range: -1 to 1
            }
            
            return embedding;
        }
    }
}
