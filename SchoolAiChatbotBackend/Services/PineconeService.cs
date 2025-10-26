using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Linq;

namespace SchoolAiChatbotBackend.Services
{
    public class PineconeService
    {
        private readonly string _apiKey;
        private readonly string _host;
        private readonly string _indexName;
    private readonly int? _expectedDimension;
        private readonly HttpClient _httpClient;

        public PineconeService(IConfiguration config)
        {
            _apiKey = config["Pinecone:ApiKey"];
            _host = config["Pinecone:Host"];
            _indexName = config["Pinecone:IndexName"] ?? string.Empty;

            if (string.IsNullOrEmpty(_apiKey))
                throw new ArgumentNullException("Missing Pinecone:ApiKey in configuration");
            if (string.IsNullOrEmpty(_host))
                throw new ArgumentNullException("Missing Pinecone:Host in configuration");

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (req, cert, chain, errors) => true
            };

            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("Api-Key", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (int.TryParse(config["Pinecone:ExpectedDimension"], out var dim))
            {
                _expectedDimension = dim;
            }
        }

        // -------------------- Test Connection --------------------
        public async Task<(bool ok, string message)> TestConnectionAsync()
        {
            var url = $"{_host}/describe_index_stats";
            var body = new StringContent("{}", Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, body);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return (true, $"Connection OK: {responseText}");
                else
                    return (false, $"Status: {response.StatusCode}, Body: {responseText}");
            }
            catch (Exception ex)
            {
                return (false, $"Exception: {ex.Message}");
            }
        }

        // -------------------- Upsert Vectors --------------------
    public async Task<(bool ok, string result)> UpsertVectorsAsync(SchoolAiChatbotBackend.Models.PineconeUpsertRequest request)
        {
            var url = $"{_host}/vectors/upsert";

            if (request.Vectors == null || !request.Vectors.Any())
                return (false, "No vectors provided.");

            foreach (var v in request.Vectors)
            {
                if (v.Values == null || !v.Values.Any())
                    return (false, $"Invalid vector '{v.Id}' — no values provided.");

                if (_expectedDimension.HasValue && v.Values.Count != _expectedDimension.Value)
                {
                    return (false, $"Invalid vector '{v.Id}' — expected {_expectedDimension.Value} dimensions, got {v.Values.Count}.");
                }
            }

            var payload = new { vectors = request.Vectors };
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            var body = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, body);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return (true, $"Success: {responseText}");
                else
                    return (false, $"Status: {response.StatusCode}, Body: {responseText}");
            }
            catch (Exception ex)
            {
                return (false, $"Exception: {ex.Message}");
            }
        }

        // -------------------- Query / Fetch Vectors --------------------
        public async Task<(bool ok, string result)> QueryVectorsAsync(List<string> ids)
        {
            var url = $"{_host}/vectors/fetch";
            var payload = new { ids = ids };
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };
            var json = System.Text.Json.JsonSerializer.Serialize(payload, options);
            var body = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, body);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return (true, responseText); // Return pure JSON from Pinecone
                else
                    return (false, $"Status: {response.StatusCode}, Body: {responseText}");
            }
            catch (Exception ex)
            {
                return (false, $"Exception: {ex.Message}");
            }
        }

        // -------------------- Query Similar Vectors (Semantic Search) --------------------
        public async Task<List<string>> QuerySimilarVectorsAsync(List<float> embedding, int topK)
        {
            var url = $"{_host}/query";
            var payload = new
            {
                vector = embedding,
                topK = topK
            };
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var body = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, body);
            var responseText = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                return new List<string>();
            using var doc = System.Text.Json.JsonDocument.Parse(responseText);
            var matches = doc.RootElement.GetProperty("matches");
            var ids = new List<string>();
            foreach (var match in matches.EnumerateArray())
            {
                ids.Add(match.GetProperty("id").GetString());
            }
            return ids;
        }
    }
}
