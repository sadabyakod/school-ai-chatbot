using Microsoft.EntityFrameworkCore;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// RAG (Retrieval-Augmented Generation) Service
    /// SQL-based vector similarity search using ChunkEmbeddings table
    /// Migrated from Azure Functions to work with shared Azure SQL database
    /// </summary>
    public interface IRAGService
    {
        Task<List<FileChunk>> FindRelevantChunksAsync(string query, int topK = 5, string? subject = null, string? grade = null);
        Task<string> BuildContextTextAsync(List<FileChunk> chunks);
        Task<string> GetRAGAnswerAsync(string question, string userId, string sessionId);
    }

    public class RAGService : IRAGService
    {
        private readonly IOpenAIService _openAIService;
        private readonly AppDbContext _dbContext;
        private readonly IChatHistoryService _chatHistoryService;
        private readonly Microsoft.Extensions.Logging.ILogger<RAGService> _logger;

        public RAGService(
            IOpenAIService openAIService,
            AppDbContext dbContext,
            IChatHistoryService chatHistoryService,
            Microsoft.Extensions.Logging.ILogger<RAGService> logger)
        {
            _openAIService = openAIService;
            _dbContext = dbContext;
            _chatHistoryService = chatHistoryService;
            _logger = logger;
        }

        /// <summary>
        /// Find relevant chunks using SQL-based cosine similarity
        /// 1. Generate embedding for query
        /// 2. Compute cosine similarity in SQL
        /// 3. Return top-K most similar chunks
        /// </summary>
        public async Task<List<FileChunk>> FindRelevantChunksAsync(
            string query, 
            int topK = 5, 
            string? subject = null, 
            string? grade = null)
        {
            try
            {
                // Step 1: Generate embedding for the query
                var queryEmbedding = await _openAIService.GetEmbeddingAsync(query);
                var queryEmbeddingJson = JsonSerializer.Serialize(queryEmbedding);

                _logger.LogInformation("Generated query embedding with {Dimensions} dimensions", queryEmbedding.Count);

                // Step 2: Get all chunk embeddings with their chunks (with optional filters)
                var chunksQuery = _dbContext.ChunkEmbeddings
                    .Include(ce => ce.FileChunk)
                        .ThenInclude(fc => fc!.UploadedFile)
                    .AsQueryable();

                // Apply filters if provided
                if (!string.IsNullOrWhiteSpace(subject))
                {
                    chunksQuery = chunksQuery.Where(ce => 
                        ce.FileChunk!.Subject == subject);
                }

                if (!string.IsNullOrWhiteSpace(grade))
                {
                    chunksQuery = chunksQuery.Where(ce => 
                        ce.FileChunk!.Grade == grade);
                }

                var chunkEmbeddings = await chunksQuery.ToListAsync();

                if (!chunkEmbeddings.Any())
                {
                    _logger.LogWarning("No chunk embeddings found in database");
                    return new List<FileChunk>();
                }

                _logger.LogInformation("Found {Count} chunk embeddings to compare", chunkEmbeddings.Count);

                // Step 3: Calculate cosine similarity for each chunk
                var similarities = new List<(FileChunk Chunk, double Similarity)>();

                foreach (var chunkEmbedding in chunkEmbeddings)
                {
                    if (chunkEmbedding.FileChunk == null)
                        continue;

                    try
                    {
                        var storedEmbedding = JsonSerializer.Deserialize<List<float>>(chunkEmbedding.EmbeddingVector);
                        if (storedEmbedding == null || storedEmbedding.Count == 0)
                            continue;

                        var similarity = CosineSimilarity(queryEmbedding, storedEmbedding);
                        similarities.Add((chunkEmbedding.FileChunk, similarity));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse embedding for chunk {ChunkId}", chunkEmbedding.ChunkId);
                    }
                }

                // Step 4: Return top-K most similar chunks
                var topChunks = similarities
                    .OrderByDescending(s => s.Similarity)
                    .Take(topK)
                    .Select(s => s.Chunk)
                    .ToList();

                _logger.LogInformation("Returning {Count} most relevant chunks (requested {TopK})", 
                    topChunks.Count, topK);

                return topChunks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding relevant chunks for query: {Query}", query);
                return new List<FileChunk>();
            }
        }

        /// <summary>
        /// Calculate cosine similarity between two vectors
        /// </summary>
        private double CosineSimilarity(List<float> vectorA, List<float> vectorB)
        {
            if (vectorA.Count != vectorB.Count)
            {
                throw new ArgumentException("Vectors must have the same dimension");
            }

            double dotProduct = 0;
            double magnitudeA = 0;
            double magnitudeB = 0;

            for (int i = 0; i < vectorA.Count; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += vectorA[i] * vectorA[i];
                magnitudeB += vectorB[i] * vectorB[i];
            }

            magnitudeA = Math.Sqrt(magnitudeA);
            magnitudeB = Math.Sqrt(magnitudeB);

            if (magnitudeA == 0 || magnitudeB == 0)
                return 0;

            return dotProduct / (magnitudeA * magnitudeB);
        }

        /// <summary>
        /// Build formatted context text from retrieved chunks
        /// </summary>
        public async Task<string> BuildContextTextAsync(List<FileChunk> chunks)
        {
            await Task.CompletedTask; // Make async

            if (!chunks.Any())
            {
                return "No relevant educational content found. Please provide a general answer based on your knowledge.";
            }

            var contextParts = chunks.Select((chunk, index) =>
            {
                var header = $"--- Source {index + 1} ---";
                var metadata = $"Subject: {chunk.Subject ?? "N/A"} | Grade: {chunk.Grade ?? "N/A"} | Chapter: {chunk.Chapter ?? "N/A"}";
                var content = chunk.ChunkText;
                
                return $"{header}\n{metadata}\n{content}";
            });

            return string.Join("\n\n", contextParts);
        }

        /// <summary>
        /// Get complete RAG answer: retrieve context + generate answer + save history
        /// </summary>
        public async Task<string> GetRAGAnswerAsync(string question, string userId, string sessionId)
        {
            try
            {
                string answer;
                int chunksFound = 0;
                
                try
                {
                    // Step 1: Find relevant chunks
                    var relevantChunks = await FindRelevantChunksAsync(question, topK: 5);
                    chunksFound = relevantChunks.Count;

                    // Step 2: Build context
                    var contextText = await BuildContextTextAsync(relevantChunks);

                    // Step 3: Detect if this is a response to a follow-up (suggesting alternatives)
                    bool isSuggestingAlternatives = question.Contains("declined to continue") || 
                                                   question.Contains("Suggest 3 different");

                    string prompt;
                    if (isSuggestingAlternatives)
                    {
                        // Special prompt for when user says "no" to follow-up
                        prompt = $@"### ROLE: You are a helpful AI study assistant.

### TASK: The student declined to continue with the previous topic. Suggest 3 alternative topics they might find interesting.

### CONTEXT (Educational Content):
{contextText}

### SITUATION:
{question}

### YOUR RESPONSE:
Acknowledge their choice politely and suggest 3 different but related topics they might be interested in instead. Keep it brief, friendly, and engaging. Format as:

No problem! Let me suggest some other topics you might enjoy:
1. [Topic 1] - [brief description]
2. [Topic 2] - [brief description]  
3. [Topic 3] - [brief description]

Which one interests you, or would you like to explore something completely different?

DO NOT add a follow-up question with 💡 for alternative suggestions.";
                    }
                    else
                    {
                        // Normal prompt with follow-up question
                        prompt = $@"### ROLE: You are a helpful AI study assistant.

### TASK: Answer the student's question using the provided educational content and ALWAYS end with an engaging follow-up question.

### CONTEXT (Educational Content):
{contextText}

### STUDENT QUESTION:
{question}

### YOUR ANSWER:
Provide a clear, accurate answer based on the context above. If the context doesn't contain relevant information, say so and provide general guidance.

### IMPORTANT: 
At the end of your answer, ALWAYS ask ONE engaging follow-up question that:
1. Helps deepen understanding of the topic
2. Connects to related concepts
3. Encourages critical thinking
4. Is specific and relevant to what was just explained

Format the follow-up question on a new line starting with ""💡 "".";
                    }

                    // Step 4: Get AI response
                    answer = await _openAIService.GetChatCompletionAsync(prompt);
                }
                catch (Exception ex)
                {
                    // If RAG fails, fall back to direct AI response
                    _logger.LogWarning(ex, "RAG pipeline failed, using direct AI response");
                    
                    bool isSuggestingAlternatives = question.Contains("declined to continue") || 
                                                   question.Contains("Suggest 3 different");
                    
                    string fallbackPrompt;
                    if (isSuggestingAlternatives)
                    {
                        fallbackPrompt = $@"The student declined to continue with the previous topic. Politely acknowledge this and suggest 3 different related topics they might be interested in instead. Format as:

No problem! Let me suggest some other topics:
1. [Topic 1]
2. [Topic 2]
3. [Topic 3]

Which one interests you?

DO NOT add a follow-up question with 💡.";
                    }
                    else
                    {
                        fallbackPrompt = $@"Answer this student question clearly and concisely: {question}

IMPORTANT: At the end of your answer, ALWAYS ask ONE engaging follow-up question that helps deepen understanding. Format it on a new line starting with ""💡 "".";
                    }
                    
                    answer = await _openAIService.GetChatCompletionAsync(fallbackPrompt);
                    chunksFound = 0;
                }

                // Step 5: Save to chat history
                var contextUsedJson = "[]";

                await _chatHistoryService.SaveChatHistoryAsync(
                    userId,
                    sessionId,
                    question,
                    answer,
                    contextUsedJson,
                    chunksFound
                );

                _logger.LogInformation("RAG answer generated for session {SessionId} with {ChunkCount} chunks", 
                    sessionId, chunksFound);

                return answer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating RAG answer");
                return "I'm having trouble answering your question right now. Please try again.";
            }
        }
    }
}
