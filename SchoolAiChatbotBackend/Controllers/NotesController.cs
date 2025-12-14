using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SchoolAiChatbotBackend.Models;
using SchoolAiChatbotBackend.Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolAiChatbotBackend.Controllers
{
    /// <summary>
    /// Study Notes Controller
    /// Migrated from Azure Functions GenerateStudyNotes feature
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class NotesController : ControllerBase
    {
        private readonly IStudyNotesService _notesService;
        private readonly ILogger<NotesController> _logger;

        public NotesController(
            IStudyNotesService notesService,
            ILogger<NotesController> logger)
        {
            _notesService = notesService;
            _logger = logger;
        }

        /// <summary>
        /// Generate study notes for a topic using AI and RAG
        /// POST /api/notes/generate
        /// </summary>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateNotes([FromBody] GenerateNotesRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string userId = ip; // Replace with authenticated user ID if available

            _logger.LogInformation("Generating study notes for user {UserId}, topic: {Topic}", userId, request.Topic);

            try
            {
                var studyNote = await _notesService.GenerateStudyNotesAsync(
                    userId,
                    request.Topic,
                    request.Subject,
                    request.Grade,
                    request.Chapter
                );

                return Ok(new
                {
                    status = "success",
                    noteId = studyNote.Id,
                    topic = studyNote.Topic,
                    notes = studyNote.GeneratedNotes,
                    subject = studyNote.Subject,
                    grade = studyNote.Grade,
                    chapter = studyNote.Chapter,
                    createdAt = studyNote.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating study notes for topic: {Topic}", request.Topic);
                return StatusCode(500, new
                {
                    status = "error",
                    message = "Failed to generate study notes. Please try again.",
                    debug = ex.Message
                });
            }
        }

        /// <summary>
        /// Get user's study notes history
        /// GET /api/notes
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNotes([FromQuery] int limit = 20)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string userId = ip;

            _logger.LogInformation("Retrieving study notes for user {UserId}", userId);

            try
            {
                var notes = await _notesService.GetUserStudyNotesAsync(userId, limit);

                return Ok(new
                {
                    status = "success",
                    count = notes.Count,
                    notes = notes.Select(n => new
                    {
                        id = n.Id,
                        topic = n.Topic,
                        subject = n.Subject,
                        grade = n.Grade,
                        chapter = n.Chapter,
                        createdAt = n.CreatedAt,
                        rating = n.Rating,
                        preview = n.GeneratedNotes.Length > 200
                            ? n.GeneratedNotes.Substring(0, 200) + "..."
                            : n.GeneratedNotes
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving study notes");
                return StatusCode(500, new { status = "error", message = "Failed to retrieve notes." });
            }
        }

        /// <summary>
        /// Get a specific study note by ID
        /// GET /api/notes/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNoteById(int id)
        {
            _logger.LogInformation("Retrieving study note with ID {NoteId}", id);

            try
            {
                var note = await _notesService.GetStudyNoteByIdAsync(id);

                if (note == null)
                    return NotFound(new { status = "error", message = "Study note not found." });

                return Ok(new
                {
                    status = "success",
                    note = new
                    {
                        id = note.Id,
                        topic = note.Topic,
                        notes = note.GeneratedNotes,
                        subject = note.Subject,
                        grade = note.Grade,
                        chapter = note.Chapter,
                        createdAt = note.CreatedAt,
                        rating = note.Rating
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving study note {NoteId}", id);
                return StatusCode(500, new { status = "error", message = "Failed to retrieve note." });
            }
        }

        /// <summary>
        /// Rate a study note
        /// POST /api/notes/{id}/rate
        /// </summary>
        [HttpPost("{id}/rate")]
        public async Task<IActionResult> RateNote(int id, [FromBody] RateNoteRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _logger.LogInformation("Rating study note {NoteId} with {Rating} stars", id, request.Rating);

            try
            {
                var success = await _notesService.RateStudyNoteAsync(id, request.Rating);

                if (!success)
                    return NotFound(new { status = "error", message = "Study note not found or invalid rating." });

                return Ok(new { status = "success", message = "Rating saved successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rating study note {NoteId}", id);
                return StatusCode(500, new { status = "error", message = "Failed to save rating." });
            }
        }

        /// <summary>
        /// Update/edit a study note
        /// PUT /api/notes/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNote(int id, [FromBody] UpdateNoteRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string userId = ip;

            _logger.LogInformation("Updating study note {NoteId} for user {UserId}", id, userId);

            try
            {
                var updatedNote = await _notesService.UpdateStudyNoteAsync(id, userId, request.Content);

                if (updatedNote == null)
                    return NotFound(new { status = "error", message = "Study note not found or you don't have permission to edit it." });

                return Ok(new
                {
                    status = "success",
                    message = "Note updated successfully.",
                    note = new
                    {
                        id = updatedNote.Id,
                        topic = updatedNote.Topic,
                        notes = updatedNote.GeneratedNotes,
                        updatedAt = updatedNote.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating study note {NoteId}", id);
                return StatusCode(500, new { status = "error", message = "Failed to update note." });
            }
        }

        /// <summary>
        /// Share a study note and get shareable link
        /// POST /api/notes/{id}/share
        /// </summary>
        [HttpPost("{id}/share")]
        public async Task<IActionResult> ShareNote(int id)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string userId = ip;

            _logger.LogInformation("Sharing study note {NoteId} for user {UserId}", id, userId);

            try
            {
                var shareToken = await _notesService.ShareStudyNoteAsync(id, userId);

                if (shareToken == null)
                    return NotFound(new { status = "error", message = "Study note not found or you don't have permission to share it." });

                var shareUrl = $"{Request.Scheme}://{Request.Host}/api/notes/shared/{shareToken}";

                return Ok(new
                {
                    status = "success",
                    message = "Note shared successfully.",
                    shareToken,
                    shareUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sharing study note {NoteId}", id);
                return StatusCode(500, new { status = "error", message = "Failed to share note." });
            }
        }

        /// <summary>
        /// Unshare a study note (revoke public access)
        /// POST /api/notes/{id}/unshare
        /// </summary>
        [HttpPost("{id}/unshare")]
        public async Task<IActionResult> UnshareNote(int id)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string userId = ip;

            _logger.LogInformation("Unsharing study note {NoteId} for user {UserId}", id, userId);

            try
            {
                var success = await _notesService.UnshareStudyNoteAsync(id, userId);

                if (!success)
                    return NotFound(new { status = "error", message = "Study note not found or you don't have permission to unshare it." });

                return Ok(new { status = "success", message = "Note unshared successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsharing study note {NoteId}", id);
                return StatusCode(500, new { status = "error", message = "Failed to unshare note." });
            }
        }

        /// <summary>
        /// Get a shared study note by token (public access)
        /// GET /api/notes/shared/{token}
        /// </summary>
        [HttpGet("shared/{token}")]
        public async Task<IActionResult> GetSharedNote(string token)
        {
            _logger.LogInformation("Retrieving shared study note with token {Token}", token);

            try
            {
                var note = await _notesService.GetSharedStudyNoteAsync(token);

                if (note == null)
                    return NotFound(new { status = "error", message = "Shared note not found or has been unshared." });

                return Ok(new
                {
                    status = "success",
                    note = new
                    {
                        id = note.Id,
                        topic = note.Topic,
                        notes = note.GeneratedNotes,
                        subject = note.Subject,
                        grade = note.Grade,
                        chapter = note.Chapter,
                        createdAt = note.CreatedAt,
                        updatedAt = note.UpdatedAt,
                        rating = note.Rating
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shared study note with token {Token}", token);
                return StatusCode(500, new { status = "error", message = "Failed to retrieve shared note." });
            }
        }

        [HttpGet("test")]
        public IActionResult Test() => Ok("✅ Notes endpoint is working!");
    }

    public class GenerateNotesRequest
    {
        [Required(ErrorMessage = "Topic is required.")]
        public string Topic { get; set; } = string.Empty;

        public string? Subject { get; set; }
        public string? Grade { get; set; }
        public string? Chapter { get; set; }
    }

    public class UpdateNoteRequest
    {
        [Required(ErrorMessage = "Content is required.")]
        public string Content { get; set; } = string.Empty;
    }

    public class RateNoteRequest
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }
    }
}
