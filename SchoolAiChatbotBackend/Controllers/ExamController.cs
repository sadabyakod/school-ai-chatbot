using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Features.Exams;
using Microsoft.AspNetCore.Http;

namespace SchoolAiChatbotBackend.Controllers;

[ApiController]
[Route("api/exam")]
[Produces("application/json")]
[Tags("Exam System")]
public class ExamController : ControllerBase
{
    private readonly IExamService _examService;
    private readonly AppDbContext _context;
    private readonly ILogger<ExamController> _logger;

    public ExamController(IExamService examService, AppDbContext context, ILogger<ExamController> logger)
    {
        _examService = examService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/exams/templates - Create a new exam template
    /// </summary>
    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateExamTemplateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Exam name is required.");

            if (string.IsNullOrWhiteSpace(request.Subject))
                return BadRequest("Subject is required.");

            if (request.TotalQuestions <= 0)
                return BadRequest("Total questions must be greater than 0.");

            if (request.DurationMinutes <= 0)
                return BadRequest("Duration must be greater than 0.");

            var template = await _examService.CreateExamTemplateAsync(
                request.Name,
                request.Subject,
                request.Chapter,
                request.TotalQuestions,
                request.DurationMinutes,
                request.AdaptiveEnabled,
                "System" // Default creator
            );

            var dto = new ExamTemplateDto
            {
                Id = template.Id,
                Name = template.Name,
                Subject = template.Subject,
                Chapter = template.Chapter,
                TotalQuestions = template.TotalQuestions,
                DurationMinutes = template.DurationMinutes,
                AdaptiveEnabled = template.AdaptiveEnabled,
                CreatedAt = template.CreatedAt
            };

            _logger.LogInformation("Exam template created: {TemplateName} ({TemplateId})", template.Name, template.Id);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating exam template");
            return StatusCode(500, "Failed to create exam template.");
        }
    }

    /// <summary>
    /// POST /api/exams/start - Start a new exam attempt
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartExam([FromBody] StartExamRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.StudentId))
                return BadRequest("Student ID is required.");

            if (request.ExamTemplateId <= 0)
                return BadRequest("Valid exam template ID is required.");

            // Verify template exists
            var template = await _context.ExamTemplates.FindAsync(request.ExamTemplateId);
            if (template == null)
                return NotFound($"Exam template {request.ExamTemplateId} not found.");

            var (attempt, firstQuestion) = await _examService.StartExamAsync(request.StudentId, request.ExamTemplateId);

            var response = new StartExamResponse
            {
                AttemptId = attempt.Id,
                Template = new ExamTemplateDto
                {
                    Id = template.Id,
                    Name = template.Name,
                    Subject = template.Subject,
                    Chapter = template.Chapter,
                    TotalQuestions = template.TotalQuestions,
                    DurationMinutes = template.DurationMinutes,
                    AdaptiveEnabled = template.AdaptiveEnabled,
                    CreatedAt = template.CreatedAt
                },
                FirstQuestion = firstQuestion != null ? MapToQuestionDto(firstQuestion) : null
            };

            _logger.LogInformation("Exam started: StudentId={StudentId}, AttemptId={AttemptId}", request.StudentId, attempt.Id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting exam");
            return StatusCode(500, "Failed to start exam.");
        }
    }

    /// <summary>
    /// POST /api/exams/{attemptId}/answer - Submit an answer and get next question
    /// </summary>
    [HttpPost("{attemptId}/answer")]
    public async Task<IActionResult> SubmitAnswer(int attemptId, [FromBody] SubmitAnswerRequest request)
    {
        try
        {
            if (attemptId <= 0)
                return BadRequest("Valid attempt ID is required.");

            if (request.QuestionId <= 0)
                return BadRequest("Valid question ID is required.");

            // Verify attempt exists and is in progress
            var attempt = await _context.ExamAttempts
                .Include(a => a.ExamTemplate)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt == null)
                return NotFound($"Exam attempt {attemptId} not found.");

            if (attempt.Status != "InProgress")
                return BadRequest($"Exam attempt is not in progress. Status: {attempt.Status}");

            var (answer, nextQuestion, isExamComplete) = await _examService.SubmitAnswerAsync(
                attemptId,
                request.QuestionId,
                request.SelectedOptionId,
                request.TimeTakenSeconds
            );

            // Get current stats
            var answers = await _context.ExamAnswers
                .Where(a => a.ExamAttemptId == attemptId)
                .ToListAsync();

            var correctCount = answers.Count(a => a.IsCorrect);
            var wrongCount = answers.Count(a => !a.IsCorrect);
            var answeredCount = answers.Count;

            var currentStats = new CurrentStatsDto
            {
                AnsweredCount = answeredCount,
                CorrectCount = correctCount,
                WrongCount = wrongCount,
                CurrentAccuracy = answeredCount > 0 ? (decimal)correctCount / answeredCount * 100 : 0
            };

            // Complete exam if finished
            if (isExamComplete)
            {
                await _examService.CompleteExamAsync(attemptId);
            }

            var response = new SubmitAnswerResponse
            {
                IsCorrect = answer.IsCorrect,
                IsCompleted = isExamComplete,
                NextQuestion = nextQuestion != null ? MapToQuestionDto(nextQuestion) : null,
                CurrentStats = currentStats
            };

            _logger.LogInformation("Answer submitted: AttemptId={AttemptId}, QuestionId={QuestionId}, Correct={IsCorrect}", 
                attemptId, request.QuestionId, answer.IsCorrect);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting answer for attempt {AttemptId}", attemptId);
            return StatusCode(500, "Failed to submit answer.");
        }
    }

    /// <summary>
    /// GET /api/exams/{attemptId}/summary - Get exam summary with statistics
    /// </summary>
    [HttpGet("{attemptId}/summary")]
    public async Task<IActionResult> GetExamSummary(int attemptId)
    {
        try
        {
            if (attemptId <= 0)
                return BadRequest("Valid attempt ID is required.");

            var attempt = await _context.ExamAttempts
                .Include(a => a.ExamTemplate)
                .Include(a => a.ExamAnswers)
                    .ThenInclude(ans => ans.Question)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt == null)
                return NotFound($"Exam attempt {attemptId} not found.");

            // Complete exam if still in progress
            if (attempt.Status == "InProgress")
            {
                await _examService.CompleteExamAsync(attemptId);
                // Refresh the attempt data
                await _context.Entry(attempt).ReloadAsync();
            }

            // Calculate per-difficulty stats
            var perDifficultyStats = new Dictionary<string, DifficultyStatsDto>();
            var difficulties = new[] { "Easy", "Medium", "Hard" };

            foreach (var difficulty in difficulties)
            {
                var difficultyAnswers = attempt.ExamAnswers
                    .Where(a => a.Question != null && a.Question.Difficulty == difficulty)
                    .ToList();

                if (difficultyAnswers.Any())
                {
                    var correctInDifficulty = difficultyAnswers.Count(a => a.IsCorrect);
                    perDifficultyStats[difficulty] = new DifficultyStatsDto
                    {
                        TotalQuestions = difficultyAnswers.Count,
                        CorrectAnswers = correctInDifficulty,
                        Accuracy = difficultyAnswers.Count > 0 
                            ? (decimal)correctInDifficulty / difficultyAnswers.Count * 100 
                            : 0
                    };
                }
            }

            var response = new ExamSummaryResponse
            {
                AttemptId = attempt.Id,
                StudentId = attempt.StudentId,
                Template = new ExamTemplateDto
                {
                    Id = attempt.ExamTemplate.Id,
                    Name = attempt.ExamTemplate.Name,
                    Subject = attempt.ExamTemplate.Subject,
                    Chapter = attempt.ExamTemplate.Chapter,
                    TotalQuestions = attempt.ExamTemplate.TotalQuestions,
                    DurationMinutes = attempt.ExamTemplate.DurationMinutes,
                    AdaptiveEnabled = attempt.ExamTemplate.AdaptiveEnabled,
                    CreatedAt = attempt.ExamTemplate.CreatedAt
                },
                ScorePercent = attempt.ScorePercent ?? 0,
                CorrectCount = attempt.CorrectCount ?? 0,
                WrongCount = attempt.WrongCount ?? 0,
                TotalQuestions = attempt.ExamAnswers.Count,
                StartedAt = attempt.StartedAt,
                CompletedAt = attempt.CompletedAt,
                Status = attempt.Status,
                PerDifficultyStats = perDifficultyStats
            };

            _logger.LogInformation("Exam summary retrieved: AttemptId={AttemptId}, Score={Score}%", attemptId, attempt.ScorePercent);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving exam summary for attempt {AttemptId}", attemptId);
            return StatusCode(500, "Failed to retrieve exam summary.");
        }
    }

    /// <summary>
    /// GET /api/exams/history?studentId=... - Get student's exam history
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetExamHistory([FromQuery] string studentId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(studentId))
                return BadRequest("Student ID is required.");

            var attempts = await _context.ExamAttempts
                .Include(a => a.ExamTemplate)
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.StartedAt)
                .Take(20)
                .ToListAsync();

            var history = attempts.Select(a => new ExamHistoryDto
            {
                AttemptId = a.Id,
                ExamName = a.ExamTemplate.Name,
                Subject = a.ExamTemplate.Subject,
                Chapter = a.ExamTemplate.Chapter,
                ScorePercent = a.ScorePercent ?? 0,
                CorrectCount = a.CorrectCount ?? 0,
                WrongCount = a.WrongCount ?? 0,
                Status = a.Status,
                StartedAt = a.StartedAt,
                CompletedAt = a.CompletedAt
            }).ToList();

            _logger.LogInformation("Exam history retrieved: StudentId={StudentId}, Count={Count}", studentId, history.Count);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving exam history for student {StudentId}", studentId);
            return StatusCode(500, "Failed to retrieve exam history.");
        }
    }

    // Helper method to map Question entity to DTO
    private QuestionDto MapToQuestionDto(Question question)
    {
        return new QuestionDto
        {
            Id = question.Id,
            Subject = question.Subject,
            Chapter = question.Chapter,
            Topic = question.Topic,
            Text = question.Text,
            Type = question.Type,
            Difficulty = question.Difficulty,
            Options = question.Options?.Select(o => new QuestionOptionDto
            {
                Id = o.Id,
                OptionText = o.OptionText
            }).ToList() ?? new List<QuestionOptionDto>()
        };
    }
}
