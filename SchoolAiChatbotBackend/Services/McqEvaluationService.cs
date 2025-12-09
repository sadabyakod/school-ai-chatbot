using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SchoolAiChatbotBackend.Controllers;
using SchoolAiChatbotBackend.DTOs;
using SchoolAiChatbotBackend.Models;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// Service for evaluating MCQ answers extracted from uploaded answer sheets
    /// </summary>
    public class McqEvaluationService : IMcqEvaluationService
    {
        private readonly ILogger<McqEvaluationService> _logger;

        public McqEvaluationService(ILogger<McqEvaluationService> logger)
        {
            _logger = logger;
        }

        public async Task<McqEvaluationFromSheet> EvaluateExtractedAnswersAsync(
            McqExtraction extraction,
            GeneratedExamResponse exam)
        {
            _logger.LogInformation(
                "Starting MCQ evaluation for extraction {ExtractionId}, exam {ExamId}",
                extraction.McqExtractionId,
                extraction.ExamId);

            var evaluation = new McqEvaluationFromSheet
            {
                McqExtractionId = extraction.McqExtractionId,
                ExamId = extraction.ExamId,
                StudentId = extraction.StudentId
            };

            try
            {
                // Get all MCQ questions from the exam
                var mcqQuestions = GetMcqQuestionsWithDetails(exam);

                if (mcqQuestions.Count == 0)
                {
                    _logger.LogWarning("No MCQ questions found in exam {ExamId}", exam.ExamId);
                    return evaluation;
                }

                _logger.LogInformation(
                    "Found {Count} MCQ questions in exam {ExamId}",
                    mcqQuestions.Count,
                    exam.ExamId);

                // Match extracted answers with exam questions
                int totalScore = 0;
                int totalMarks = 0;

                foreach (var question in mcqQuestions)
                {
                    totalMarks += question.Marks;

                    // Find extracted answer for this question number
                    var extractedAnswer = extraction.ExtractedAnswers
                        .FirstOrDefault(a => a.QuestionNumber == question.QuestionNumber);

                    var answerEvaluation = new McqAnswerEvaluation
                    {
                        QuestionId = question.QuestionId,
                        QuestionNumber = question.QuestionNumber,
                        QuestionText = question.QuestionText,
                        Options = question.Options,
                        CorrectAnswer = question.CorrectAnswer,
                        Marks = question.Marks,
                        WasExtracted = extractedAnswer != null
                    };

                    if (extractedAnswer != null)
                    {
                        // Student answer was extracted from sheet
                        answerEvaluation.StudentAnswer = extractedAnswer.ExtractedOption;
                        answerEvaluation.IsCorrect = extractedAnswer.ExtractedOption
                            .Equals(question.CorrectAnswer, StringComparison.OrdinalIgnoreCase);
                        answerEvaluation.MarksAwarded = answerEvaluation.IsCorrect ? question.Marks : 0;
                        totalScore += answerEvaluation.MarksAwarded;

                        _logger.LogDebug(
                            "Q{QuestionNumber}: Student={StudentAnswer}, Correct={CorrectAnswer}, IsCorrect={IsCorrect}",
                            question.QuestionNumber,
                            extractedAnswer.ExtractedOption,
                            question.CorrectAnswer,
                            answerEvaluation.IsCorrect);
                    }
                    else
                    {
                        // Answer not found in extraction
                        answerEvaluation.StudentAnswer = string.Empty;
                        answerEvaluation.IsCorrect = false;
                        answerEvaluation.MarksAwarded = 0;

                        _logger.LogWarning(
                            "Q{QuestionNumber} not found in extracted answers",
                            question.QuestionNumber);
                    }

                    evaluation.Evaluations.Add(answerEvaluation);
                }

                evaluation.TotalScore = totalScore;
                evaluation.TotalMarks = totalMarks;

                _logger.LogInformation(
                    "MCQ evaluation completed for extraction {ExtractionId}: Score {Score}/{TotalMarks}",
                    extraction.McqExtractionId,
                    totalScore,
                    totalMarks);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error evaluating MCQ extraction {ExtractionId}",
                    extraction.McqExtractionId);
            }

            return await Task.FromResult(evaluation);
        }

        private List<McqQuestionDetails> GetMcqQuestionsWithDetails(GeneratedExamResponse exam)
        {
            var questions = new List<McqQuestionDetails>();

            if (exam.Parts == null || exam.Parts.Count == 0)
            {
                return questions;
            }

            int globalQuestionNumber = 1;

            // Iterate through all parts
            foreach (var part in exam.Parts)
            {
                // Check if this is an MCQ part
                if (part.QuestionType != null && 
                    part.QuestionType.Contains("MCQ", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var question in part.Questions)
                    {
                        questions.Add(new McqQuestionDetails
                        {
                            QuestionId = question.QuestionId,
                            QuestionNumber = globalQuestionNumber++,
                            QuestionText = question.QuestionText,
                            Options = question.Options ?? new List<string>(),
                            CorrectAnswer = question.CorrectAnswer ?? string.Empty,
                            Marks = part.MarksPerQuestion
                        });
                    }
                }
                else
                {
                    // Skip subjective questions but maintain question numbering
                    globalQuestionNumber += part.Questions.Count;
                }
            }

            return questions;
        }

        private class McqQuestionDetails
        {
            public string QuestionId { get; set; } = string.Empty;
            public int QuestionNumber { get; set; }
            public string QuestionText { get; set; } = string.Empty;
            public List<string> Options { get; set; } = new();
            public string CorrectAnswer { get; set; } = string.Empty;
            public int Marks { get; set; }
        }
    }
}
