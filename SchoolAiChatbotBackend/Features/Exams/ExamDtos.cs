namespace SchoolAiChatbotBackend.Features.Exams;

// Request DTOs
public class CreateExamTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? Chapter { get; set; }
    public int TotalQuestions { get; set; }
    public int DurationMinutes { get; set; }
    public bool AdaptiveEnabled { get; set; }
}

public class StartExamRequest
{
    public string StudentId { get; set; } = string.Empty;
    public int ExamTemplateId { get; set; }
}

public class SubmitAnswerRequest
{
    public int QuestionId { get; set; }
    public int? SelectedOptionId { get; set; }
    public int TimeTakenSeconds { get; set; }
}

// Response DTOs
public class ExamTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? Chapter { get; set; }
    public int TotalQuestions { get; set; }
    public int DurationMinutes { get; set; }
    public bool AdaptiveEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class QuestionOptionDto
{
    public int Id { get; set; }
    public string OptionText { get; set; } = string.Empty;
}

public class QuestionDto
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Chapter { get; set; }
    public string? Topic { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public List<QuestionOptionDto> Options { get; set; } = new();
}

public class StartExamResponse
{
    public int AttemptId { get; set; }
    public ExamTemplateDto Template { get; set; } = null!;
    public QuestionDto? FirstQuestion { get; set; }
}

public class SubmitAnswerResponse
{
    public bool IsCorrect { get; set; }
    public bool IsCompleted { get; set; }
    public QuestionDto? NextQuestion { get; set; }
    public CurrentStatsDto CurrentStats { get; set; } = null!;
}

public class CurrentStatsDto
{
    public int AnsweredCount { get; set; }
    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }
    public decimal CurrentAccuracy { get; set; }
}

public class ExamSummaryResponse
{
    public int AttemptId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public ExamTemplateDto Template { get; set; } = null!;
    public decimal ScorePercent { get; set; }
    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }
    public int TotalQuestions { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, DifficultyStatsDto> PerDifficultyStats { get; set; } = new();
}

public class DifficultyStatsDto
{
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public decimal Accuracy { get; set; }
}

public class ExamHistoryDto
{
    public int AttemptId { get; set; }
    public string ExamName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? Chapter { get; set; }
    public decimal ScorePercent { get; set; }
    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
