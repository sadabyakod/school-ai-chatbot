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
        /// Always returns a meaningful AI-generated response even if no data found.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ChatAskRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            _logger.LogInformation("Chat request received from {IP}. Question length: {Len}", ip, request.Question?.Length ?? 0);

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
                string contextText;
                if (chunks.Any())
                {
                    contextText = string.Join("\n---\n", chunks.Select(c =>
                        $"Subject: {c.Subject} | Grade: {c.Grade} | Chapter: {c.Chapter}\n{c.ChunkText}"));
                }
                else
                {
                    // No syllabus context found → fallback mode
                    contextText = "No syllabus context available. Please use general school knowledge.";
                }

                // 5️⃣ Build prompt dynamically
                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine("### Role: You are a knowledgeable, friendly, and accurate school AI tutor.");
                promptBuilder.AppendLine("### Instruction: Answer clearly and simply, as if explaining to a student.");
                promptBuilder.AppendLine("If no syllabus data is found, use your general academic knowledge to help.");
                promptBuilder.AppendLine("Avoid saying 'I don't have data' — instead, give the best possible helpful answer.");
                promptBuilder.AppendLine("\n### Context:");
                promptBuilder.AppendLine(contextText);
                promptBuilder.AppendLine("\n### Question:");
                promptBuilder.AppendLine(request.Question);
                promptBuilder.AppendLine("\n### Answer:");

                var prompt = promptBuilder.ToString();

                // 6️⃣ Call AI completion service (ChatGPT-style)
                var answer = await _chatService.GetChatCompletionAsync(prompt, "en");

                // 7️⃣ Return structured response
                return Ok(new
                {
                    status = "success",
                    question = request.Question,
                    reply = answer,
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

                // Graceful fallback on error
                return Ok(new
                {
                    status = "error",
                    reply = "Sorry, I couldn’t process that question right now, but here’s what I know: " +
                            "You can ask me about science, math, or any school topic — I’ll help you step by step!",
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
