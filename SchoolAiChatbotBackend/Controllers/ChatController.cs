using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text;
using SchoolAiChatbotBackend.Models;
using SchoolAiChatbotBackend.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SchoolAiChatbotBackend.Controllers
{
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
        /// Now includes follow-up prompts to keep users engaged.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ChatAskRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            _logger.LogInformation("Chat request from {IP}. Question length: {Len}", ip, request.Question?.Length ?? 0);

            try
            {
                // 1️⃣ Generate vector embedding for the question
                var embedding = await _chatService.GetEmbeddingAsync(request.Question);

                // 2️⃣ Query Pinecone for top similar chunks (context)
                const int topK = 5;
                var pineconeIds = await _pineconeService.QuerySimilarVectorsAsync(embedding, topK);

                // 3️⃣ Retrieve syllabus context from DB (if any)
                var chunks = new List<SyllabusChunk>();
                if (pineconeIds?.Any() == true)
                {
                    chunks = await _dbContext.SyllabusChunks
                        .Where(s => pineconeIds.Contains(s.PineconeVectorId))
                        .Take(topK)
                        .ToListAsync();
                }

                // 4️⃣ Prepare context text (if available)
                string contextText = chunks.Any()
                    ? string.Join("\n---\n", chunks.Select(c =>
                        $"Subject: {c.Subject} | Grade: {c.Grade} | Chapter: {c.Chapter}\n{c.ChunkText}"))
                    : "No syllabus context available. Use general academic knowledge.";

                // 5️⃣ Build AI Prompt with Engagement
                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine("### ROLE: You are Smarty, a friendly and intelligent AI study assistant.");
                promptBuilder.AppendLine("### TONE: Conversational, engaging, and supportive. Use emojis occasionally.");
                promptBuilder.AppendLine("### TASK:");
                promptBuilder.AppendLine("- Answer the student's question clearly and simply.");
                promptBuilder.AppendLine("- End your answer by asking a friendly follow-up question that keeps the conversation going.");
                promptBuilder.AppendLine("- Do not repeat the same follow-up twice in a row.");
                promptBuilder.AppendLine("- Examples of follow-up style:");
                promptBuilder.AppendLine("   'Would you like a simple example of that?'");
                promptBuilder.AppendLine("   'Should I explain this with a diagram?'");
                promptBuilder.AppendLine("   'Want to try a short quiz to test your understanding?'");
                promptBuilder.AppendLine("\n### CONTEXT (syllabus):");
                promptBuilder.AppendLine(contextText);
                promptBuilder.AppendLine("\n### STUDENT QUESTION:");
                promptBuilder.AppendLine(request.Question);
                promptBuilder.AppendLine("\n### YOUR RESPONSE:");

                var prompt = promptBuilder.ToString();

                // 6️⃣ Get AI-generated response (main answer)
                var answer = await _chatService.GetChatCompletionAsync(prompt, "en");

                // 7️⃣ Optional: refine follow-up question if answer too short
                if (answer.Length < 60)
                {
                    var enrichPrompt = $"Improve this response by expanding it and adding an engaging question at the end:\n\n{answer}";
                    var enriched = await _chatService.GetChatCompletionAsync(enrichPrompt, "en");
                    if (!string.IsNullOrWhiteSpace(enriched))
                        answer = enriched;
                }

                // 8️⃣ Return structured response
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

                // 9️⃣ Graceful fallback
                return Ok(new
                {
                    status = "error",
                    reply = "⚠️ Hmm, I ran into a small issue processing your question. But don’t worry — you can ask me about Science, Math, or any topic, and I’ll help you step by step! 😊",
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
