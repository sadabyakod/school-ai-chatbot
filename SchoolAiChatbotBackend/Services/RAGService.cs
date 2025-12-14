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
                return "No relevant syllabus content found. Use your general knowledge to provide a helpful and accurate answer for the student.";
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
                        prompt = $@"### ROLE: You are an AI tutor for Karnataka State Board students.

### TASK: The student declined to continue with the previous topic. Suggest 3 alternative topics from their syllabus.

### CONTEXT (SYLLABUS CONTENT - ONLY SOURCE OF TRUTH):
{contextText}

### SITUATION:
{question}

### YOUR RESPONSE:
Acknowledge their choice politely and suggest 3 different topics from the syllabus they might be interested in. Keep it brief, friendly, and encouraging. Format as:

No problem! Here are some other topics from your syllabus:
1. [Topic 1] - [brief description]
2. [Topic 2] - [brief description]  
3. [Topic 3] - [brief description]

Which one would you like to learn about?

DO NOT add a follow-up question with 💡 for alternative suggestions.";
                    }
                    else
                    {
                        // Karnataka State Board syllabus-based prompt with fallback to general knowledge
                        prompt = $@"### ROLE: You are an AI tutor for Karnataka State Board students.

### PRIORITY RULES:
1. FIRST, try to answer using the syllabus content provided below.
2. If the answer IS found in the syllabus, clearly use that content as the primary source.
3. If the answer is NOT in the syllabus or only partially available, provide the answer from your general knowledge.
4. When using general knowledge, clearly indicate: ""📚 This topic is not in your uploaded syllabus, but here's what you need to know:""

### ANSWER QUALITY RULES:
5. Write answers suitable for the student's class level.
6. Use very simple language.
7. Prefer short sentences.
8. Explain step-by-step when the topic involves a process or reasoning.
9. Highlight important keywords.
10. If providing steps or points, present them in logical order.
11. Give clear examples to help understanding.
12. Add simple real-world examples to make concepts relatable.

### STUDENT QUESTION HANDLING:
13. If the question is vague, answer using the closest relevant topic.
14. Do NOT ask clarifying questions unless absolutely required.
15. If the question partially matches the syllabus, answer using syllabus content AND supplement with general knowledge if needed.
16. NEVER say you cannot answer - always provide helpful information.

### CONTEXT (SYLLABUS CONTENT - CHECK THIS FIRST):
{contextText}

### STUDENT QUESTION:
{question}

### FORMAT YOUR ANSWER:
- If from syllabus: Start with the syllabus content directly
- If from general knowledge: Start with ""📚 This topic is not in your uploaded syllabus, but here's what you need to know:""
- Use bullet points or numbered steps when applicable
- Keep the tone friendly and encouraging
- End with a short 2-3 line summary

### FOLLOW-UP:
At the end, add ONE engaging follow-up question on a new line starting with ""💡 "" that:
- Helps deepen understanding of the topic
- Encourages the student to learn more";
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
                        fallbackPrompt = $@"You are an AI tutor for Karnataka State Board students. The student declined to continue with the previous topic. Politely acknowledge this and suggest 3 different topics they might be interested in. Format as:

No problem! Here are some other topics you might enjoy:
1. [Topic 1]
2. [Topic 2]
3. [Topic 3]

Which one would you like to learn about?

DO NOT add a follow-up question with 💡.";
                    }
                    else
                    {
                        fallbackPrompt = $@"You are an AI tutor for Karnataka State Board students.

IMPORTANT: No syllabus content was found for this question, but you should still help the student learn!

STUDENT QUESTION: {question}

RESPOND:
1. Start with: ""📚 This topic is not in your uploaded syllabus, but here's what you need to know:""
2. Provide a clear, detailed explanation suitable for a school student
3. Use simple language and examples
4. Break down complex concepts into easy steps
5. Keep the tone friendly and encouraging

FORMAT:
- Use bullet points or numbered steps when applicable
- Include real-world examples to make it relatable
- End with a brief summary

End with a follow-up question on a new line starting with ""💡 "" to encourage further learning.";
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
