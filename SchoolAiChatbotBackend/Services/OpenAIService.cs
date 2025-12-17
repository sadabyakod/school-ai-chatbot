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
        Task<string> GetExamGenerationAsync(string prompt, bool fastMode = true);
        Task<string> EvaluateAnswerAsync(string question, string correctAnswer, string studentAnswer, int maxMarks);
        Task<string> EvaluateAnswerFromImageAsync(string question, string correctAnswer, byte[] imageData, string mimeType, int maxMarks);
        Task<string> EvaluateSubjectiveAnswerAsync(string systemPrompt, string userPrompt);
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
        /// Get exam paper generation from OpenAI with higher token limit
        /// Specifically designed for Karnataka 2nd PUC style exam generation
        /// </summary>
        /// <param name="prompt">The exam generation prompt</param>
        /// <param name="fastMode">If true, uses the configured deployment with reduced tokens for faster generation</param>
        public async Task<string> GetExamGenerationAsync(string prompt, bool fastMode = true)
        {
            try
            {
                // Always use the configured deployment (gpt-4o-mini is already fast)
                var modelToUse = _chatDeployment; 
                
                _logger.LogInformation("Generating exam with model: {Model} (FastMode: {FastMode})", 
                    modelToUse, fastMode);
                
                var requestBody = new
                {
                    messages = new[]
                    {
                        new { role = "system", content = "You are an exam paper generator for Karnataka 2nd PUC board exams. You MUST output ONLY valid JSON with no additional text, comments, or markdown formatting. Respond with a single JSON object only." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = fastMode ? 4000 : 8000, // Reduced tokens for faster response
                    temperature = 0.3,
                    response_format = new { type = "json_object" }
                };

                string url;
                if (_useAzureOpenAI)
                {
                    // Use the configured deployment
                    url = $"{_endpoint}/openai/deployments/{_chatDeployment}/chat/completions?api-version=2024-08-01-preview";
                }
                else
                {
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
                    _logger.LogError("OpenAI API error for exam generation: {Status} - {Content}", response.StatusCode, responseContent);
                    throw new Exception($"OpenAI API returned {response.StatusCode}: {responseContent}");
                }

                var jsonResponse = JsonDocument.Parse(responseContent);
                var messageContent = jsonResponse.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                _logger.LogInformation("Exam generation completed successfully");
                return messageContent ?? throw new Exception("Empty response from OpenAI");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI for exam generation");
                throw;
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
        /// Evaluate a student's text answer against the correct answer
        /// Returns JSON with score and feedback
        /// </summary>
        public async Task<string> EvaluateAnswerAsync(string question, string correctAnswer, string studentAnswer, int maxMarks)
        {
            try
            {
                var systemPrompt = @"You are an exam evaluator for Karnataka 2nd PUC board exams. 
Evaluate the student's answer against the correct answer and assign marks fairly.
Consider partial credit for partially correct answers.
Be strict but fair - award marks based on correctness and completeness.

You MUST respond with ONLY valid JSON in this exact format:
{
  ""score"": <number between 0 and maxMarks>,
  ""feedback"": ""<brief feedback explaining the score>"",
  ""isCorrect"": <true if full marks, false otherwise>
}";

                var userPrompt = $@"Question: {question}

Correct Answer: {correctAnswer}

Student's Answer: {studentAnswer}

Maximum Marks: {maxMarks}

Evaluate the student's answer and provide score (0 to {maxMarks}) with feedback.";

                var requestBody = new
                {
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    max_tokens = 500,
                    temperature = 0.3,
                    response_format = new { type = "json_object" }
                };

                string url;
                if (_useAzureOpenAI)
                {
                    url = $"{_endpoint}/openai/deployments/{_chatDeployment}/chat/completions?api-version=2024-08-01-preview";
                }
                else
                {
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
                    _logger.LogError("OpenAI API error for answer evaluation: {Status} - {Content}", response.StatusCode, responseContent);
                    throw new Exception($"OpenAI API returned {response.StatusCode}");
                }

                var jsonResponse = JsonDocument.Parse(responseContent);
                var messageContent = jsonResponse.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return messageContent ?? throw new Exception("Empty response from OpenAI");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating answer");
                throw;
            }
        }

        /// <summary>
        /// Evaluate a student's handwritten answer from an image using GPT-4 Vision
        /// Returns JSON with score and feedback
        /// </summary>
        public async Task<string> EvaluateAnswerFromImageAsync(string question, string correctAnswer, byte[] imageData, string mimeType, int maxMarks)
        {
            try
            {
                var base64Image = Convert.ToBase64String(imageData);

                var systemPrompt = @"You are an exam evaluator for Karnataka 2nd PUC board exams.
First, read and extract the text from the student's handwritten answer in the image.
Then evaluate the answer against the correct answer and assign marks fairly.
Consider partial credit for partially correct answers.
Be strict but fair - award marks based on correctness and completeness.

You MUST respond with ONLY valid JSON in this exact format:
{
  ""extractedText"": ""<text extracted from the image>"",
  ""score"": <number between 0 and maxMarks>,
  ""feedback"": ""<brief feedback explaining the score>"",
  ""isCorrect"": <true if full marks, false otherwise>
}";

                var userPrompt = $@"Question: {question}

Correct Answer: {correctAnswer}

Maximum Marks: {maxMarks}

Please read the student's handwritten answer from the image, then evaluate it and provide score (0 to {maxMarks}) with feedback.";

                // Build the message content with image
                var messageContent = new object[]
                {
                    new { type = "text", text = userPrompt },
                    new {
                        type = "image_url",
                        image_url = new {
                            url = $"data:{mimeType};base64,{base64Image}",
                            detail = "high"
                        }
                    }
                };

                var requestBody = new
                {
                    messages = new object[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = messageContent }
                    },
                    max_tokens = 1000,
                    temperature = 0.3
                };

                string url;
                if (_useAzureOpenAI)
                {
                    url = $"{_endpoint}/openai/deployments/{_chatDeployment}/chat/completions?api-version=2024-08-01-preview";
                }
                else
                {
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
                    _logger.LogError("OpenAI API error for image evaluation: {Status} - {Content}", response.StatusCode, responseContent);
                    throw new Exception($"OpenAI API returned {response.StatusCode}");
                }

                var jsonResponse = JsonDocument.Parse(responseContent);
                var content = jsonResponse.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return content ?? throw new Exception("Empty response from OpenAI");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating answer from image");
                throw;
            }
        }

        /// <summary>
        /// Evaluate subjective answer with custom system and user prompts
        /// Returns JSON string with evaluation result
        /// </summary>
        public async Task<string> EvaluateSubjectiveAnswerAsync(string systemPrompt, string userPrompt)
        {
            try
            {
                _logger.LogInformation("Evaluating subjective answer with AI");

                var requestBody = new
                {
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    max_tokens = 2000,
                    temperature = 0.3,
                    response_format = new { type = "json_object" }
                };

                string url;
                if (_useAzureOpenAI)
                {
                    url = $"{_endpoint}/openai/deployments/{_chatDeployment}/chat/completions?api-version=2024-08-01-preview";
                }
                else
                {
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
                    _logger.LogError("OpenAI API error for subjective evaluation: {Status} - {Content}",
                        response.StatusCode, responseContent);
                    throw new Exception($"OpenAI API returned {response.StatusCode}");
                }

                var jsonResponse = JsonDocument.Parse(responseContent);
                var content = jsonResponse.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return content ?? throw new Exception("Empty response from OpenAI");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating subjective answer");
                throw;
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
