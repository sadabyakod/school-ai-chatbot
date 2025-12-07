using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Features.Exams;

namespace SchoolAiChatbotBackend.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(AppDbContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seed sample questions for exam system
    /// </summary>
    [HttpPost("seed-questions")]
    public async Task<IActionResult> SeedQuestions()
    {
        try
        {
            // Check if questions already exist
            if (await _context.Questions.AnyAsync())
            {
                return Ok(new { message = "Questions already exist", count = await _context.Questions.CountAsync() });
            }

            var questions = new List<Question>
            {
                new Question
                {
                    Subject = "Mathematics",
                    Chapter = "Algebra",
                    Topic = "Linear Equations",
                    Text = "What is the value of x in the equation 2x + 5 = 15?",
                    Type = "MCQ",
                    Difficulty = "Easy",
                    Options = new List<QuestionOption>
                    {
                        new QuestionOption { OptionText = "5", IsCorrect = true },
                        new QuestionOption { OptionText = "10", IsCorrect = false },
                        new QuestionOption { OptionText = "15", IsCorrect = false },
                        new QuestionOption { OptionText = "20", IsCorrect = false }
                    }
                },
                new Question
                {
                    Subject = "Mathematics",
                    Chapter = "Algebra",
                    Topic = "Linear Equations",
                    Text = "Solve for y: 3y - 7 = 14",
                    Type = "MCQ",
                    Difficulty = "Easy",
                    Options = new List<QuestionOption>
                    {
                        new QuestionOption { OptionText = "5", IsCorrect = false },
                        new QuestionOption { OptionText = "7", IsCorrect = true },
                        new QuestionOption { OptionText = "9", IsCorrect = false },
                        new QuestionOption { OptionText = "11", IsCorrect = false }
                    }
                },
                new Question
                {
                    Subject = "Mathematics",
                    Chapter = "Algebra",
                    Topic = "Quadratic Equations",
                    Text = "Solve for x: x² - 5x + 6 = 0",
                    Type = "MCQ",
                    Difficulty = "Medium",
                    Options = new List<QuestionOption>
                    {
                        new QuestionOption { OptionText = "x = 2 or x = 3", IsCorrect = true },
                        new QuestionOption { OptionText = "x = 1 or x = 6", IsCorrect = false },
                        new QuestionOption { OptionText = "x = -2 or x = -3", IsCorrect = false },
                        new QuestionOption { OptionText = "x = 5 or x = 1", IsCorrect = false }
                    }
                },
                new Question
                {
                    Subject = "Mathematics",
                    Chapter = "Algebra",
                    Topic = "Systems of Equations",
                    Text = "What is the value of x in: x + y = 10 and x - y = 4?",
                    Type = "MCQ",
                    Difficulty = "Medium",
                    Options = new List<QuestionOption>
                    {
                        new QuestionOption { OptionText = "6", IsCorrect = false },
                        new QuestionOption { OptionText = "7", IsCorrect = true },
                        new QuestionOption { OptionText = "8", IsCorrect = false },
                        new QuestionOption { OptionText = "9", IsCorrect = false }
                    }
                },
                new Question
                {
                    Subject = "Mathematics",
                    Chapter = "Algebra",
                    Topic = "Complex Equations",
                    Text = "Solve: 2x² + 7x - 15 = 0",
                    Type = "MCQ",
                    Difficulty = "Hard",
                    Options = new List<QuestionOption>
                    {
                        new QuestionOption { OptionText = "x = 1.5 or x = -5", IsCorrect = true },
                        new QuestionOption { OptionText = "x = 3 or x = -2.5", IsCorrect = false },
                        new QuestionOption { OptionText = "x = 5 or x = -1.5", IsCorrect = false },
                        new QuestionOption { OptionText = "x = 2 or x = -7.5", IsCorrect = false }
                    }
                },
                new Question
                {
                    Subject = "Science",
                    Chapter = "Physics",
                    Topic = "Force and Motion",
                    Text = "What is the SI unit of force?",
                    Type = "MCQ",
                    Difficulty = "Easy",
                    Options = new List<QuestionOption>
                    {
                        new QuestionOption { OptionText = "Newton", IsCorrect = true },
                        new QuestionOption { OptionText = "Joule", IsCorrect = false },
                        new QuestionOption { OptionText = "Watt", IsCorrect = false },
                        new QuestionOption { OptionText = "Pascal", IsCorrect = false }
                    }
                },
                new Question
                {
                    Subject = "Science",
                    Chapter = "Physics",
                    Topic = "Newton's Laws",
                    Text = "A 5kg object accelerates at 2 m/s². What is the net force?",
                    Type = "MCQ",
                    Difficulty = "Medium",
                    Options = new List<QuestionOption>
                    {
                        new QuestionOption { OptionText = "7 N", IsCorrect = false },
                        new QuestionOption { OptionText = "10 N", IsCorrect = true },
                        new QuestionOption { OptionText = "12 N", IsCorrect = false },
                        new QuestionOption { OptionText = "15 N", IsCorrect = false }
                    }
                },
                new Question
                {
                    Subject = "Science",
                    Chapter = "Physics",
                    Topic = "Energy Conservation",
                    Text = "A 2kg ball is dropped from 10m height. What is its velocity just before impact? (g=10 m/s²)",
                    Type = "MCQ",
                    Difficulty = "Hard",
                    Options = new List<QuestionOption>
                    {
                        new QuestionOption { OptionText = "10 m/s", IsCorrect = false },
                        new QuestionOption { OptionText = "14.1 m/s", IsCorrect = true },
                        new QuestionOption { OptionText = "20 m/s", IsCorrect = false },
                        new QuestionOption { OptionText = "28.3 m/s", IsCorrect = false }
                    }
                }
            };

            _context.Questions.AddRange(questions);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Seeded {Count} questions successfully", questions.Count);

            return Ok(new { 
                message = "Questions seeded successfully",
                count = questions.Count,
                subjects = questions.Select(q => q.Subject).Distinct().ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding questions");
            return StatusCode(500, new { error = "Failed to seed questions", details = ex.Message });
        }
    }
}
