using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SchoolAiChatbotBackend.DTOs;
using SchoolAiChatbotBackend.Models;
using SchoolAiChatbotBackend.Services;

namespace SchoolAiChatbotBackend.Controllers
{
    /// <summary>
    /// Thin API controller for exam submissions
    /// Heavy processing (OCR, AI evaluation) is handled by Azure Functions
    /// </summary>
    [ApiController]
    [Route("api/exam")]
    public class ExamSubmissionController : ControllerBase
    {
        // File upload constraints (production hardened)
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf", ".webp" };
        private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/webp", "application/pdf" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB per file
        private const int MaxFilesPerUpload = 20;

        private readonly IExamRepository _examRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IExamStorageService _examStorageService;
        private readonly IQueueService _queueService;
        private readonly IMathOcrNormalizer _mathNormalizer;
        private readonly ISubjectiveRubricService _rubricService;
        private readonly ILogger<ExamSubmissionController> _logger;
        private readonly IConfiguration _configuration;

        public ExamSubmissionController(
            IExamRepository examRepository,
            IFileStorageService fileStorageService,
            IExamStorageService examStorageService,
            IQueueService queueService,
            IMathOcrNormalizer mathNormalizer,
            ISubjectiveRubricService rubricService,
            ILogger<ExamSubmissionController> logger,
            IConfiguration configuration)
        {
            _examRepository = examRepository;
            _fileStorageService = fileStorageService;
            _examStorageService = examStorageService;
            _queueService = queueService;
            _mathNormalizer = mathNormalizer;
            _rubricService = rubricService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Submit MCQ answers for evaluation
        /// </summary>
        /// <param name="request">MCQ answers submission</param>
        /// <returns>Evaluation result with score and feedback</returns>
        [HttpPost("submit-mcq")]
        [ProducesResponseType(typeof(McqSubmissionResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<McqSubmissionResponse>> SubmitMcqAnswers([FromBody] SubmitMcqRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Received MCQ submission for exam {ExamId} from student {StudentId}",
                    request.ExamId,
                    request.StudentId);

                // Load exam from storage service
                var exam = _examStorageService.GetExam(request.ExamId);
                if (exam == null)
                {
                    return NotFound(new { error = $"Exam {request.ExamId} not found. Please generate the exam first using /api/exam/generate" });
                }

                // Get all MCQ questions
                var mcqQuestions = GetMcqQuestions(exam);

                if (mcqQuestions.Count == 0)
                {
                    return BadRequest(new { error = "No MCQ questions found in this exam" });
                }

                // Evaluate each answer
                var results = new List<McqResultDto>();
                int totalScore = 0;
                int totalMarks = 0;

                foreach (var answer in request.Answers)
                {
                    var question = mcqQuestions.FirstOrDefault(q => q.QuestionId == answer.QuestionId);

                    if (question == null)
                    {
                        _logger.LogWarning("Question {QuestionId} not found", answer.QuestionId);
                        continue;
                    }

                    var isCorrect = answer.SelectedOption.Equals(question.CorrectAnswer, StringComparison.OrdinalIgnoreCase);
                    var marksAwarded = isCorrect ? question.Marks : 0;

                    totalScore += marksAwarded;
                    totalMarks += question.Marks;

                    results.Add(new McqResultDto
                    {
                        QuestionId = answer.QuestionId,
                        SelectedOption = answer.SelectedOption,
                        CorrectAnswer = question.CorrectAnswer,
                        IsCorrect = isCorrect,
                        MarksAwarded = marksAwarded
                    });
                }

                // Save submission
                var submission = new McqSubmission
                {
                    ExamId = request.ExamId,
                    StudentId = request.StudentId,
                    Score = totalScore,
                    TotalMarks = totalMarks,
                    Answers = request.Answers.Select(a =>
                    {
                        var result = results.FirstOrDefault(r => r.QuestionId == a.QuestionId);
                        return new McqAnswer
                        {
                            QuestionId = a.QuestionId,
                            SelectedOption = a.SelectedOption,
                            IsCorrect = result?.IsCorrect ?? false,
                            MarksAwarded = result?.MarksAwarded ?? 0
                        };
                    }).ToList()
                };

                await _examRepository.SaveMcqSubmissionAsync(submission);

                var response = new McqSubmissionResponse
                {
                    McqSubmissionId = submission.McqSubmissionId,
                    Score = totalScore,
                    TotalMarks = totalMarks,
                    Percentage = totalMarks > 0 ? Math.Round((double)totalScore / totalMarks * 100, 2) : 0,
                    Results = results
                };

                _logger.LogInformation(
                    "MCQ submission completed: {Score}/{TotalMarks} ({Percentage}%)",
                    totalScore,
                    totalMarks,
                    response.Percentage);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MCQ submission");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Upload written answers for subjective questions (THIN: validates, saves, enqueues only)
        /// </summary>
        /// <param name="examId">Exam ID</param>
        /// <param name="studentId">Student ID</param>
        /// <param name="files">Scanned answer images or PDF</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Submission ID and status</returns>
        [HttpPost("upload-written")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB total request limit
        [RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
        [ProducesResponseType(typeof(UploadWrittenResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)] // Conflict for duplicate
        public async Task<ActionResult<UploadWrittenResponse>> UploadWrittenAnswers(
            [FromForm] string examId,
            [FromForm] string studentId,
            [FromForm] List<IFormFile> files,
            CancellationToken cancellationToken = default)
        {
            // Generate correlation ID for this request
            var correlationId = Guid.NewGuid().ToString("N")[..8];

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["ExamId"] = examId ?? "null",
                ["StudentId"] = studentId ?? "null"
            });

            try
            {
                _logger.LogInformation(
                    "[UPLOAD_STARTED] Received {FileCount} files, total size {TotalSize} bytes",
                    files?.Count ?? 0,
                    files?.Sum(f => f.Length) ?? 0);

                // === VALIDATION PHASE ===

                // 1. Validate required fields
                if (string.IsNullOrWhiteSpace(examId) || string.IsNullOrWhiteSpace(studentId))
                {
                    _logger.LogWarning("[VALIDATION_FAILED] Missing examId or studentId");
                    return BadRequest(new { error = "ExamId and StudentId are required", correlationId });
                }

                // 2. Sanitize inputs (prevent injection)
                examId = examId.Trim();
                studentId = studentId.Trim();

                // Validate no path traversal characters
                if (examId.Contains("..") || studentId.Contains("..") ||
                    examId.Contains('/') || studentId.Contains('/') ||
                    examId.Contains('\\') || studentId.Contains('\\'))
                {
                    _logger.LogWarning("[SECURITY] Path traversal attempt detected");
                    return BadRequest(new { error = "Invalid characters in examId or studentId", correlationId });
                }

                // 3. Validate files exist
                if (files == null || files.Count == 0)
                {
                    return BadRequest(new { error = "At least one file is required", correlationId });
                }

                // 4. Validate file count
                if (files.Count > MaxFilesPerUpload)
                {
                    return BadRequest(new { error = $"Maximum {MaxFilesPerUpload} files allowed per upload", correlationId });
                }

                // 5. Validate each file (extension, MIME type, size)
                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!AllowedExtensions.Contains(extension))
                    {
                        _logger.LogWarning("[VALIDATION_FAILED] Invalid extension: {Extension}", extension);
                        return BadRequest(new { error = $"File type '{extension}' not allowed. Allowed: {string.Join(", ", AllowedExtensions)}", correlationId });
                    }

                    if (!AllowedMimeTypes.Contains(file.ContentType?.ToLowerInvariant()))
                    {
                        _logger.LogWarning("[VALIDATION_FAILED] Invalid MIME type: {MimeType}", file.ContentType);
                        return BadRequest(new { error = $"MIME type '{file.ContentType}' not allowed", correlationId });
                    }

                    if (file.Length > MaxFileSizeBytes)
                    {
                        return BadRequest(new { error = $"File '{file.FileName}' exceeds {MaxFileSizeBytes / 1024 / 1024}MB limit", correlationId });
                    }

                    if (file.Length == 0)
                    {
                        return BadRequest(new { error = $"File '{file.FileName}' is empty", correlationId });
                    }
                }

                // 6. Validate exam exists
                if (!_examStorageService.ExamExists(examId))
                {
                    return NotFound(new { error = $"Exam {examId} not found. Please generate the exam first.", correlationId });
                }

                // 7. Idempotency check - prevent duplicate submissions
                var existingSubmission = await _examRepository.GetWrittenSubmissionByExamAndStudentAsync(examId, studentId);
                if (existingSubmission != null && existingSubmission.Status != SubmissionStatus.Failed)
                {
                    _logger.LogWarning("[DUPLICATE] Submission already exists: {SubmissionId}", existingSubmission.WrittenSubmissionId);
                    return Conflict(new
                    {
                        error = "A submission already exists for this exam and student",
                        existingSubmissionId = existingSubmission.WrittenSubmissionId,
                        status = existingSubmission.Status.ToString(),
                        correlationId
                    });
                }

                // === STORAGE PHASE ===

                var filePaths = new List<string>();
                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var path = await _fileStorageService.SaveFileAsync(file, examId, studentId);
                    filePaths.Add(path);
                }

                _logger.LogInformation("[BLOB_UPLOADED] {FileCount} files saved to storage", filePaths.Count);

                // === DATABASE PHASE ===

                var submission = new WrittenSubmission
                {
                    WrittenSubmissionId = Guid.NewGuid().ToString(),
                    ExamId = examId,
                    StudentId = studentId,
                    FilePaths = filePaths,
                    Status = SubmissionStatus.PendingEvaluation
                };

                await _examRepository.SaveWrittenSubmissionAsync(submission);
                _logger.LogInformation("[DB_SAVED] Submission {SubmissionId} created", submission.WrittenSubmissionId);

                // === QUEUE PHASE ===

                var queueMessage = new WrittenSubmissionQueueMessage
                {
                    WrittenSubmissionId = submission.WrittenSubmissionId,
                    ExamId = examId,
                    StudentId = studentId,
                    FilePaths = filePaths,
                    SubmittedAt = DateTime.UtcNow,
                    Priority = "normal",
                    RetryCount = 0
                };
                await _queueService.EnqueueAsync(QueueNames.WrittenSubmissionProcessing, queueMessage);

                _logger.LogInformation(
                    "[QUEUE_ENQUEUED] Submission {SubmissionId} sent to {QueueName}",
                    submission.WrittenSubmissionId,
                    QueueNames.WrittenSubmissionProcessing);

                // === RESPONSE ===

                return Ok(new UploadWrittenResponse
                {
                    WrittenSubmissionId = submission.WrittenSubmissionId,
                    Status = "PendingEvaluation",
                    Message = "✅ Answer sheet uploaded successfully! Processing will begin shortly. Check status for results."
                });
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[CANCELLED] Upload cancelled by client");
                return StatusCode(499, new { error = "Request cancelled", correlationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UPLOAD_FAILED] Unexpected error");
                return StatusCode(500, new { error = "Internal server error", correlationId });
            }
        }

        /// <summary>
        /// Check evaluation status of a written submission
        /// GET /api/exam/submission-status/{writtenSubmissionId}
        /// </summary>
        [HttpGet("submission-status/{writtenSubmissionId}")]
        [ProducesResponseType(typeof(SubmissionStatusResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SubmissionStatusResponse>> GetSubmissionStatus(string writtenSubmissionId)
        {
            try
            {
                _logger.LogInformation("Checking status for submission {SubmissionId}", writtenSubmissionId);

                var submission = await _examRepository.GetWrittenSubmissionAsync(writtenSubmissionId);
                if (submission == null)
                {
                    return NotFound(new { error = "Submission not found" });
                }

                // Map status to user-friendly message (Status: 0=Uploaded, 1=OCR Complete, 2=Evaluation Complete, 3=OCR Failed, 4=Evaluation Failed)
                string statusMessage = submission.Status switch
                {
                    SubmissionStatus.Uploaded => "⏳ Uploaded. Waiting for OCR to start...", // 0
                    SubmissionStatus.OcrComplete => "📄 OCR Complete. AI evaluation starting...", // 1  
                    SubmissionStatus.EvaluationComplete => "✅ Evaluation completed! Your results are ready.", // 2
                    SubmissionStatus.OcrFailed => "❌ OCR Failed. Please try uploading clearer images.", // 3
                    SubmissionStatus.EvaluationFailed => "❌ Evaluation failed. Please contact support.", // 4
                    _ => "Unknown status"
                };

                var response = new SubmissionStatusResponse
                {
                    WrittenSubmissionId = writtenSubmissionId,
                    Status = ((int)submission.Status).ToString(), // Return numeric status (0-4)
                    StatusMessage = statusMessage,
                    SubmittedAt = submission.SubmittedAt,
                    EvaluatedAt = submission.EvaluatedAt,
                    IsComplete = submission.Status == SubmissionStatus.EvaluationComplete, // Status 2
                    ExamId = submission.ExamId,
                    StudentId = submission.StudentId,
                    EvaluationResultBlobPath = submission.EvaluationResultBlobPath // From WrittenSubmissions.EvaluationResultBlobPath
                };

                // If evaluation is completed (Status = 2), include the full results
                if (submission.Status == SubmissionStatus.EvaluationComplete)
                {
                    try
                    {
                        var examResult = await GetConsolidatedResultInternal(submission.ExamId, submission.StudentId);
                        response.Result = examResult;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error fetching results for completed submission {SubmissionId}", writtenSubmissionId);
                        // Continue without results - client can fetch separately
                    }
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching submission status");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get consolidated exam result for a student
        /// </summary>
        /// <param name="examId">Exam ID</param>
        /// <param name="studentId">Student ID</param>
        /// <returns>Complete exam result with MCQ and subjective scores</returns>
        [HttpGet("result/{examId}/{studentId}")]
        [ProducesResponseType(typeof(ConsolidatedExamResult), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ConsolidatedExamResult>> GetExamResult(string examId, string studentId)
        {
            try
            {
                var result = await GetConsolidatedResultInternal(examId, studentId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching exam result");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Internal method to get consolidated exam result (reusable for status endpoint)
        /// </summary>
        private async Task<ConsolidatedExamResult> GetConsolidatedResultInternal(string examId, string studentId)
        {
            _logger.LogInformation("Fetching result for exam {ExamId}, student {StudentId}", examId, studentId);

            // Load exam from storage service
            var exam = _examStorageService.GetExam(examId);
            if (exam == null)
            {
                throw new KeyNotFoundException($"Exam {examId} not found. Please generate the exam first using /api/exam/generate");
            }

            // Load MCQ submission (from direct MCQ submission)
            var mcqSubmission = await _examRepository.GetMcqSubmissionAsync(examId, studentId);

            // Load MCQ evaluation from sheet (from uploaded answer sheet)
            var mcqEvaluationFromSheet = await _examRepository.GetMcqEvaluationFromSheetAsync(examId, studentId);

            // Load written submission and evaluations
            var writtenSubmission = await _examRepository.GetWrittenSubmissionByExamAndStudentAsync(examId, studentId);
            List<SubjectiveEvaluationResult> subjectiveEvaluations = new();

            if (writtenSubmission != null)
            {
                subjectiveEvaluations = await _examRepository.GetSubjectiveEvaluationsAsync(
                    writtenSubmission.WrittenSubmissionId);
            }

            // Check if any submission exists
            if (mcqSubmission == null && mcqEvaluationFromSheet == null && writtenSubmission == null)
            {
                throw new KeyNotFoundException("No submission found for this exam and student");
            }

            // Calculate MCQ scores
            // Prioritize MCQ from sheet extraction if available, otherwise use direct submission
            int mcqScore = 0;
            int mcqTotalMarks = 0;
            List<McqResultDto> mcqResults = new();

            if (mcqEvaluationFromSheet != null)
            {
                // Use MCQ answers extracted from uploaded answer sheet
                mcqScore = mcqEvaluationFromSheet.TotalScore;
                mcqTotalMarks = mcqEvaluationFromSheet.TotalMarks;
                mcqResults = mcqEvaluationFromSheet.Evaluations.Select(e => new McqResultDto
                {
                    QuestionId = e.QuestionId,
                    SelectedOption = e.StudentAnswer,
                    CorrectAnswer = e.CorrectAnswer,
                    IsCorrect = e.IsCorrect,
                    MarksAwarded = e.MarksAwarded
                }).ToList();

                _logger.LogInformation(
                    "Using MCQ results from sheet extraction: {Score}/{Total}",
                    mcqScore,
                    mcqTotalMarks);
            }
            else if (mcqSubmission != null)
            {
                // Use MCQ answers from direct submission
                // Get MCQ questions with correct answers
                var mcqQuestions = GetMcqQuestions(exam);

                mcqScore = mcqSubmission.Score;
                mcqTotalMarks = mcqSubmission.TotalMarks;
                mcqResults = mcqSubmission.Answers.Select(a =>
                {
                    var question = mcqQuestions.FirstOrDefault(q => q.QuestionId == a.QuestionId);
                    return new McqResultDto
                    {
                        QuestionId = a.QuestionId,
                        SelectedOption = a.SelectedOption,
                        CorrectAnswer = question?.CorrectAnswer ?? "",
                        IsCorrect = a.IsCorrect,
                        MarksAwarded = a.MarksAwarded
                    };
                }).ToList();

                _logger.LogInformation(
                    "Using MCQ results from direct submission: {Score}/{Total}",
                    mcqScore,
                    mcqTotalMarks);
            }

            // Calculate subjective scores
            double subjectiveScore = subjectiveEvaluations.Sum(e => e.EarnedMarks);
            double subjectiveTotalMarks = subjectiveEvaluations.Sum(e => e.MaxMarks);

            // Calculate grand total
            double grandScore = mcqScore + subjectiveScore;
            double grandTotalMarks = mcqTotalMarks + subjectiveTotalMarks;
            double percentage = grandTotalMarks > 0 ? Math.Round(grandScore / grandTotalMarks * 100, 2) : 0;

            // Calculate grade
            string grade = CalculateGrade(percentage);
            bool passed = percentage >= 35; // Karnataka 2nd PUC pass marks

            // Build response
            var result = new ConsolidatedExamResult
            {
                ExamId = examId,
                StudentId = studentId,
                ExamTitle = $"{exam.Subject} - {exam.Chapter}",
                McqScore = mcqScore,
                McqTotalMarks = mcqTotalMarks,
                McqResults = mcqResults,
                SubjectiveScore = subjectiveScore,
                SubjectiveTotalMarks = subjectiveTotalMarks,
                SubjectiveResults = subjectiveEvaluations.Select(e => new SubjectiveResultDto
                {
                    QuestionId = e.QuestionId,
                    QuestionNumber = e.QuestionNumber,
                    QuestionText = GetQuestionText(exam, e.QuestionId),
                    EarnedMarks = e.EarnedMarks,
                    MaxMarks = e.MaxMarks,
                    IsFullyCorrect = e.IsFullyCorrect,
                    ExpectedAnswer = e.ExpectedAnswer,
                    StudentAnswerEcho = e.StudentAnswerEcho,
                    StepAnalysis = e.StepAnalysis.Select(s => new StepAnalysisDto
                    {
                        Step = s.Step,
                        Description = s.Description,
                        IsCorrect = s.IsCorrect,
                        MarksAwarded = s.MarksAwarded,
                        MaxMarksForStep = s.MaxMarksForStep,
                        Feedback = s.Feedback
                    }).ToList(),
                    OverallFeedback = e.OverallFeedback
                }).ToList(),
                GrandScore = grandScore,
                GrandTotalMarks = grandTotalMarks,
                Percentage = percentage,
                Grade = grade,
                Passed = passed,
                EvaluatedAt = writtenSubmission?.EvaluatedAt?.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return result;
        }

        /// <summary>
        /// Store exam for later retrieval (helper method for testing)
        /// Note: Exams are now automatically stored when generated via /api/exam/generate
        /// </summary>
        [HttpPost("store-exam")]
        public IActionResult StoreExam([FromBody] GeneratedExamResponse exam)
        {
            _examStorageService.StoreExam(exam);
            _logger.LogInformation("Stored exam {ExamId}", exam.ExamId);
            return Ok(new { message = "Exam stored successfully", examId = exam.ExamId });
        }

        /// <summary>
        /// Get a previously generated exam by its ID
        /// </summary>
        [HttpGet("get/{examId}")]
        [ProducesResponseType(typeof(GeneratedExamResponse), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetExam(string examId)
        {
            var exam = _examStorageService.GetExam(examId);
            if (exam == null)
            {
                return NotFound(new { error = $"Exam {examId} not found" });
            }
            return Ok(exam);
        }

        /// <summary>
        /// List all stored exam IDs
        /// </summary>
        [HttpGet("list")]
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        public IActionResult ListExams()
        {
            var examIds = _examStorageService.GetAllExamIds();
            return Ok(new { exams = examIds, count = examIds.Count() });
        }

        /// <summary>
        /// Normalize OCR text containing mathematical expressions
        /// Converts noisy OCR output into clean, evaluable mathematical notation
        /// </summary>
        /// <param name="request">OCR text to normalize</param>
        /// <returns>Normalized mathematical text</returns>
        [HttpPost("normalize-ocr")]
        [ProducesResponseType(typeof(DTOs.MathNormalizationResult), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<DTOs.MathNormalizationResult>> NormalizeOcrText([FromBody] NormalizeOcrRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.OcrText))
            {
                return BadRequest(new { error = "ocrText is required" });
            }

            _logger.LogInformation("Normalizing OCR text ({Length} chars)", request.OcrText.Length);

            var result = await _mathNormalizer.NormalizeAsync(request.OcrText);

            return Ok(new DTOs.MathNormalizationResult
            {
                NormalizedAnswer = result.NormalizedAnswer,
                OriginalText = result.OriginalText,
                WasModified = result.WasModified
            });
        }

        // Private helper methods
        // NOTE: Heavy processing (ProcessWrittenSubmissionAsync) moved to Azure Functions
        // This API only handles upload + enqueue, status queries, and result retrieval

        private List<McqQuestion> GetMcqQuestions(GeneratedExamResponse exam)
        {
            var mcqQuestions = new List<McqQuestion>();

            if (exam.Parts == null || exam.Parts.Count == 0)
            {
                return mcqQuestions;
            }

            // Get Part A (MCQ questions)
            foreach (var part in exam.Parts)
            {
                if (part.QuestionType.Contains("MCQ", StringComparison.OrdinalIgnoreCase))
                {
                    mcqQuestions.AddRange(part.Questions.Select(q => new McqQuestion
                    {
                        QuestionId = q.QuestionId,
                        CorrectAnswer = q.CorrectAnswer,
                        Marks = part.MarksPerQuestion
                    }));
                }
            }

            return mcqQuestions;
        }

        private string GetQuestionText(GeneratedExamResponse exam, string questionId)
        {
            foreach (var part in exam.Parts)
            {
                var question = part.Questions.FirstOrDefault(q => q.QuestionId == questionId);
                if (question != null)
                {
                    return question.QuestionText;
                }
            }
            return string.Empty;
        }

        private string CalculateGrade(double percentage)
        {
            return percentage switch
            {
                >= 90 => "A+",
                >= 80 => "A",
                >= 70 => "B+",
                >= 60 => "B",
                >= 50 => "C+",
                >= 40 => "C",
                >= 35 => "D",
                _ => "F"
            };
        }

        private class McqQuestion
        {
            public string QuestionId { get; set; } = string.Empty;
            public string CorrectAnswer { get; set; } = string.Empty;
            public int Marks { get; set; }
        }

        #region Rubric Management Endpoints

        /// <summary>
        /// Get rubric for a specific question in an exam
        /// </summary>
        /// <param name="examId">Exam ID</param>
        /// <param name="questionId">Question ID</param>
        /// <returns>Rubric with marking steps</returns>
        [HttpGet("rubric/{examId}/{questionId}")]
        [ProducesResponseType(typeof(RubricResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RubricResponseDto>> GetRubric(string examId, string questionId)
        {
            _logger.LogInformation("Getting rubric for Exam={ExamId}, Question={QuestionId}", examId, questionId);

            var rubric = await _rubricService.GetRubricAsync(examId, questionId);

            if (rubric == null)
            {
                return NotFound(new { error = $"No rubric found for exam {examId}, question {questionId}" });
            }

            return Ok(rubric);
        }

        /// <summary>
        /// Get all rubrics for an exam
        /// </summary>
        /// <param name="examId">Exam ID</param>
        /// <returns>List of all rubrics for the exam</returns>
        [HttpGet("rubrics/{examId}")]
        [ProducesResponseType(typeof(List<RubricResponseDto>), 200)]
        public async Task<ActionResult<List<RubricResponseDto>>> GetExamRubrics(string examId)
        {
            _logger.LogInformation("Getting all rubrics for Exam={ExamId}", examId);

            var rubrics = await _rubricService.GetRubricsForExamAsync(examId);
            return Ok(rubrics);
        }

        /// <summary>
        /// Create or update a rubric for a question
        /// Used for teacher/admin rubric override
        /// </summary>
        /// <param name="request">Rubric creation request</param>
        /// <returns>Success message</returns>
        [HttpPost("rubric")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> CreateRubric([FromBody] RubricCreateRequest request)
        {
            if (request.Steps == null || request.Steps.Count == 0)
            {
                return BadRequest(new { error = "At least one rubric step is required" });
            }

            // Validate that sum of step marks equals total marks
            var stepMarksSum = request.Steps.Sum(s => s.Marks);
            if (stepMarksSum != request.TotalMarks)
            {
                return BadRequest(new
                {
                    error = $"Sum of step marks ({stepMarksSum}) must equal total marks ({request.TotalMarks})"
                });
            }

            _logger.LogInformation("Creating rubric for Exam={ExamId}, Question={QuestionId} with {StepCount} steps",
                request.ExamId, request.QuestionId, request.Steps.Count);

            await _rubricService.SaveRubricAsync(request);

            return Ok(new
            {
                message = "Rubric saved successfully",
                examId = request.ExamId,
                questionId = request.QuestionId,
                stepCount = request.Steps.Count,
                totalMarks = request.TotalMarks
            });
        }

        /// <summary>
        /// Batch create rubrics for multiple questions in an exam
        /// </summary>
        /// <param name="request">Batch rubric creation request</param>
        /// <returns>Success message</returns>
        [HttpPost("rubrics/batch")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> CreateRubricsBatch([FromBody] BatchRubricCreateRequest request)
        {
            if (request.Rubrics == null || request.Rubrics.Count == 0)
            {
                return BadRequest(new { error = "At least one rubric is required" });
            }

            // Validate each rubric
            foreach (var rubric in request.Rubrics)
            {
                if (rubric.Steps == null || rubric.Steps.Count == 0)
                {
                    return BadRequest(new
                    {
                        error = $"Rubric for question {rubric.QuestionId} must have at least one step"
                    });
                }

                var stepMarksSum = rubric.Steps.Sum(s => s.Marks);
                if (stepMarksSum != rubric.TotalMarks)
                {
                    return BadRequest(new
                    {
                        error = $"Question {rubric.QuestionId}: Sum of step marks ({stepMarksSum}) must equal total marks ({rubric.TotalMarks})"
                    });
                }
            }

            _logger.LogInformation("Batch creating {Count} rubrics for Exam={ExamId}",
                request.Rubrics.Count, request.ExamId);

            await _rubricService.SaveRubricsBatchAsync(request.ExamId, request.Rubrics);

            return Ok(new
            {
                message = "Rubrics saved successfully",
                examId = request.ExamId,
                rubricCount = request.Rubrics.Count
            });
        }

        /// <summary>
        /// Delete a rubric for a specific question
        /// </summary>
        /// <param name="examId">Exam ID</param>
        /// <param name="questionId">Question ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("rubric/{examId}/{questionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> DeleteRubric(string examId, string questionId)
        {
            var existing = await _rubricService.GetRubricAsync(examId, questionId);
            if (existing == null)
            {
                return NotFound(new { error = $"No rubric found for exam {examId}, question {questionId}" });
            }

            _logger.LogInformation("Deleting rubric for Exam={ExamId}, Question={QuestionId}", examId, questionId);

            await _rubricService.DeleteRubricAsync(examId, questionId);

            return Ok(new
            {
                message = "Rubric deleted successfully",
                examId = examId,
                questionId = questionId
            });
        }

        /// <summary>
        /// Generate a default rubric for a question (preview, not saved)
        /// </summary>
        /// <param name="questionText">Question text</param>
        /// <param name="modelAnswer">Model answer</param>
        /// <param name="totalMarks">Total marks for the question</param>
        /// <returns>Generated rubric steps</returns>
        [HttpGet("rubric/generate-preview")]
        [ProducesResponseType(typeof(List<StepRubricItemDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<List<StepRubricItemDto>>> GenerateRubricPreview(
            [FromQuery] string questionText,
            [FromQuery] string modelAnswer,
            [FromQuery] int totalMarks)
        {
            if (totalMarks <= 0)
            {
                return BadRequest(new { error = "totalMarks must be greater than 0" });
            }

            _logger.LogInformation("Generating rubric preview for {TotalMarks} marks question", totalMarks);

            var steps = await _rubricService.GenerateDefaultRubricAsync(
                questionText ?? string.Empty,
                modelAnswer ?? string.Empty,
                totalMarks);

            var dtoSteps = steps.Select(s => new StepRubricItemDto
            {
                StepNumber = s.StepNumber,
                Description = s.Description,
                Marks = s.Marks
            }).ToList();

            return Ok(dtoSteps);
        }

        #endregion
    }
}
