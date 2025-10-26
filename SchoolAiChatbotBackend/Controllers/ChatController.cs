using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;
using SchoolAiChatbotBackend.Models;
using SchoolAiChatbotBackend.Services;
using System.Threading.Tasks;

namespace SchoolAiChatbotBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly FaqEmbeddingService _faqEmbeddingService;
        private readonly SchoolAiChatbotBackend.Services.PineconeService _pineconeService;
        private readonly SchoolAiChatbotBackend.Data.AppDbContext _dbContext;
        private readonly ILogger<ChatController> _logger;

        public ChatController(ILogger<ChatController> logger, IChatService chatService, FaqEmbeddingService faqService, SchoolAiChatbotBackend.Services.PineconeService pineconeService, SchoolAiChatbotBackend.Data.AppDbContext dbContext)
        {
            _logger = logger;
            _chatService = chatService;
            _faqEmbeddingService = faqService;
            _pineconeService = pineconeService;
            _dbContext = dbContext;
        }

        [HttpPost]
        /// <summary>
        /// Finds the closest matching FAQ answer based on the user's question.
        /// </summary>
        /// <param name="question">The user input question.</param>
        /// <returns>The most relevant answer from the database.</returns>
        public async Task<ActionResult<ChatResponse>> Post([FromBody] ChatRequest request)
        {
            if (request == null)
                return BadRequest("Request body is missing.");

            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("Message is required.");

            // Log requester details
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var origin = Request.Headers["Origin"].ToString();
            var referer = Request.Headers["Referer"].ToString();
            var ua = Request.Headers["User-Agent"].ToString();
            _logger.LogInformation("POST /api/chat from {RemoteIp} Origin={Origin} Referer={Referer} UA={UserAgent} MessageLength={Len}", ip, origin, referer, ua, request.Message?.Length ?? 0);

            var reply = await _chatService.GetChatCompletionAsync(request.Message, request.Language ?? "en");
            return Ok(new ChatResponse { Reply = reply, Language = request.Language });
        }

        [HttpPost("ask")]
        /// <summary>
        /// Finds the closest matching FAQ answer based on the user's question.
        /// </summary>
        /// <param name="question">The user input question.</param>
        /// <returns>The most relevant answer from the database. only specific to school FAQs.</returns>
        public async Task<IActionResult> Ask([FromBody] ChatAskRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Question))
                return BadRequest(new { status = "error", message = "Question is required." });

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var origin = Request.Headers["Origin"].ToString();
            var referer = Request.Headers["Referer"].ToString();
            var ua = Request.Headers["User-Agent"].ToString();
            _logger.LogInformation("POST /api/chat/ask from {RemoteIp} Origin={Origin} Referer={Referer} UA={UserAgent} QuestionLength={Len}", ip, origin, referer, ua, request.Question?.Length ?? 0);

            _logger.LogInformation("Received question: {Question}", request.Question);

            // 1) Get embedding for the question
            var embedding = await _chatService.GetEmbeddingAsync(request.Question);

            // 2) Query Pinecone for similar vectors (optional school filter)
            var topK = request.TopK ?? 5;
            var pineconeIds = await _pineconeService.QuerySimilarVectorsAsync(embedding, topK);

            // 3) Fetch matching syllabus chunks from DB
            var chunks = new List<Models.SyllabusChunk>();
            if (pineconeIds != null && pineconeIds.Any())
            {
                chunks = _dbContext.SyllabusChunks.Where(s => pineconeIds.Contains(s.PineconeVectorId)).Take(topK).ToList();
            }

            // 4) Build prompt using retrieved context
            var contextText = string.Empty;
            if (chunks != null && chunks.Any())
            {
                contextText = string.Join("\n---\n", chunks.Select(c => $"Subject: {c.Subject} | Grade: {c.Grade} | Chapter: {c.Chapter}\n{c.ChunkText}"));
            }

            var promptBuilder = new System.Text.StringBuilder();
            promptBuilder.AppendLine("You are a helpful tutor. Use the provided syllabus/context to answer the student's question. If the context doesn't contain the answer, say you don't know and provide a concise guidance.");
            if (!string.IsNullOrWhiteSpace(contextText))
            {
                promptBuilder.AppendLine("Context:");
                promptBuilder.AppendLine(contextText);
                promptBuilder.AppendLine("---");
            }
            promptBuilder.AppendLine("Question:");
            promptBuilder.AppendLine(request.Question);

            var prompt = promptBuilder.ToString();

            // 5) Call OpenAI completion with the prompt
            var language = request.Language ?? "en";
            var answer = await _chatService.GetChatCompletionAsync(prompt, language);

            // 6) Return structured result
            return Ok(new
            {
                answer,
                contextCount = chunks?.Count ?? 0,
                usedChunkIds = chunks?.Select(c => c.PineconeVectorId).ToList() ?? new List<string>()
            });
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var origin = Request.Headers["Origin"].ToString();
            var referer = Request.Headers["Referer"].ToString();
            var ua = Request.Headers["User-Agent"].ToString();
            _logger.LogInformation("GET /api/chat/test from {RemoteIp} Origin={Origin} Referer={Referer} UA={UserAgent}", ip, origin, referer, ua);

            return Ok("Test endpoint is working!");
        }
    }

    public class ChatAskRequest
    {
        public string? Question { get; set; }
        public int? TopK { get; set; }
        public string? Language { get; set; }
    }
}