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
            "EVALUATION REQUIREMENTS:\n" +
            "1) STUDENT'S ANSWER ANALYSIS:\n" +
            "   - Echo back the student's answer in studentAnswerEcho (cleaned up for clarity)\n" +
            "   - Identify what the student attempted to do\n" +
            "   - Break their work into 2-5 logical steps\n\n" +
            "2) EXPECTED ANSWER:\n" +
            "   - Provide a COMPLETE, DETAILED expected answer with ALL steps shown\n" +
            "   - Include formulas, calculations, and explanations\n" +
            "   - Show the complete working, not just the final answer\n" +
            "   - Format it clearly with proper mathematical notation\n\n" +
            "3) STEP-BY-STEP COMPARISON WITH DETAILED FEEDBACK:\n" +
            "   - For each step, compare what student wrote vs what was expected\n" +
            "   - Award marks for correct steps, even if final answer is wrong\n" +
            "   - For EACH step feedback, you MUST include:\n" +
            "     a) What the student wrote/did in this step\n" +
            "     b) The correct approach/answer for this step\n" +
            "     c) If incorrect or incomplete: specific improvement needed\n" +
            "     d) If correct: acknowledgment and encouragement\n" +
            "   - Format: 'Your answer: [X]. Correct answer: [Y]. Improvement needed: [Z]' OR 'Your answer: [X] is correct!'\n\n" +
            "4) OVERALL FEEDBACK WITH IMPROVEMENT GUIDANCE:\n" +
            "   - Start: 'Your Answer: [brief summary of what student wrote]'\n" +
            "   - Then: 'Correct Answer: [complete solution with all steps clearly shown]'\n" +
            "   - Compare: 'Key Differences: [list what was missing, incorrect, or could be improved]'\n" +
            "   - Guide: 'Improvements Needed: [specific, actionable advice on how to improve]'\n" +
            "   - Encourage: [positive note about what was done well, if anything]\n\n" +
            "Output JSON only with this schema:\n" +
            "{\n" +
            "  \"earnedMarks\": number,\n" +
            "  \"maxMarks\": number,\n" +
            "  \"isFullyCorrect\": boolean,\n" +
            "  \"expectedAnswer\": \"string (MUST be detailed with all steps)\",\n" +
            "  \"studentAnswerEcho\": \"string (cleaned version of what student wrote)\",\n" +
            "  \"stepAnalysis\": [\n" +
            "    {\n" +
            "      \"step\": number,\n" +
            "      \"description\": \"string (what this step should accomplish)\",\n" +
            "      \"isCorrect\": boolean,\n" +
            "      \"marksAwarded\": number,\n" +
            "      \"maxMarksForStep\": number,\n" +
            "      \"feedback\": \"string (MUST include: Your answer: [X], Correct answer: [Y], Improvement: [Z] or Your answer is correct!)\"\n" +
            "    }\n" +
            "  ],\n" +
            "  \"overallFeedback\": \"string (MUST include: Your Answer: [X], Correct Answer: [Y with full steps], Key Differences: [Z], Improvements Needed: [W])\"\n" +
            "}\n" +
            "Rules:\n" +
            "- earnedMarks MUST be between 0 and maxMarks\n" +
            "- Sum of marksAwarded MUST equal earnedMarks\n" +
            "- expectedAnswer MUST contain COMPLETE solution with ALL steps, not just final answer\n" +
            "- studentAnswerEcho MUST contain what student actually wrote (cleaned up)\n" +
            "- Each step feedback MUST include: 'Your answer: [what student did]', 'Correct answer: [expected]', 'Improvement: [specific guidance]'\n" +
            "- If step is correct, feedback should say 'Your answer: [X] is correct! Well done.'\n" +
            "- overallFeedback MUST follow format: 'Your Answer: [X]\\nCorrect Answer: [Y with steps]\\nKey Differences: [Z]\\nImprovements Needed: [specific actionable advice]'\n" +
            "- Be encouraging and constructive in all feedback\n" +
            "- Return ONLY JSON, no extra text";

        // System prompt for subjective evaluation WITH stored rubric
        private const string RUBRIC_EVALUATION_SYSTEM_PROMPT = 
            "You are an experienced Karnataka State Board mathematics examiner.\n" +
            "Evaluate ONE student's SUBJECTIVE answer using the provided MARKING RUBRIC.\n\n" +
            "You will be given:\n" +
            "- The question text\n" +
            "- The marking rubric with steps and marks for each step\n" +
            "- The model correct answer\n" +
            "- The student's answer (may contain OCR/spelling issues)\n\n" +
            "EVALUATION REQUIREMENTS:\n" +
            "1) STUDENT'S ANSWER:\n" +
            "   - Echo back what student wrote in studentAnswerEcho (cleaned up)\n" +
            "   - Identify what approach they took\n\n" +
            "2) EXPECTED ANSWER:\n" +
            "   - Provide COMPLETE solution with ALL steps detailed\n" +
            "   - Include all formulas, calculations, and explanations\n" +
            "   - Show complete working, not just final answer\n\n" +
            "3) RUBRIC-BASED STEP EVALUATION WITH DETAILED FEEDBACK:\n" +
            "   - Evaluate each rubric step IN ORDER\n" +
            "   - For EACH step, provide comprehensive feedback including:\n" +
            "     a) What the student actually did: 'Your answer: [X]'\n" +
            "     b) What was expected per rubric: 'Correct approach: [Y]'\n" +
            "     c) If wrong/incomplete: 'Improvement needed: [specific guidance]'\n" +
            "     d) If correct: 'Your answer is correct! [encouragement]'\n" +
            "   - Award partial marks for partially correct steps\n\n" +
            "4) OVERALL FEEDBACK WITH IMPROVEMENT GUIDANCE:\n" +
            "   - Start: 'Your Approach: [what student attempted]'\n" +
            "   - Then: 'Correct Solution:\\n[complete step-by-step solution with all details]'\n" +
            "   - Compare: 'Key Gaps: [specific missing elements or errors]'\n" +
            "   - Guide: 'How to Improve: [actionable, specific advice for each gap]'\n" +
            "   - Encourage: [positive feedback on what was done well]\n\n" +
            "Output JSON only with this schema:\n" +
            "{\n" +
            "  \"earnedMarks\": number,\n" +
            "  \"maxMarks\": number,\n" +
            "  \"isFullyCorrect\": boolean,\n" +
            "  \"expectedAnswer\": \"string (MUST be complete solution with all steps)\",\n" +
            "  \"studentAnswerEcho\": \"string (what student actually wrote, cleaned)\",\n" +
            "  \"stepAnalysis\": [\n" +
            "    {\n" +
            "      \"step\": number,\n" +
            "      \"description\": \"string (from rubric)\",\n" +
            "      \"isCorrect\": boolean,\n" +
            "      \"marksAwarded\": number,\n" +
            "      \"maxMarksForStep\": number,\n" +
            "      \"feedback\": \"string (MUST include: Your answer: [X], Correct approach: [Y], Improvement needed: [Z] OR Your answer is correct!)\"\n" +
            "    }\n" +
            "  ],\n" +
            "  \"overallFeedback\": \"string (MUST include: Your Approach: [X], Correct Solution: [Y with all steps], Key Gaps: [Z], How to Improve: [W])\"\n" +
            "}\n" +
            "Rules:\n" +
            "- earnedMarks MUST be between 0 and maxMarks\n" +
            "- stepAnalysis MUST follow rubric steps exactly\n" +
            "- marksAwarded must not exceed maxMarksForStep from rubric\n" +
            "- Sum of marksAwarded MUST equal earnedMarks\n" +
            "- expectedAnswer MUST contain COMPLETE solution with ALL steps\n" +
            "- Each step feedback MUST include: 'Your answer: [X]', 'Correct approach: [Y]', and if wrong: 'Improvement needed: [Z]'\n" +
            "- If step is correct, say: 'Your answer: [X] is correct! [encouragement]'\n" +
            "- overallFeedback MUST follow format: 'Your Approach: [X]\\nCorrect Solution: [Y]\\nKey Gaps: [Z]\\nHow to Improve: [actionable advice]'\n" +
            "- Be specific, constructive, and encouraging in all feedback\n" +
            "- Return ONLY JSON, no extra text";

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

            _logger.LogInformation("AI Response for question {QuestionId} with rubric: {Response}", 
                question.QuestionId, evaluationJson);

            // Parse JSON response
            SubjectiveEvaluationResult? evaluation;
            try
            {
                evaluation = JsonSerializer.Deserialize<SubjectiveEvaluationResult>(
                    evaluationJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse rubric AI response as JSON. Response was: {Response}", evaluationJson);
                throw new Exception($"Failed to parse rubric evaluation response: {ex.Message}");
            }

            if (evaluation == null)
            {
                _logger.LogError("Deserialization returned null for rubric response: {Response}", evaluationJson);
                throw new Exception("Failed to parse rubric-based evaluation response - result was null");
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

            _logger.LogInformation("AI Response for question {QuestionId}: {Response}", 
                question.QuestionId, evaluationJson);

            // Parse JSON response
            SubjectiveEvaluationResult? evaluation;
            try
            {
                evaluation = JsonSerializer.Deserialize<SubjectiveEvaluationResult>(
                    evaluationJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse AI response as JSON. Response was: {Response}", evaluationJson);
                throw new Exception($"Failed to parse evaluation response: {ex.Message}");
            }

            if (evaluation == null)
            {
                _logger.LogError("Deserialization returned null for response: {Response}", evaluationJson);
                throw new Exception("Failed to parse evaluation response - result was null");
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
