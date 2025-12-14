using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Features.Exams;

namespace SchoolAiChatbotBackend.Tests
{
    /// <summary>
    /// Quick test to verify ExamService functionality
    /// Run this by uncommenting the test method calls
    /// </summary>
    public class ExamServiceManualTest
    {
        public static async Task TestExamFlow(IServiceProvider serviceProvider)
        {
            var examService = serviceProvider.GetRequiredService<IExamService>();
            var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

            Console.WriteLine("=== Testing Exam System ===\n");

            // 1. Create sample questions
            Console.WriteLine("Creating sample questions...");
            var question1 = new Question
            {
                Subject = "Mathematics",
                Chapter = "Algebra",
                Topic = "Linear Equations",
                Text = "What is the solution to 2x + 4 = 10?",
                Explanation = "Subtract 4 from both sides, then divide by 2",
                Difficulty = "Easy",
                Type = "MultipleChoice"
            };

            var question2 = new Question
            {
                Subject = "Mathematics",
                Chapter = "Algebra",
                Topic = "Quadratic Equations",
                Text = "What is the discriminant of x² + 5x + 6 = 0?",
                Explanation = "Discriminant = b² - 4ac = 25 - 24 = 1",
                Difficulty = "Medium",
                Type = "MultipleChoice"
            };

            var question3 = new Question
            {
                Subject = "Mathematics",
                Chapter = "Algebra",
                Topic = "Complex Numbers",
                Text = "What is i² equal to?",
                Explanation = "By definition, i² = -1",
                Difficulty = "Hard",
                Type = "MultipleChoice"
            };

            dbContext.Questions.AddRange(question1, question2, question3);
            await dbContext.SaveChangesAsync();

            // Add options
            var options1 = new[]
            {
                new QuestionOption { QuestionId = question1.Id, OptionText = "x = 3", IsCorrect = true },
                new QuestionOption { QuestionId = question1.Id, OptionText = "x = 4", IsCorrect = false },
                new QuestionOption { QuestionId = question1.Id, OptionText = "x = 5", IsCorrect = false }
            };

            var options2 = new[]
            {
                new QuestionOption { QuestionId = question2.Id, OptionText = "1", IsCorrect = true },
                new QuestionOption { QuestionId = question2.Id, OptionText = "0", IsCorrect = false },
                new QuestionOption { QuestionId = question2.Id, OptionText = "-1", IsCorrect = false }
            };

            var options3 = new[]
            {
                new QuestionOption { QuestionId = question3.Id, OptionText = "-1", IsCorrect = true },
                new QuestionOption { QuestionId = question3.Id, OptionText = "1", IsCorrect = false },
                new QuestionOption { QuestionId = question3.Id, OptionText = "0", IsCorrect = false }
            };

            dbContext.QuestionOptions.AddRange(options1);
            dbContext.QuestionOptions.AddRange(options2);
            dbContext.QuestionOptions.AddRange(options3);
            await dbContext.SaveChangesAsync();

            Console.WriteLine($"Created 3 questions with options\n");

            // 2. Create exam template
            Console.WriteLine("Creating exam template...");
            var template = await examService.CreateExamTemplateAsync(
                name: "Algebra Basics Test",
                subject: "Mathematics",
                chapter: "Algebra",
                totalQuestions: 3,
                durationMinutes: 30,
                adaptiveEnabled: true,
                createdBy: "test-teacher");

            Console.WriteLine($"Created exam template: {template.Name} (ID: {template.Id})\n");

            // 3. Start exam
            Console.WriteLine("Starting exam for student...");
            var (attempt, firstQuestion) = await examService.StartExamAsync("student-123", template.Id);
            Console.WriteLine($"Exam started - Attempt ID: {attempt.Id}");
            Console.WriteLine($"First question (Difficulty: {firstQuestion?.Difficulty}): {firstQuestion?.Text}\n");

            // 4. Submit answers
            if (firstQuestion != null)
            {
                Console.WriteLine("Submitting answer 1 (correct)...");
                var correctOption = dbContext.QuestionOptions
                    .FirstOrDefault(o => o.QuestionId == firstQuestion.Id && o.IsCorrect);

                var (answer1, nextQ1, complete1) = await examService.SubmitAnswerAsync(
                    attempt.Id, firstQuestion.Id, correctOption?.Id, 45);

                Console.WriteLine($"Answer submitted - Correct: {answer1.IsCorrect}");
                Console.WriteLine($"Next question (Difficulty: {nextQ1?.Difficulty}): {nextQ1?.Text}\n");

                if (nextQ1 != null && !complete1)
                {
                    Console.WriteLine("Submitting answer 2 (correct)...");
                    var correctOption2 = dbContext.QuestionOptions
                        .FirstOrDefault(o => o.QuestionId == nextQ1.Id && o.IsCorrect);

                    var (answer2, nextQ2, complete2) = await examService.SubmitAnswerAsync(
                        attempt.Id, nextQ1.Id, correctOption2?.Id, 60);

                    Console.WriteLine($"Answer submitted - Correct: {answer2.IsCorrect}");
                    Console.WriteLine($"Next question (Difficulty: {nextQ2?.Difficulty}): {nextQ2?.Text}\n");

                    if (nextQ2 != null && !complete2)
                    {
                        Console.WriteLine("Submitting answer 3 (correct)...");
                        var correctOption3 = dbContext.QuestionOptions
                            .FirstOrDefault(o => o.QuestionId == nextQ2.Id && o.IsCorrect);

                        var (answer3, nextQ3, complete3) = await examService.SubmitAnswerAsync(
                            attempt.Id, nextQ2.Id, correctOption3?.Id, 90);

                        Console.WriteLine($"Answer submitted - Correct: {answer3.IsCorrect}");
                        Console.WriteLine($"Exam complete: {complete3}\n");
                    }
                }
            }

            // 5. Complete exam
            Console.WriteLine("Completing exam and calculating score...");
            var completedAttempt = await examService.CompleteExamAsync(attempt.Id);

            Console.WriteLine($"\n=== Exam Results ===");
            Console.WriteLine($"Status: {completedAttempt.Status}");
            Console.WriteLine($"Correct: {completedAttempt.CorrectCount}");
            Console.WriteLine($"Wrong: {completedAttempt.WrongCount}");
            Console.WriteLine($"Score: {completedAttempt.ScorePercent}%");
            Console.WriteLine($"Duration: {(completedAttempt.CompletedAt - completedAttempt.StartedAt)?.TotalSeconds} seconds");

            Console.WriteLine("\n=== Test Complete ===");
        }
    }
}
