using Microsoft.EntityFrameworkCore;
using SchoolAiChatbotBackend.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolAiChatbotBackend.Features.Exams
{
    /// <summary>
    /// Service for managing exam functionality with adaptive difficulty
    /// </summary>
    public interface IExamService
    {
        Task<ExamTemplate> CreateExamTemplateAsync(string name, string subject, string? chapter, int totalQuestions, int durationMinutes, bool adaptiveEnabled, string? createdBy);
        Task<(ExamAttempt attempt, Question? firstQuestion)> StartExamAsync(string studentId, int templateId);
        Task<(ExamAnswer answer, Question? nextQuestion, bool isExamComplete)> SubmitAnswerAsync(int attemptId, int questionId, int? selectedOptionId, int? timeTakenSeconds);
        Task<ExamAttempt> CompleteExamAsync(int attemptId);
        Task<ExamAttempt?> GetExamAttemptAsync(int attemptId);
        Task<List<ExamAttempt>> GetStudentExamHistoryAsync(string studentId, int limit = 10);
    }

    public class ExamService : IExamService
    {
        private readonly AppDbContext _context;

        public ExamService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Create a new exam template
        /// </summary>
        public async Task<ExamTemplate> CreateExamTemplateAsync(
            string name,
            string subject,
            string? chapter,
            int totalQuestions,
            int durationMinutes,
            bool adaptiveEnabled,
            string? createdBy)
        {
            var template = new ExamTemplate
            {
                Name = name,
                Subject = subject,
                Chapter = chapter,
                TotalQuestions = totalQuestions,
                DurationMinutes = durationMinutes,
                AdaptiveEnabled = adaptiveEnabled,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.ExamTemplates.Add(template);
            await _context.SaveChangesAsync();

            return template;
        }

        /// <summary>
        /// Start a new exam attempt for a student
        /// </summary>
        public async Task<(ExamAttempt attempt, Question? firstQuestion)> StartExamAsync(string studentId, int templateId)
        {
            // Verify template exists
            var template = await _context.ExamTemplates.FindAsync(templateId);
            if (template == null)
                throw new ArgumentException($"Exam template with ID {templateId} not found.");

            // Create exam attempt
            var attempt = new ExamAttempt
            {
                StudentId = studentId,
                ExamTemplateId = templateId,
                StartedAt = DateTime.UtcNow,
                Status = "InProgress"
            };

            _context.ExamAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            // Pick first question - start with Medium difficulty
            var firstQuestion = await GetNextQuestionAsync(template.Subject, template.Chapter, "Medium", new List<int>());

            return (attempt, firstQuestion);
        }

        /// <summary>
        /// Submit an answer and get the next question using adaptive logic
        /// </summary>
        public async Task<(ExamAnswer answer, Question? nextQuestion, bool isExamComplete)> SubmitAnswerAsync(
            int attemptId,
            int questionId,
            int? selectedOptionId,
            int? timeTakenSeconds)
        {
            // Load attempt with template and existing answers
            var attempt = await _context.ExamAttempts
                .Include(a => a.ExamTemplate)
                .Include(a => a.ExamAnswers)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt == null)
                throw new ArgumentException($"Exam attempt with ID {attemptId} not found.");

            if (attempt.Status != "InProgress")
                throw new InvalidOperationException("Cannot submit answer - exam is not in progress.");

            // Get the question with options to check correctness
            var question = await _context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
                throw new ArgumentException($"Question with ID {questionId} not found.");

            // Determine if answer is correct
            bool isCorrect = false;
            if (selectedOptionId.HasValue)
            {
                var selectedOption = question.Options.FirstOrDefault(o => o.Id == selectedOptionId.Value);
                isCorrect = selectedOption?.IsCorrect ?? false;
            }

            // Save the answer
            var examAnswer = new ExamAnswer
            {
                ExamAttemptId = attemptId,
                QuestionId = questionId,
                SelectedOptionId = selectedOptionId,
                IsCorrect = isCorrect,
                TimeTakenSeconds = timeTakenSeconds
            };

            _context.ExamAnswers.Add(examAnswer);
            await _context.SaveChangesAsync();

            // Check if exam is complete
            var answeredQuestionIds = await _context.ExamAnswers
                .Where(a => a.ExamAttemptId == attemptId)
                .Select(a => a.QuestionId)
                .ToListAsync();

            bool isExamComplete = answeredQuestionIds.Count >= attempt.ExamTemplate.TotalQuestions;

            if (isExamComplete)
            {
                return (examAnswer, null, true);
            }

            // Get next question using adaptive logic
            string nextDifficulty = "Medium";

            if (attempt.ExamTemplate.AdaptiveEnabled)
            {
                nextDifficulty = CalculateNextDifficulty(attemptId);
            }

            var nextQuestion = await GetNextQuestionAsync(
                attempt.ExamTemplate.Subject,
                attempt.ExamTemplate.Chapter,
                nextDifficulty,
                answeredQuestionIds);

            return (examAnswer, nextQuestion, false);
        }

        /// <summary>
        /// Calculate next difficulty based on last 5 answers
        /// </summary>
        private string CalculateNextDifficulty(int attemptId)
        {
            var last5Answers = _context.ExamAnswers
                .Where(a => a.ExamAttemptId == attemptId)
                .OrderByDescending(a => a.Id)
                .Take(5)
                .Select(a => a.IsCorrect)
                .ToList();

            if (last5Answers.Count == 0)
                return "Medium";

            // Calculate accuracy
            double accuracy = last5Answers.Count(x => x) / (double)last5Answers.Count;

            // Adaptive logic
            if (accuracy > 0.8)
                return "Hard";
            else if (accuracy >= 0.5)
                return "Medium";
            else
                return "Easy";
        }

        /// <summary>
        /// Get next question based on difficulty and subject, excluding already answered
        /// </summary>
        private async Task<Question?> GetNextQuestionAsync(
            string subject,
            string? chapter,
            string difficulty,
            List<int> excludeQuestionIds)
        {
            var query = _context.Questions
                .Include(q => q.Options)
                .Where(q => q.Subject == subject && q.Difficulty == difficulty);

            if (!string.IsNullOrEmpty(chapter))
            {
                query = query.Where(q => q.Chapter == chapter);
            }

            if (excludeQuestionIds.Any())
            {
                query = query.Where(q => !excludeQuestionIds.Contains(q.Id));
            }

            // Get random question
            var questions = await query.ToListAsync();
            if (!questions.Any())
            {
                // Fallback: try without difficulty filter
                query = _context.Questions
                    .Include(q => q.Options)
                    .Where(q => q.Subject == subject);

                if (!string.IsNullOrEmpty(chapter))
                {
                    query = query.Where(q => q.Chapter == chapter);
                }

                if (excludeQuestionIds.Any())
                {
                    query = query.Where(q => !excludeQuestionIds.Contains(q.Id));
                }

                questions = await query.ToListAsync();
            }

            if (!questions.Any())
                return null;

            // Return random question
            var random = new Random();
            return questions[random.Next(questions.Count)];
        }

        /// <summary>
        /// Complete an exam and calculate final statistics
        /// </summary>
        public async Task<ExamAttempt> CompleteExamAsync(int attemptId)
        {
            var attempt = await _context.ExamAttempts
                .Include(a => a.ExamAnswers)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt == null)
                throw new ArgumentException($"Exam attempt with ID {attemptId} not found.");

            if (attempt.Status == "Completed")
                return attempt;

            // Calculate statistics
            var totalAnswers = attempt.ExamAnswers.Count;
            var correctCount = attempt.ExamAnswers.Count(a => a.IsCorrect);
            var wrongCount = totalAnswers - correctCount;

            attempt.CorrectCount = correctCount;
            attempt.WrongCount = wrongCount;
            attempt.ScorePercent = totalAnswers > 0
                ? Math.Round((decimal)correctCount / totalAnswers * 100, 2)
                : 0;
            attempt.CompletedAt = DateTime.UtcNow;
            attempt.Status = "Completed";

            await _context.SaveChangesAsync();

            return attempt;
        }

        /// <summary>
        /// Get exam attempt with all details
        /// </summary>
        public async Task<ExamAttempt?> GetExamAttemptAsync(int attemptId)
        {
            return await _context.ExamAttempts
                .Include(a => a.ExamTemplate)
                .Include(a => a.ExamAnswers)
                    .ThenInclude(ans => ans.Question)
                        .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(a => a.Id == attemptId);
        }

        /// <summary>
        /// Get student's exam history
        /// </summary>
        public async Task<List<ExamAttempt>> GetStudentExamHistoryAsync(string studentId, int limit = 10)
        {
            return await _context.ExamAttempts
                .Include(a => a.ExamTemplate)
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.StartedAt)
                .Take(limit)
                .ToListAsync();
        }
    }
}
