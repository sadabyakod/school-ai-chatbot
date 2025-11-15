using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text;
using SchoolAiChatbotBackend.Models;
using SchoolAiChatbotBackend.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System;

namespace SchoolAiChatbotBackend.Controllers
{
    // 🧠 In-memory conversation store
    public static class ConversationMemory
    {
        // Thread-safe dictionary for multiple users
        public static ConcurrentDictionary<string, (string Topic, DateTime SavedAt)> UserContext 
            = new ConcurrentDictionary<string, (string, DateTime)>();

        // Optional auto-cleanup (you can call this occasionally)
        public static void CleanupExpiredContexts(TimeSpan expiry)
        {
            var expired = UserContext
                .Where(kvp => DateTime.UtcNow - kvp.Value.SavedAt > expiry)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expired)
                UserContext.TryRemove(key, out _);
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly PineconeService _pineconeService;
        private readonly Data.AppDbContext _dbContext;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            IChatService chatService,
            PineconeService pineconeService,
            Data.AppDbContext dbContext,
            ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _pineconeService = pineconeService;
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Answers students' academic questions using AI and syllabus-based context.
        /// Now includes in-memory context to continue conversations.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ChatAskRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string userId = ip; // can later replace with user ID from frontend
            string userMessage = request.Question.ToLower();

            _logger.LogInformation("Chat request from {IP}. Question: {Q}", ip, request.Question);

            try
            {
                // 🧠 Step 1: Restore last topic if user says "yes" or "explain more"
                if ((userMessage.Contains("yes") || userMessage.Contains("explain more"))
                    && ConversationMemory.UserContext.ContainsKey(userId))
                {
                    var lastTopic = ConversationMemory.UserContext[userId].Topic;
                    request.Question = $"Explain more about {lastTopic}.";
                    _logger.LogInformation("Continuing topic for {UserId}: {Topic}", userId, lastTopic);
                }

                // 🧹 Clean expired topics (older than 10 minutes)
                ConversationMemory.CleanupExpiredContexts(TimeSpan.FromMinutes(10));

                // 1️⃣ Generate vector embedding for the question
                var embedding = await _chatService.GetEmbeddingAsync(request.Question);

                // 2️⃣ Query Pinecone for top similar chunks
                const int topK = 5;
                var pineconeIds = await _pineconeService.QuerySimilarVectorsAsync(embedding, topK);

                // 3️⃣ Retrieve syllabus context from DB
                var chunks = new List<SyllabusChunk>();
                if (pineconeIds?.Any() == true)
                {
                    chunks = await _dbContext.SyllabusChunks
                        .Where(s => pineconeIds.Contains(s.PineconeVectorId))
                        .Take(topK)
                        .ToListAsync();
                }

                // 4️⃣ Prepare context text
                string contextText = chunks.Any()
                    ? string.Join("\n---\n", chunks.Select(c =>
                        $"Subject: {c.Subject} | Grade: {c.Grade} | Chapter: {c.Chapter}\n{c.ChunkText}"))
                    : "No syllabus context available. Use general academic knowledge.";

                // 5️⃣ Build AI Prompt with Engagement
                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine("### ROLE: You are Smarty, a friendly and intelligent AI study assistant.");
                promptBuilder.AppendLine("### TONE: Encouraging and conversational, like a kind teacher.");
                promptBuilder.AppendLine("### TASK:");
                promptBuilder.AppendLine("- Answer clearly and simply.");
                promptBuilder.AppendLine("- End with a short follow-up question (e.g., 'Would you like an example?' or 'Should I explain more?').");
                promptBuilder.AppendLine("- Use one emoji if it fits naturally.");
                promptBuilder.AppendLine("\n### CONTEXT (syllabus):");
                promptBuilder.AppendLine(contextText);
                promptBuilder.AppendLine("\n### STUDENT QUESTION:");
                promptBuilder.AppendLine(request.Question);
                promptBuilder.AppendLine("\n### YOUR RESPONSE:");

                var prompt = promptBuilder.ToString();

                // 6️⃣ Get AI-generated response
                var answer = await _chatService.GetChatCompletionAsync(prompt, "en");

                // 7️⃣ Improve short responses
                if (answer.Length < 60)
                {
                    var enrichPrompt = $"Improve this response and end with an engaging question:\n\n{answer}";
                    var enriched = await _chatService.GetChatCompletionAsync(enrichPrompt, "en");
                    if (!string.IsNullOrWhiteSpace(enriched))
                        answer = enriched;
                }

                // 🧠 Step 8: Save current topic for future follow-ups
                if (!userMessage.Contains("yes") && !userMessage.Contains("explain more"))
                {
                    ConversationMemory.UserContext[userId] = (request.Question, DateTime.UtcNow);
                    _logger.LogInformation("Saved topic for {UserId}: {Topic}", userId, request.Question);
                }

                // 9️⃣ Return structured response
                return Ok(new
                {
                    status = "success",
                    question = request.Question,
                    reply = answer.Trim(),
                    contextCount = chunks.Count,
                    usedChunks = chunks.Select(c => new
                    {
                        c.Subject,
                        c.Grade,
                        c.Chapter
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error answering question");
                return Ok(new
                {
                    status = "error",
                    reply = "⚠️ Oops! I had a small hiccup. Try again, and I’ll help you step by step! 😊",
                    debug = ex.Message
                });
            }
        }

        [HttpGet("test")]
        public IActionResult Test() => Ok("✅ Chat endpoint is working!");
    }

    public class ChatAskRequest
    {
        [Required(ErrorMessage = "Question is required.")]
        public string Question { get; set; } = string.Empty;
    }
}
