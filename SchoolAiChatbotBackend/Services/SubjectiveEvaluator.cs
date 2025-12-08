using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SchoolAiChatbotBackend.Controllers;
using SchoolAiChatbotBackend.Models;

namespace SchoolAiChatbotBackend.Services
{
    public class SubjectiveEvaluator : ISubjectiveEvaluator
    {
        private readonly IOpenAIService _openAIService;
        private readonly ISubjectiveRubricService _rubricService;
        private readonly ILogger<SubjectiveEvaluator> _logger;

        // System prompt for subjective evaluation WITHOUT rubric
        private const string EVALUATION_SYSTEM_PROMPT = 
            "You are an experienced Karnataka State Board mathematics examiner.\n" +
            "Evaluate ONE student's SUBJECTIVE answer to ONE question step by step and award partial marks.\n" +
            "You will be given:\n" +
            "- The question text\n" +
            "- The model correct answer or solution idea\n" +
            "- The maximum marks (maxMarks)\n" +
            "- The student's answer (may contain OCR/spelling issues)\n\n" +
            "You MUST:\n" +
            "1) Break the student's work into 2–5 logical steps and judge each.\n" +
            "2) Award marks for correct steps, even if final answer is wrong.\n" +
            "3) Provide a full expected correct answer.\n" +
            "4) Provide clear feedback for each step and overall.\n\n" +
            "Output JSON only with this schema:\n" +
            "{\n" +
            "  \"earnedMarks\": number,\n" +
            "  \"maxMarks\": number,\n" +
            "  \"isFullyCorrect\": boolean,\n" +
            "  \"expectedAnswer\": \"string\",\n" +
            "  \"studentAnswerEcho\": \"string\",\n" +
            "  \"stepAnalysis\": [\n" +
            "    {\n" +
            "      \"step\": number,\n" +
            "      \"description\": \"string\",\n" +
            "      \"isCorrect\": boolean,\n" +
            "      \"marksAwarded\": number,\n" +
            "      \"maxMarksForStep\": number,\n" +
            "      \"feedback\": \"string\"\n" +
            "    }\n" +
            "  ],\n" +
            "  \"overallFeedback\": \"string\"\n" +
            "}\n" +
            "Rules:\n" +
            "- earnedMarks MUST be between 0 and maxMarks.\n" +
            "- Sum of marksAwarded in stepAnalysis MUST equal earnedMarks.\n" +
            "- expectedAnswer MUST always contain the full correct final answer.\n" +
            "- overallFeedback MUST clearly mention the expected final answer if the student's answer is not fully correct.\n" +
            "- Return ONLY JSON, no extra text.";

        // System prompt for subjective evaluation WITH stored rubric
        private const string RUBRIC_EVALUATION_SYSTEM_PROMPT = 
            "You are an experienced Karnataka State Board mathematics examiner.\n" +
            "Evaluate ONE student's SUBJECTIVE answer using the provided MARKING RUBRIC.\n\n" +
            "You will be given:\n" +
            "- The question text\n" +
            "- The marking rubric with steps and marks for each step\n" +
            "- The model correct answer\n" +
            "- The student's answer (may contain OCR/spelling issues)\n\n" +
            "You MUST:\n" +
            "1) Evaluate each rubric step IN ORDER and award marks based on rubric.\n" +
            "2) Award partial marks for partially correct steps.\n" +
            "3) Provide clear feedback for each step.\n" +
            "4) Be fair and consistent with the rubric.\n\n" +
            "Output JSON only with this schema:\n" +
            "{\n" +
            "  \"earnedMarks\": number,\n" +
            "  \"maxMarks\": number,\n" +
            "  \"isFullyCorrect\": boolean,\n" +
            "  \"expectedAnswer\": \"string\",\n" +
            "  \"studentAnswerEcho\": \"string\",\n" +
            "  \"stepAnalysis\": [\n" +
            "    {\n" +
            "      \"step\": number,\n" +
            "      \"description\": \"string\",\n" +
            "      \"isCorrect\": boolean,\n" +
            "      \"marksAwarded\": number,\n" +
            "      \"maxMarksForStep\": number,\n" +
            "      \"feedback\": \"string\"\n" +
            "    }\n" +
            "  ],\n" +
            "  \"overallFeedback\": \"string\"\n" +
            "}\n" +
            "Rules:\n" +
            "- earnedMarks MUST be between 0 and maxMarks.\n" +
            "- stepAnalysis MUST follow the rubric steps exactly.\n" +
            "- For each step, marksAwarded must not exceed maxMarksForStep from rubric.\n" +
            "- Sum of marksAwarded in stepAnalysis MUST equal earnedMarks.\n" +
            "- Return ONLY JSON, no extra text.";

        public SubjectiveEvaluator(
            IOpenAIService openAIService,
            ISubjectiveRubricService rubricService,
            ILogger<SubjectiveEvaluator> logger)
        {
            _openAIService = openAIService;
            _rubricService = rubricService;
            _logger = logger;
        }

        public async Task<List<SubjectiveEvaluationResult>> EvaluateSubjectiveAnswersAsync(
            GeneratedExamResponse exam,
            string studentAnswersRawText)
        {
            var results = new List<SubjectiveEvaluationResult>();

            // Get all subjective questions (non-MCQ questions)
            var subjectiveQuestions = GetSubjectiveQuestions(exam);

            if (subjectiveQuestions.Count == 0)
            {
                _logger.LogInformation("No subjective questions found in exam {ExamId}", exam.ExamId);
                return results;
            }

            // Split OCR text into per-question chunks
            var answerChunks = SplitAnswersByQuestion(studentAnswersRawText, subjectiveQuestions.Count);

            // Evaluate each subjective question
            for (int i = 0; i < subjectiveQuestions.Count; i++)
            {
                var question = subjectiveQuestions[i];
                var studentAnswer = i < answerChunks.Count ? answerChunks[i] : string.Empty;

                try
                {
                    var evaluation = await EvaluateSingleQuestionAsync(question, studentAnswer);
                    results.Add(evaluation);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating question {QuestionId}", question.QuestionId);
                    
                    // Add error result
                    results.Add(new SubjectiveEvaluationResult
                    {
                        QuestionId = question.QuestionId,
                        QuestionNumber = question.QuestionNumber,
                        EarnedMarks = 0,
                        MaxMarks = question.MaxMarks,
                        IsFullyCorrect = false,
                        ExpectedAnswer = question.CorrectAnswer,
                        StudentAnswerEcho = studentAnswer,
                        OverallFeedback = "Error during evaluation. Please contact support."
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Evaluate subjective answers using stored rubrics for consistent, auditable marking.
        /// </summary>
        public async Task<List<SubjectiveEvaluationResult>> EvaluateWithRubricsAsync(
            string examId,
            GeneratedExamResponse exam,
            string studentAnswersRawText)
        {
            var results = new List<SubjectiveEvaluationResult>();

            // Get all subjective questions (non-MCQ questions)
            var subjectiveQuestions = GetSubjectiveQuestions(exam);

            if (subjectiveQuestions.Count == 0)
            {
                _logger.LogInformation("No subjective questions found in exam {ExamId}", examId);
                return results;
            }

            // Split OCR text into per-question chunks
            var answerChunks = SplitAnswersByQuestion(studentAnswersRawText, subjectiveQuestions.Count);

            // Evaluate each subjective question using its stored rubric
            for (int i = 0; i < subjectiveQuestions.Count; i++)
            {
                var question = subjectiveQuestions[i];
                var studentAnswer = i < answerChunks.Count ? answerChunks[i] : string.Empty;

                try
                {
                    // Try to get stored rubric for this question
                    var rubricSteps = await _rubricService.GetRubricStepsAsync(examId, question.QuestionId);

                    SubjectiveEvaluationResult evaluation;
                    if (rubricSteps != null && rubricSteps.Count > 0)
                    {
                        // Use rubric-based evaluation
                        _logger.LogInformation("Using stored rubric for Exam={ExamId}, Question={QuestionId} ({StepCount} steps)",
                            examId, question.QuestionId, rubricSteps.Count);
                        evaluation = await EvaluateWithRubricAsync(question, studentAnswer, rubricSteps);
                    }
                    else
                    {
                        // Fall back to dynamic evaluation without rubric
                        _logger.LogWarning("No rubric found for Exam={ExamId}, Question={QuestionId}, using dynamic evaluation",
                            examId, question.QuestionId);
                        evaluation = await EvaluateSingleQuestionAsync(question, studentAnswer);
                    }

                    results.Add(evaluation);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating question {QuestionId} with rubric", question.QuestionId);
                    
                    results.Add(new SubjectiveEvaluationResult
                    {
                        QuestionId = question.QuestionId,
                        QuestionNumber = question.QuestionNumber,
                        EarnedMarks = 0,
                        MaxMarks = question.MaxMarks,
                        IsFullyCorrect = false,
                        ExpectedAnswer = question.CorrectAnswer,
                        StudentAnswerEcho = studentAnswer,
                        OverallFeedback = "Error during evaluation. Please contact support."
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Evaluate a single question using a stored rubric
        /// </summary>
        private async Task<SubjectiveEvaluationResult> EvaluateWithRubricAsync(
            SubjectiveQuestion question,
            string studentAnswer,
            List<StepRubricItem> rubricSteps)
        {
            // Build rubric text for prompt
            var rubricBuilder = new StringBuilder();
            rubricBuilder.AppendLine("MARKING RUBRIC:");
            foreach (var step in rubricSteps)
            {
                rubricBuilder.AppendLine($"  Step {step.StepNumber}: {step.Description} [{step.Marks} marks]");
            }
            var totalMarks = rubricSteps.Sum(s => s.Marks);

            var userPrompt = $@"Question: {question.QuestionText}

{rubricBuilder}
Total Marks: {totalMarks}

Model Correct Answer: {question.CorrectAnswer}

Student's Answer:
{studentAnswer}

Evaluate this answer using the marking rubric. For each step in the rubric, determine if the student demonstrated the required skill and award marks accordingly.";

            _logger.LogInformation("Evaluating question {QuestionId} with rubric ({StepCount} steps, {TotalMarks} marks)",
                question.QuestionId, rubricSteps.Count, totalMarks);

            // Call OpenAI service with rubric-based evaluation prompt
            var evaluationJson = await _openAIService.EvaluateSubjectiveAnswerAsync(
                RUBRIC_EVALUATION_SYSTEM_PROMPT,
                userPrompt);

            // Parse JSON response
            var evaluation = JsonSerializer.Deserialize<SubjectiveEvaluationResult>(
                evaluationJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (evaluation == null)
            {
                throw new Exception("Failed to parse rubric-based evaluation response");
            }

            // Set metadata
            evaluation.QuestionId = question.QuestionId;
            evaluation.QuestionNumber = question.QuestionNumber;
            evaluation.MaxMarks = question.MaxMarks;

            return evaluation;
        }

        private async Task<SubjectiveEvaluationResult> EvaluateSingleQuestionAsync(
            SubjectiveQuestion question,
            string studentAnswer)
        {
            // Build user prompt
            var userPrompt = $@"Question: {question.QuestionText}

Model Correct Answer: {question.CorrectAnswer}

Maximum Marks: {question.MaxMarks}

Student's Answer:
{studentAnswer}

Evaluate this answer step by step and provide detailed feedback.";

            _logger.LogInformation("Evaluating question {QuestionId} with AI", question.QuestionId);

            // Call OpenAI service with evaluation prompt
            var evaluationJson = await _openAIService.EvaluateSubjectiveAnswerAsync(
                EVALUATION_SYSTEM_PROMPT,
                userPrompt);

            // Parse JSON response
            var evaluation = JsonSerializer.Deserialize<SubjectiveEvaluationResult>(
                evaluationJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (evaluation == null)
            {
                throw new Exception("Failed to parse evaluation response");
            }

            // Set metadata
            evaluation.QuestionId = question.QuestionId;
            evaluation.QuestionNumber = question.QuestionNumber;
            evaluation.MaxMarks = question.MaxMarks;

            return evaluation;
        }

        private List<SubjectiveQuestion> GetSubjectiveQuestions(GeneratedExamResponse exam)
        {
            var subjectiveQuestions = new List<SubjectiveQuestion>();

            if (exam.Parts == null || exam.Parts.Count == 0)
            {
                return subjectiveQuestions;
            }

            // Get all non-MCQ parts (Parts B, C, D, E)
            foreach (var part in exam.Parts)
            {
                if (part.QuestionType.Contains("MCQ", StringComparison.OrdinalIgnoreCase))
                {
                    continue; // Skip MCQ parts
                }

                foreach (var question in part.Questions)
                {
                    subjectiveQuestions.Add(new SubjectiveQuestion
                    {
                        QuestionId = question.QuestionId,
                        QuestionNumber = question.QuestionNumber,
                        QuestionText = question.QuestionText,
                        CorrectAnswer = question.CorrectAnswer,
                        MaxMarks = part.MarksPerQuestion
                    });
                }
            }

            return subjectiveQuestions;
        }

        private List<string> SplitAnswersByQuestion(string rawText, int expectedCount)
        {
            var chunks = new List<string>();

            if (string.IsNullOrWhiteSpace(rawText))
            {
                // Return empty answers for all questions
                for (int i = 0; i < expectedCount; i++)
                {
                    chunks.Add(string.Empty);
                }
                return chunks;
            }

            // Try to split by question markers: Q1, Q2, etc. or Question 1, Question 2, etc.
            var pattern = @"(?:Q(?:uestion)?\s*(\d+)|(?:^|\n)(\d+)\s*[.)])\s*";
            var matches = Regex.Matches(rawText, pattern, RegexOptions.IgnoreCase);

            if (matches.Count == 0)
            {
                // No question markers found, treat entire text as one answer
                chunks.Add(rawText.Trim());
                
                // Fill remaining with empty
                for (int i = 1; i < expectedCount; i++)
                {
                    chunks.Add(string.Empty);
                }
                return chunks;
            }

            // Extract chunks between markers
            for (int i = 0; i < matches.Count; i++)
            {
                var startIndex = matches[i].Index + matches[i].Length;
                var endIndex = i + 1 < matches.Count ? matches[i + 1].Index : rawText.Length;
                
                var chunk = rawText.Substring(startIndex, endIndex - startIndex).Trim();
                chunks.Add(chunk);
            }

            // Fill remaining questions with empty answers if needed
            while (chunks.Count < expectedCount)
            {
                chunks.Add(string.Empty);
            }

            return chunks;
        }

        private class SubjectiveQuestion
        {
            public string QuestionId { get; set; } = string.Empty;
            public int QuestionNumber { get; set; }
            public string QuestionText { get; set; } = string.Empty;
            public string CorrectAnswer { get; set; } = string.Empty;
            public int MaxMarks { get; set; }
        }
    }
}
