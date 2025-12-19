using Microsoft.AspNetCore.Mvc;
using SchoolAiChatbotBackend.Models;
using SchoolAiChatbotBackend.Services;
using System.Text.Json;

namespace SchoolAiChatbotBackend.Controllers
{
    /// <summary>
    /// LOCAL DEVELOPMENT ONLY - Processes answer sheets without Azure Functions
    /// This simulates what the Azure Function would do for local testing
    /// </summary>
    [ApiController]
    [Route("api/local-eval")]
    public class LocalEvaluationController : ControllerBase
    {
        private readonly IExamRepository _examRepository;
        private readonly IExamStorageService _examStorageService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IOpenAIService _openAIService;
        private readonly ILogger<LocalEvaluationController> _logger;

        public LocalEvaluationController(
            IExamRepository examRepository,
            IExamStorageService examStorageService,
            IFileStorageService fileStorageService,
            IOpenAIService openAIService,
            ILogger<LocalEvaluationController> logger)
        {
            _examRepository = examRepository;
            _examStorageService = examStorageService;
            _fileStorageService = fileStorageService;
            _openAIService = openAIService;
            _logger = logger;
        }

        /// <summary>
        /// LOCAL DEV: Get all pending submissions
        /// </summary>
        [HttpGet("pending-submissions")]
        public async Task<IActionResult> GetPendingSubmissions()
        {
            try
            {
                // This would need a repository method - for now return instructions
                return Ok(new
                {
                    message = "To process a submission, use: POST /api/local-eval/process-submission/{submissionId}",
                    note = "Get submission ID from the upload response or from the mobile app"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// LOCAL DEV: Process pending written submissions
        /// Call this after uploading answer sheets to trigger evaluation
        /// </summary>
        [HttpPost("process-submission/{submissionId}")]
        public async Task<IActionResult> ProcessSubmission(string submissionId)
        {
            try
            {
                _logger.LogInformation("🔧 [LOCAL-EVAL] Processing submission {SubmissionId}", submissionId);
                
                var submission = await _examRepository.GetWrittenSubmissionAsync(submissionId);
                if (submission == null)
                {
                    return NotFound(new { error = "Submission not found" });
                }

                if (submission.Status != SubmissionStatus.PendingEvaluation && 
                    submission.Status != SubmissionStatus.Uploaded)
                {
                    return BadRequest(new { error = $"Submission already processed or in wrong state: {submission.Status}" });
                }

                // Get the exam
                var exam = _examStorageService.GetExam(submission.ExamId);
                if (exam == null)
                {
                    return NotFound(new { error = "Exam not found" });
                }

                // Get subjective questions
                var subjectiveQuestions = GetSubjectiveQuestions(exam);
                if (subjectiveQuestions.Count == 0)
                {
                    return BadRequest(new { error = "No subjective questions found in exam" });
                }

                // Update status to processing
                submission.Status = SubmissionStatus.OcrProcessing;
                submission.OcrStartedAt = DateTime.UtcNow;
                await _examRepository.SaveWrittenSubmissionAsync(submission);

                // MOCK OCR: For local testing, use placeholder text
                // In production, Azure Function would perform actual OCR using Google Vision API
                var ocrText = "MOCK OCR TEXT - Student wrote answers here";
                submission.ExtractedText = ocrText;
                submission.OcrCompletedAt = DateTime.UtcNow;
                submission.OcrProcessingTimeMs = 100;
                
                // Update status to evaluating
                submission.Status = SubmissionStatus.Evaluating;
                submission.EvaluationStartedAt = DateTime.UtcNow;
                await _examRepository.SaveWrittenSubmissionAsync(submission);

                _logger.LogInformation("🤖 [LOCAL-EVAL] Starting AI evaluation for {QuestionCount} questions", 
                    subjectiveQuestions.Count);

                // Evaluate each question
                var evaluations = new List<object>();
                decimal totalScore = 0;
                decimal maxScore = 0;

                foreach (var (question, marks) in subjectiveQuestions)
                {
                    maxScore += marks;
                    
                    // Create evaluation prompt
                    var systemPrompt = @"You are an expert teacher evaluating a student's answer.
Provide a detailed evaluation with:
1. Score out of the maximum marks
2. Detailed feedback on what was correct and what was missed
3. Step-by-step breakdown";

                    var userPrompt = $@"
Question: {question.QuestionText}
Expected Answer: {question.CorrectAnswer ?? "No model answer provided"}
Student Answer: [Answer extracted from image - for local testing, use partial marks]
Maximum Marks: {marks}

Evaluate and provide JSON response:
{{
  ""score"": <number>,
  ""feedback"": ""<detailed feedback>"",
  ""breakdown"": [
    {{""step"": ""Step 1"", ""description"": ""..."", ""marks"": <number>}}
  ]
}}";

                    try
                    {
                        // Call OpenAI for evaluation
                        var aiResponse = await _openAIService.EvaluateSubjectiveAnswerAsync(
                            systemPrompt, userPrompt);

                        // Parse AI response
                        var evalResult = JsonSerializer.Deserialize<SubjectiveQuestionEvaluation>(aiResponse);
                        
                        if (evalResult != null)
                        {
                            totalScore += evalResult.Score ?? 0;
                            
                            evaluations.Add(new
                            {
                                questionId = question.QuestionId,
                                questionNumber = question.QuestionNumber,
                                questionText = question.QuestionText,
                                maxMarks = marks,
                                studentAnswer = "[Not answered]", // Mock - no OCR in local mode
                                awardedMarks = evalResult.Score ?? 0,
                                feedback = evalResult.Feedback ?? "Question not answered.",
                                breakdown = evalResult.Breakdown ?? new List<EvaluationStep>()
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error evaluating question {QuestionId}", question.QuestionId);
                        
                        // Add error evaluation
                        evaluations.Add(new
                        {
                            questionId = question.QuestionId,
                            questionNumber = question.QuestionNumber,
                            questionText = question.QuestionText,
                            maxMarks = marks,
                            studentAnswer = "[Not answered]",
                            awardedMarks = 0,
                            feedback = "Question not answered.",
                            breakdown = new[]
                            {
                                new { step = "Step 1", description = "Step 1", marks = 0 },
                                new { step = "Step 2", description = "Step 2", marks = 0 },
                                new { step = "Step 3", description = "Step 3", marks = 0 }
                            }
                        });
                    }
                }

                // Calculate grade
                var percentage = maxScore > 0 ? (totalScore / maxScore) * 100 : 0;
                var grade = percentage >= 90 ? "A+" :
                           percentage >= 80 ? "A" :
                           percentage >= 70 ? "B" :
                           percentage >= 60 ? "C" :
                           percentage >= 50 ? "D" : "F";

                // Create result object
                var result = new
                {
                    writtenSubmissionId = submissionId,
                    examId = submission.ExamId,
                    studentId = submission.StudentId,
                    totalScore = totalScore,
                    maxPossibleScore = maxScore,
                    percentage = Math.Round(percentage, 2),
                    grade = grade,
                    evaluatedAt = DateTime.UtcNow,
                    questions = evaluations
                };

                // Save result to blob storage
                var resultJson = JsonSerializer.Serialize(result, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                var blobPath = $"{submissionId}/evaluation-result.json";
                await _fileStorageService.SaveJsonToBlobAsync(resultJson, blobPath, "evaluation-results");

                // Update submission
                submission.Status = SubmissionStatus.ResultsReady;
                submission.EvaluatedAt = DateTime.UtcNow;
                submission.EvaluationProcessingTimeMs = (long)(DateTime.UtcNow - submission.EvaluationStartedAt!.Value).TotalMilliseconds;
                submission.TotalScore = totalScore;
                submission.MaxPossibleScore = maxScore;
                submission.Percentage = percentage;
                submission.Grade = grade;
                submission.EvaluationResultBlobPath = blobPath;
                
                await _examRepository.SaveWrittenSubmissionAsync(submission);

                _logger.LogInformation("✅ [LOCAL-EVAL] Evaluation complete: {Score}/{MaxScore} ({Percentage}%)", 
                    totalScore, maxScore, percentage);

                return Ok(new
                {
                    message = "✅ Evaluation completed successfully!",
                    submissionId = submissionId,
                    score = $"{totalScore}/{maxScore}",
                    percentage = $"{percentage:F2}%",
                    grade = grade,
                    note = "This is a LOCAL DEVELOPMENT evaluation. In production, Azure Functions handle this automatically."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [LOCAL-EVAL] Error processing submission {SubmissionId}", submissionId);
                
                // Update submission with error
                try
                {
                    var submission = await _examRepository.GetWrittenSubmissionAsync(submissionId);
                    if (submission != null)
                    {
                        submission.Status = SubmissionStatus.Failed;
                        submission.ErrorMessage = ex.Message;
                        await _examRepository.SaveWrittenSubmissionAsync(submission);
                    }
                }
                catch { }
                
                return StatusCode(500, new { error = "Evaluation failed", message = ex.Message });
            }
        }

        private List<(PartQuestion question, int marks)> GetSubjectiveQuestions(GeneratedExamResponse exam)
        {
            var questions = new List<(PartQuestion question, int marks)>();
            
            if (exam.Parts != null)
            {
                foreach (var part in exam.Parts)
                {
                    // Get non-MCQ questions (subjective)
                    if (part.Questions != null && !part.QuestionType.Contains("MCQ", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var question in part.Questions)
                        {
                            questions.Add((question, part.MarksPerQuestion));
                        }
                    }
                }
            }
            
            return questions;
        }
    }

    // Helper classes for JSON deserialization
    public class SubjectiveQuestionEvaluation
    {
        public decimal? Score { get; set; }
        public string? Feedback { get; set; }
        public List<EvaluationStep>? Breakdown { get; set; }
    }

    public class EvaluationStep
    {
        public string? Step { get; set; }
        public string? Description { get; set; }
        public decimal? Marks { get; set; }
    }
}
