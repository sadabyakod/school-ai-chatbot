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

                // 3️⃣ Retrieve syllabus context from DB
                var chunks = new List<SyllabusChunk>();
                if (pineconeIds?.Any() == true)
                {
                    chunks = await _dbContext.SyllabusChunks
                        .Where(s => pineconeIds.Contains(s.PineconeVectorId))
                        .Take(topK)
                        .ToListAsync();
                }

                // 4️⃣ Prepare the context text
                var contextText = chunks.Any()
                    ? string.Join("\n---\n", chunks.Select(c =>
                        $"Subject: {c.Subject} | Grade: {c.Grade} | Chapter: {c.Chapter}\n{c.ChunkText}"))
                    : "No syllabus context found.";

                // 5️⃣ Build a rich AI prompt
                var prompt = new StringBuilder()
                    .AppendLine("### Role: You are a knowledgeable, friendly smart study school tutor.")
                    .AppendLine("### Instruction: Use the syllabus context to explain concepts clearly and accurately.")
                    .AppendLine("If you don't find the answer, say 'I don’t have enough syllabus data to answer precisely.'")
                    .AppendLine("Always keep answers concise and student-friendly.\n")
                    .AppendLine("### Context:")
                    .AppendLine(contextText)
                    .AppendLine("\n### Question:")
                    .AppendLine(request.Question)
                    .AppendLine("\n### Answer:")
                    .ToString();

                // 6️⃣ Call AI completion service
                var answer = await _chatService.GetChatCompletionAsync(prompt, "en");

                // 7️⃣ Return structured response
                return Ok(new
                {
                    status = "success",
                    question = request.Question,
                    reply = answer,  // Changed from 'answer' to 'reply' to match frontend expectation
                    contextCount = chunks.Count,
                    usedChunks = chunks.Select(c => new { c.Subject, c.Grade, c.Chapter }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error answering question");

                return Ok(new
                {
                    status = "error",
                    reply = "AI service temporarily unavailable. Please try again later.",  // Changed from 'message' to 'reply'
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






