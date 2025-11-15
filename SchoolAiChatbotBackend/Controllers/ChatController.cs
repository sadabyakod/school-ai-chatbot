using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text;
using SchoolAiChatbotBackend.Models;
using SchoolAiChatbotBackend.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace SchoolAiChatbotBackend.Controllers
{
    /// <summary>
    /// Chat Controller - RAG-powered Q&A with SQL-backed conversation history
    /// Migrated from Azure Functions SearchRagQuery feature
    /// Uses SQL-based vector similarity search with FileChunks and ChunkEmbeddings
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IRAGService _ragService;
        private readonly IChatHistoryService _chatHistoryService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            IRAGService ragService,
            IChatHistoryService chatHistoryService,
            ILogger<ChatController> logger)
        {
            _ragService = ragService;
            _chatHistoryService = chatHistoryService;
            _logger = logger;
        }

        /// <summary>
        /// Answers students' academic questions using AI and SQL-based RAG.
        /// POST /api/chat
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ChatAskRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string userId = ip; // Can later replace with authenticated user ID
            string sessionId = request.SessionId ?? Guid.NewGuid().ToString();

            _logger.LogInformation("Chat request from {IP}. Session: {SessionId}, Question: {Q}", 
                ip, sessionId, request.Question);

            try
            {
                // Check for follow-up questions
                string question = request.Question;
                if (question.ToLower().Contains("yes") || 
                    question.ToLower().Contains("explain more") || 
                    question.ToLower().Contains("continue"))
                {
                    var lastMessage = await _chatHistoryService.GetLastMessageAsync(userId, sessionId);
                    if (lastMessage != null)
                    {
                        question = $"Explain more about: {lastMessage.Message}";
                        _logger.LogInformation("Continuing topic for session {SessionId}", sessionId);
                    }
                }

                // Use RAG service to get answer (handles retrieval, prompt building, and saving)
                var answer = await _ragService.GetRAGAnswerAsync(question, userId, sessionId);

                return Ok(new
                {
                    status = "success",
                    sessionId = sessionId,
                    question = request.Question,
                    reply = answer,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error answering question");
                return Ok(new
                {
                    status = "error",
                    sessionId = sessionId,
                    reply = "⚠️ Oops! I had a small hiccup. Try again, and I'll help you step by step! 😊",
                    debug = ex.Message
                });
            }
        }

        /// <summary>
        /// Get chat history for a session
        /// GET /api/chat/history?sessionId={sessionId}&limit=10
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] string sessionId, [FromQuery] int limit = 10)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string userId = ip;

            _logger.LogInformation("Retrieving chat history for session {SessionId}", sessionId);

            try
            {
                var history = await _chatHistoryService.GetChatHistoryBySessionAsync(userId, sessionId, limit);

                return Ok(new
                {
                    status = "success",
                    sessionId = sessionId,
                    count = history.Count,
                    messages = history.Select(h => new
                    {
                        id = h.Id,
                        message = h.Message,
                        reply = h.Reply,
                        timestamp = h.Timestamp,
                        contextCount = h.ContextCount
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chat history");
                return StatusCode(500, new { status = "error", message = "Failed to retrieve history." });
            }
        }

        /// <summary>
        /// Get all chat sessions for a user
        /// GET /api/chat/sessions
        /// </summary>
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions([FromQuery] int limit = 20)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string userId = ip;

            _logger.LogInformation("Retrieving chat sessions for user {UserId}", userId);

            try
            {
                var sessions = await _chatHistoryService.GetUserChatSessionsAsync(userId, limit);

                return Ok(new
                {
                    status = "success",
                    count = sessions.Count,
                    sessions = sessions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chat sessions");
                return StatusCode(500, new { status = "error", message = "Failed to retrieve sessions." });
            }
        }

        [HttpGet("test")]
        public IActionResult Test() => Ok("✅ Chat endpoint is working!");
    }

    public class ChatAskRequest
    {
        [Required(ErrorMessage = "Question is required.")]
        public string Question { get; set; } = string.Empty;
        
        /// <summary>
        /// Session ID for conversation continuity. If not provided, a new session is created.
        /// </summary>
        public string? SessionId { get; set; }
    }
}
