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
                // === REQUEST LOGGING ===
                Console.WriteLine("\n" + new string('=', 80));
                Console.WriteLine("📥 MCQ SUBMIT - REQUEST RECEIVED");
                Console.WriteLine(new string('=', 80));
                Console.WriteLine($"⏰ Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"📚 Exam ID: {request.ExamId}");
                Console.WriteLine($"👤 Student ID: {request.StudentId}");
                Console.WriteLine($"📝 Answers Count: {request.Answers?.Count ?? 0}");
                if (request.Answers != null)
                {
                    foreach (var a in request.Answers.Take(5))
                        Console.WriteLine($"   - Q:{a.QuestionId} → {a.SelectedOption}");
                    if (request.Answers.Count > 5)
                        Console.WriteLine($"   ... and {request.Answers.Count - 5} more");
                }
                Console.WriteLine(new string('-', 80));
                
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

                    // Normalize both answers for comparison (handle "A)", "A", "option text" formats)
                    var studentAnswer = NormalizeAnswer(answer.SelectedOption);
                    var correctAnswer = NormalizeAnswer(question.CorrectAnswer);
                    var isCorrect = studentAnswer.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase);
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

                // Save MCQ results to evaluation-results blob (same format as subjective)
                try
                {
                    var grade = response.Percentage >= 90 ? "A" :
                               response.Percentage >= 80 ? "B" :
                               response.Percentage >= 70 ? "C" :
                               response.Percentage >= 60 ? "D" : "F";

                    var mcqResult = new
                    {
                        mcqSubmissionId = submission.McqSubmissionId,
                        examId = request.ExamId,
                        studentId = request.StudentId,
                        totalScore = totalScore,
                        maxPossibleScore = totalMarks,
                        percentage = response.Percentage,
                        grade = grade,
                        evaluatedAt = DateTime.UtcNow,
                        questions = results.Select(r => new
                        {
                            questionId = r.QuestionId,
                            selectedOption = r.SelectedOption,
                            correctAnswer = r.CorrectAnswer,
                            isCorrect = r.IsCorrect,
                            marksAwarded = r.MarksAwarded,
                            feedback = r.IsCorrect ? "Correct answer" : $"Incorrect. Correct answer is: {r.CorrectAnswer}"
                        }).ToList()
                    };

                    var resultJson = System.Text.Json.JsonSerializer.Serialize(mcqResult, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    var blobPath = $"mcq-{submission.McqSubmissionId}/evaluation-result.json";
                    await _fileStorageService.SaveJsonToBlobAsync(resultJson, blobPath, "evaluation-results");

                    _logger.LogInformation("MCQ results saved to blob: {BlobPath}", blobPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save MCQ results to blob storage (non-critical)");
                }

                _logger.LogInformation(
                    "MCQ submission completed: {Score}/{TotalMarks} ({Percentage}%)",
                    totalScore,
                    totalMarks,
                    response.Percentage);

                // === RESPONSE LOGGING ===
                Console.WriteLine("📤 MCQ SUBMIT - RESPONSE");
                Console.WriteLine(new string('-', 80));
                Console.WriteLine($"✅ Submission ID: {response.McqSubmissionId}");
                Console.WriteLine($"📊 Score: {response.Score}/{response.TotalMarks} ({response.Percentage}%)");
                Console.WriteLine($"📝 Results:");
                foreach (var r in response.Results.Take(5))
                    Console.WriteLine($"   - Q:{r.QuestionId} Selected:{r.SelectedOption} Correct:{r.CorrectAnswer} {(r.IsCorrect ? "✅" : "❌")}");
                if (response.Results.Count > 5)
                    Console.WriteLine($"   ... and {response.Results.Count - 5} more");
                Console.WriteLine(new string('=', 80) + "\n");

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
            [FromForm] string? mcqAnswers = null,
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
                // === REQUEST LOGGING ===
                Console.WriteLine("\n" + new string('=', 80));
                Console.WriteLine("📥 UPLOAD WRITTEN - REQUEST RECEIVED");
                Console.WriteLine(new string('=', 80));
                Console.WriteLine($"⏰ Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"🔗 Correlation ID: {correlationId}");
                Console.WriteLine($"📚 Exam ID: {examId ?? "null"}");
                Console.WriteLine($"👤 Student ID: {studentId ?? "null"}");
                Console.WriteLine($"📁 Files Count: {files?.Count ?? 0}");
                Console.WriteLine($"📝 MCQ Answers: {(string.IsNullOrEmpty(mcqAnswers) ? "None" : "Provided")}");
                if (files != null)
                {
                    long totalSize = 0;
                    foreach (var f in files)
                    {
                        Console.WriteLine($"   - {f.FileName} ({f.Length / 1024.0:F1} KB) [{f.ContentType}]");
                        totalSize += f.Length;
                    }
                    Console.WriteLine($"   Total Size: {totalSize / 1024.0:F1} KB");
                }
                Console.WriteLine(new string('-', 80));
                
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
                    
                    _logger.LogInformation("[BLOB_UPLOAD_START] Uploading file {FileName} ({Size} bytes)", 
                        file.FileName, file.Length);
                    
                    try
                    {
                        var path = await _fileStorageService.SaveFileAsync(file, examId, studentId);
                        filePaths.Add(path);
                        
                        _logger.LogInformation("[BLOB_UPLOAD_SUCCESS] File uploaded to blob path: {Path}", path);
                        Console.WriteLine($"✅ Uploaded: {file.FileName} → BLOB PATH: {path}");
                    }
                    catch (Exception uploadEx)
                    {
                        _logger.LogError(uploadEx, "[BLOB_UPLOAD_ERROR] Failed to upload {FileName}: {Error}", 
                            file.FileName, uploadEx.Message);
                        throw new Exception($"Failed to upload file '{file.FileName}': {uploadEx.Message}", uploadEx);
                    }
                }

                _logger.LogInformation("[BLOB_UPLOADED] {FileCount} files saved to storage", filePaths.Count);
                Console.WriteLine($"📦 Total blob paths stored: {filePaths.Count}");
                foreach (var fp in filePaths)
                {
                    Console.WriteLine($"   - {fp}");
                }

                // === MCQ ANSWERS PARSING & EVALUATION ===
                
                List<SchoolAiChatbotBackend.Models.McqAnswerDto>? mcqAnswersList = null;
                decimal mcqScore = 0;
                decimal mcqTotalMarks = 0;
                bool mcqAnswersProvided = !string.IsNullOrEmpty(mcqAnswers);
                
                Console.WriteLine($"🔍 MCQ ANSWERS CHECK: Provided={mcqAnswersProvided}, Length={mcqAnswers?.Length ?? 0}");
                
                if (mcqAnswersProvided)
                {
                    try
                    {
                        Console.WriteLine($"📝 RAW MCQ ANSWERS JSON: {mcqAnswers}");
                        mcqAnswersList = System.Text.Json.JsonSerializer.Deserialize<List<SchoolAiChatbotBackend.Models.McqAnswerDto>>(mcqAnswers);
                        _logger.LogInformation("[MCQ_ANSWERS] Parsed {Count} MCQ answers", mcqAnswersList?.Count ?? 0);
                        Console.WriteLine($"📝 MCQ Answers parsed: {mcqAnswersList?.Count ?? 0} answers");
                        
                        if (mcqAnswersList != null && mcqAnswersList.Count > 0)
                        {
                            foreach (var ans in mcqAnswersList.Take(5))
                                Console.WriteLine($"   - Q:{ans.QuestionId} → {ans.SelectedOption}");
                            if (mcqAnswersList.Count > 5)
                                Console.WriteLine($"   ... and {mcqAnswersList.Count - 5} more");
                            
                            // Evaluate MCQ answers
                            var exam = _examStorageService.GetExam(examId);
                            if (exam != null)
                            {
                                var mcqQuestions = GetMcqQuestions(exam);
                                Console.WriteLine($"📚 Found {mcqQuestions.Count} MCQ questions in exam");
                                
                                // Create new list with evaluated answers in the required format
                                var evaluatedAnswers = new List<SchoolAiChatbotBackend.Models.McqAnswerDto>();
                                int matchedCount = 0;
                                
                                foreach (var answer in mcqAnswersList)
                                {
                                    var question = mcqQuestions.FirstOrDefault(q => q.QuestionId == answer.QuestionId);
                                    if (question != null)
                                    {
                                        matchedCount++;
                                        var studentAnswer = NormalizeAnswer(answer.SelectedOption);
                                        var correctAnswer = NormalizeAnswer(question.CorrectAnswer);
                                        var isCorrect = studentAnswer.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase);
                                        var awardedScore = isCorrect ? question.Marks : 0;
                                        
                                        mcqScore += awardedScore;
                                        mcqTotalMarks += question.Marks;
                                        
                                        // Create evaluation in the new format
                                        evaluatedAnswers.Add(new SchoolAiChatbotBackend.Models.McqAnswerDto
                                        {
                                            QuestionNumber = question.QuestionNumber,
                                            QuestionText = question.QuestionText,
                                            ExtractedAnswer = answer.SelectedOption,
                                            ModelAnswer = question.CorrectAnswer,
                                            MaxScore = question.Marks,
                                            AwardedScore = awardedScore,
                                            Feedback = isCorrect ? "Correct!" : $"Incorrect. Correct answer is: {question.CorrectAnswer}",
                                            // Legacy fields for backward compatibility
                                            QuestionId = question.QuestionId,
                                            SelectedOption = answer.SelectedOption
                                        });
                                    }
                                    else
                                    {
                                        Console.WriteLine($"⚠️ Question not found: {answer.QuestionId}");
                                        // Store unevaluated answer to preserve data
                                        evaluatedAnswers.Add(new SchoolAiChatbotBackend.Models.McqAnswerDto
                                        {
                                            QuestionNumber = 0,
                                            QuestionText = "Question not found",
                                            ExtractedAnswer = answer.SelectedOption,
                                            ModelAnswer = "",
                                            MaxScore = 0,
                                            AwardedScore = 0,
                                            Feedback = "Question ID not found in exam",
                                            QuestionId = answer.QuestionId,
                                            SelectedOption = answer.SelectedOption
                                        });
                                    }
                                }
                                
                                Console.WriteLine($"✅ Matched {matchedCount}/{mcqAnswersList.Count} questions");
                                
                                // ALWAYS use evaluated list (even if empty or partial) to preserve data
                                mcqAnswersList = evaluatedAnswers;
                                
                                _logger.LogInformation("[MCQ_EVALUATION] Score: {Score}/{Total}, Matched: {Matched}/{Total}", 
                                    mcqScore, mcqTotalMarks, matchedCount, evaluatedAnswers.Count);
                                Console.WriteLine($"📊 MCQ Score: {mcqScore}/{mcqTotalMarks}");
                            }
                            else
                            {
                                Console.WriteLine($"⚠️ Exam not found for evaluation: {examId}");
                            }
                        }
                    }
                    catch (Exception jsonEx)
                    {
                        _logger.LogWarning(jsonEx, "[MCQ_ANSWERS] Failed to parse MCQ answers JSON: {Error}", jsonEx.Message);
                        Console.WriteLine($"⚠️ MCQ Answers parsing failed: {jsonEx.Message}");
                        Console.WriteLine($"   JSON: {mcqAnswers}");
                        // Don't fail the request, just log the warning and continue without MCQ answers
                    }
                }

                // === DATABASE PHASE ===

                // CRITICAL FIX: Save MCQ data if ANY answers were provided, even if evaluation failed
                // This ensures we never lose submitted data
                bool saveMcqData = mcqAnswersProvided && mcqAnswersList != null && mcqAnswersList.Count > 0;
                
                var submission = new WrittenSubmission
                {
                    WrittenSubmissionId = Guid.NewGuid().ToString(),
                    ExamId = examId,
                    StudentId = studentId,
                    FilePaths = filePaths,
                    McqAnswers = mcqAnswersList,  // Always set if we have data
                    McqScore = saveMcqData ? mcqScore : null,
                    McqTotalMarks = saveMcqData ? mcqTotalMarks : null,
                    Status = SubmissionStatus.PendingEvaluation,
                    SubmittedAt = DateTime.UtcNow
                };

                // === DEBUG LOGGING BEFORE DB SAVE ===
                Console.WriteLine($"💾 DATABASE SAVE - MCQ DATA:");
                Console.WriteLine($"   MCQ Answers Provided: {mcqAnswersProvided}");
                Console.WriteLine($"   MCQ Answers List Count: {mcqAnswersList?.Count ?? 0}");
                Console.WriteLine($"   Save MCQ Data: {saveMcqData}");
                Console.WriteLine($"   McqScore: {submission.McqScore}");
                Console.WriteLine($"   McqTotalMarks: {submission.McqTotalMarks}");
                Console.WriteLine($"   McqAnswersJson: {(submission.McqAnswersJson != null ? $"Length={submission.McqAnswersJson.Length}" : "NULL")}");
                if (submission.McqAnswersJson != null)
                {
                    Console.WriteLine($"   McqAnswersJson Preview: {(submission.McqAnswersJson.Length > 200 ? submission.McqAnswersJson.Substring(0, 200) + "..." : submission.McqAnswersJson)}");
                }

                await _examRepository.SaveWrittenSubmissionAsync(submission);
                _logger.LogInformation(
                    "[DB_SAVED] Submission {SubmissionId} created - MCQ: {HasMcq}, Score: {McqScore}/{McqTotal}, FilePaths: {FileCount}",
                    submission.WrittenSubmissionId,
                    submission.McqAnswersJson != null,
                    submission.McqScore,
                    submission.McqTotalMarks,
                    filePaths.Count);
                Console.WriteLine($"✅ Saved to database successfully");

                // === QUEUE PHASE ===

                var queueMessage = new WrittenSubmissionQueueMessage
                {
                    WrittenSubmissionId = Guid.Parse(submission.WrittenSubmissionId),
                    ExamId = examId,
                    StudentId = studentId,
                    FilePaths = filePaths,
                    SubmittedAt = DateTime.UtcNow,
                    Priority = 1, // 1 = normal priority
                    RetryCount = 0
                };
                await _queueService.EnqueueAsync(QueueNames.WrittenSubmissionProcessing, queueMessage);

                _logger.LogInformation(
                    "[QUEUE_ENQUEUED] Submission {SubmissionId} sent to {QueueName}",
                    submission.WrittenSubmissionId,
                    QueueNames.WrittenSubmissionProcessing);

                // === RESPONSE LOGGING ===
                Console.WriteLine("📤 UPLOAD WRITTEN - RESPONSE");
                Console.WriteLine(new string('-', 80));
                Console.WriteLine($"✅ Submission ID: {submission.WrittenSubmissionId}");
                Console.WriteLine($"📊 Status: PendingEvaluation (0)");
                Console.WriteLine($"📁 Files Saved: {files.Count}");
                Console.WriteLine($"📨 Queued for processing: {QueueNames.WrittenSubmissionProcessing}");
                Console.WriteLine($"💡 Next: Poll /api/exam/submission-status/{submission.WrittenSubmissionId}");
                Console.WriteLine(new string('=', 80) + "\n");

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
                _logger.LogError(ex, "[UPLOAD_FAILED] Unexpected error: {Message}", ex.Message);
                Console.WriteLine($"❌ UPLOAD ERROR: {ex.Message}");
                Console.WriteLine($"   Stack: {ex.StackTrace}");
                return StatusCode(500, new { 
                    error = "Internal server error", 
                    message = ex.Message,
                    correlationId 
                });
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
                // === REQUEST LOGGING ===
                Console.WriteLine("\n" + new string('=', 80));
                Console.WriteLine("📥 SUBMISSION STATUS - REQUEST");
                Console.WriteLine(new string('=', 80));
                Console.WriteLine($"⏰ Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"📌 Submission ID: {writtenSubmissionId}");
                Console.WriteLine(new string('-', 80));
                
                _logger.LogInformation("Checking status for submission {SubmissionId}", writtenSubmissionId);

                var submission = await _examRepository.GetWrittenSubmissionAsync(writtenSubmissionId);
                if (submission == null)
                {
                    return NotFound(new { error = "Submission not found" });
                }

                // Status Flow: 0=Uploaded, 1=OCR Processing, 2=Evaluating, 3=Results Ready, 4=Error
                // If EvaluationResultBlobPath is set, treat as completed regardless of status code
                bool isResultsReady = !string.IsNullOrEmpty(submission.EvaluationResultBlobPath)
                    || submission.Status == SubmissionStatus.ResultsReady 
                    || submission.Status == SubmissionStatus.Completed;
                
                // Only treat as error if NOT results ready (blob path takes priority)
                bool isError = !isResultsReady && (submission.Status == SubmissionStatus.Error 
                    || submission.Status == SubmissionStatus.Failed);

                // Map status to user-friendly message with polling hints
                string statusMessage;
                int pollIntervalSeconds;
                
                if (isResultsReady)
                {
                    statusMessage = "✅ Results Ready!";
                    pollIntervalSeconds = 0; // No more polling needed
                }
                else if (isError)
                {
                    statusMessage = "❌ Error occurred";
                    pollIntervalSeconds = 0; // No more polling needed
                }
                else
                {
                    (statusMessage, pollIntervalSeconds) = submission.Status switch
                    {
                        SubmissionStatus.Uploaded => ("⏳ Uploaded - Processing will start soon", 5), // Poll in 5s
                        SubmissionStatus.OcrProcessing => ("📄 Reading your answer sheet...", 3), // Poll in 3s
                        SubmissionStatus.Evaluating => ("🤖 Evaluating your answers...", 5), // Poll in 5s
                        _ => ("Processing...", 5)
                    };
                }

                var response = new SubmissionStatusResponse
                {
                    WrittenSubmissionId = writtenSubmissionId,
                    // Return Status=3 (Results Ready) if blob path exists, regardless of DB status
                    Status = isResultsReady ? "3" : ((int)submission.Status).ToString(),
                    StatusMessage = statusMessage,
                    PollIntervalSeconds = pollIntervalSeconds,
                    SubmittedAt = submission.SubmittedAt,
                    EvaluatedAt = submission.EvaluatedAt,
                    IsComplete = isResultsReady,
                    IsError = isError,
                    ErrorMessage = isError ? submission.ErrorMessage : null,
                    ExamId = submission.ExamId,
                    StudentId = submission.StudentId,
                    EvaluationResultBlobPath = isResultsReady ? submission.EvaluationResultBlobPath : null,
                    // Summary scores - only include when results are ready
                    TotalScore = isResultsReady ? submission.TotalScore : null,
                    MaxPossibleScore = isResultsReady ? submission.MaxPossibleScore : null,
                    Percentage = isResultsReady ? submission.Percentage : null,
                    Grade = isResultsReady ? submission.Grade : null
                };

                // If results are ready (Status=3 or has blob path), read full results from blob storage
                if (isResultsReady)
                {
                    if (!string.IsNullOrEmpty(submission.EvaluationResultBlobPath))
                    {
                        try
                        {
                            // Remove container prefix from blob path if present (stored path may include it)
                            var blobPath = submission.EvaluationResultBlobPath;
                            const string containerPrefix = "evaluation-results/";
                            if (blobPath.StartsWith(containerPrefix, StringComparison.OrdinalIgnoreCase))
                            {
                                blobPath = blobPath.Substring(containerPrefix.Length);
                            }
                            
                            var blobJson = await _fileStorageService.ReadJsonFromBlobAsync(
                                blobPath,
                                "evaluation-results");
                            
                            if (!string.IsNullOrEmpty(blobJson))
                            {
                                // Parse blob JSON dynamically - the blob structure is the authoritative format
                                using var jsonDoc = System.Text.Json.JsonDocument.Parse(blobJson);
                                var root = jsonDoc.RootElement;
                                
                                // Try to get scores from blob if not in DB
                                if (response.TotalScore == null || response.TotalScore == 0)
                                {
                                    if (root.TryGetProperty("totalScore", out var totalScoreProp))
                                        response.TotalScore = totalScoreProp.GetDecimal();
                                    if (root.TryGetProperty("maxPossibleScore", out var maxScoreProp))
                                        response.MaxPossibleScore = maxScoreProp.GetDecimal();
                                    if (root.TryGetProperty("percentage", out var pctProp))
                                        response.Percentage = pctProp.GetDecimal();
                                    if (root.TryGetProperty("grade", out var gradeProp))
                                        response.Grade = gradeProp.GetString();
                                }
                                
                                // Deserialize as object to return the full blob structure
                                var jsonOptions = new System.Text.Json.JsonSerializerOptions 
                                { 
                                    PropertyNameCaseInsensitive = true 
                                };
                                response.Result = System.Text.Json.JsonSerializer.Deserialize<object>(blobJson, jsonOptions);
                                
                                // === CONSOLE LOG: STATUS ENDPOINT RETURNING RESULTS ===
                                Console.WriteLine("\n" + new string('=', 80));
                                Console.WriteLine("✅ STATUS ENDPOINT - RESULTS READY");
                                Console.WriteLine(new string('=', 80));
                                Console.WriteLine($"📌 Submission ID: {writtenSubmissionId}");
                                Console.WriteLine($"📚 Exam ID: {submission.ExamId}");
                                Console.WriteLine($"👤 Student ID: {submission.StudentId}");
                                Console.WriteLine($"📊 Score: {response.TotalScore}/{response.MaxPossibleScore} ({response.Percentage:F2}%)");
                                Console.WriteLine($"🎓 Grade: {response.Grade}");
                                Console.WriteLine(new string('-', 80));
                                Console.WriteLine("📄 BLOB JSON PREVIEW (first 500 chars):");
                                Console.WriteLine(blobJson.Length > 500 ? blobJson.Substring(0, 500) + "..." : blobJson);
                                Console.WriteLine(new string('=', 80) + "\n");
                                
                                _logger.LogInformation("Loaded evaluation results from blob storage for {SubmissionId}", writtenSubmissionId);
                            }
                            else
                            {
                                _logger.LogWarning("Blob storage returned empty content for {SubmissionId}", writtenSubmissionId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error reading evaluation results from blob for {SubmissionId}", writtenSubmissionId);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("EvaluationResultBlobPath is empty for completed submission {SubmissionId}", writtenSubmissionId);
                    }
                }

                // === RESPONSE LOGGING ===
                Console.WriteLine("📤 SUBMISSION STATUS - RESPONSE");
                Console.WriteLine(new string('-', 80));
                Console.WriteLine($"📌 Submission ID: {response.WrittenSubmissionId}");
                Console.WriteLine($"📊 Status: {response.Status} - {response.StatusMessage}");
                Console.WriteLine($"✅ IsComplete: {response.IsComplete} | ❌ IsError: {response.IsError}");
                Console.WriteLine($"⏱️ Poll Interval: {response.PollIntervalSeconds}s");
                if (response.IsComplete)
                {
                    Console.WriteLine($"📈 Score: {response.TotalScore}/{response.MaxPossibleScore} ({response.Percentage:F2}%)");
                    Console.WriteLine($"🎓 Grade: {response.Grade}");
                    Console.WriteLine($"📁 Has Result Data: {response.Result != null}");
                }
                if (response.IsError)
                    Console.WriteLine($"⚠️ Error: {response.ErrorMessage}");
                Console.WriteLine(new string('=', 80) + "\n");

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
        /// Get evaluation result from blob storage for a submission
        /// This reads the complete evaluation JSON stored by the Azure Function
        /// Status must be 3 (Results Ready) to return data
        /// </summary>
        /// <param name="writtenSubmissionId">Written submission ID</param>
        /// <returns>Complete evaluation result JSON from blob storage</returns>
        [HttpGet("evaluation-result/{writtenSubmissionId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetEvaluationResultFromBlob(string writtenSubmissionId)
        {
            try
            {
                _logger.LogInformation("Fetching evaluation result from blob for submission {SubmissionId}", writtenSubmissionId);

                // Get submission to find the blob path
                var submission = await _examRepository.GetWrittenSubmissionAsync(writtenSubmissionId);
                if (submission == null)
                {
                    return NotFound(new { error = "Submission not found" });
                }

                // Status 3 = Results Ready (or has blob path which means results are ready)
                bool isResultsReady = submission.Status == SubmissionStatus.ResultsReady 
                    || submission.Status == SubmissionStatus.Completed
                    || !string.IsNullOrEmpty(submission.EvaluationResultBlobPath);

                // Check if results are ready
                if (!isResultsReady)
                {
                    return BadRequest(new { 
                        error = "Results not ready", 
                        status = (int)submission.Status,
                        statusMessage = submission.Status switch
                        {
                            SubmissionStatus.Uploaded => "Uploaded - Processing will start soon",
                            SubmissionStatus.OcrProcessing => "Reading your answer sheet...",
                            SubmissionStatus.Evaluating => "Evaluating your answers...",
                            SubmissionStatus.Error => "Error occurred",
                            _ => "Processing..."
                        }
                    });
                }

                // Check if blob path exists
                if (string.IsNullOrEmpty(submission.EvaluationResultBlobPath))
                {
                    return NotFound(new { error = "Evaluation result blob path not found" });
                }

                // Remove container prefix from blob path if present (stored path may include it)
                var blobPath = submission.EvaluationResultBlobPath;
                const string containerPrefix = "evaluation-results/";
                if (blobPath.StartsWith(containerPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    blobPath = blobPath.Substring(containerPrefix.Length);
                }

                // Read JSON from blob storage
                var jsonContent = await _fileStorageService.ReadJsonFromBlobAsync(
                    blobPath,
                    "evaluation-results");

                if (string.IsNullOrEmpty(jsonContent))
                {
                    return NotFound(new { error = "Evaluation result not found in blob storage" });
                }

                // Parse and return the JSON
                var evaluationResult = System.Text.Json.JsonSerializer.Deserialize<object>(jsonContent);
                
                // === DETAILED CONSOLE LOGGING FOR EVALUATION RESULT ===
                Console.WriteLine("\n" + new string('=', 80));
                Console.WriteLine("📋 EVALUATION RESULT RETURNED");
                Console.WriteLine(new string('=', 80));
                Console.WriteLine($"📌 Submission ID: {writtenSubmissionId}");
                Console.WriteLine($"📚 Exam ID: {submission.ExamId}");
                Console.WriteLine($"👤 Student ID: {submission.StudentId}");
                Console.WriteLine($"⏰ Evaluated At: {submission.EvaluatedAt}");
                Console.WriteLine($"📁 Blob Path: {submission.EvaluationResultBlobPath}");
                Console.WriteLine(new string('-', 80));
                Console.WriteLine("📊 SCORE SUMMARY:");
                Console.WriteLine($"   Total Score: {submission.TotalScore}/{submission.MaxPossibleScore}");
                Console.WriteLine($"   Percentage: {submission.Percentage:F2}%");
                Console.WriteLine($"   Grade: {submission.Grade}");
                Console.WriteLine(new string('-', 80));
                Console.WriteLine("📄 FULL EVALUATION JSON:");
                Console.WriteLine(jsonContent);
                Console.WriteLine(new string('=', 80) + "\n");
                
                _logger.LogInformation(
                    "[EVALUATION_RESULT] Returned for SubmissionId={SubmissionId} ExamId={ExamId} StudentId={StudentId} Score={Score}/{Max} ({Pct}%)",
                    writtenSubmissionId, submission.ExamId, submission.StudentId, 
                    submission.TotalScore, submission.MaxPossibleScore, submission.Percentage);
                
                return Ok(new
                {
                    writtenSubmissionId = writtenSubmissionId,
                    examId = submission.ExamId,
                    studentId = submission.StudentId,
                    evaluatedAt = submission.EvaluatedAt,
                    blobPath = submission.EvaluationResultBlobPath,
                    // Summary from WrittenSubmissions table
                    summary = new
                    {
                        totalScore = submission.TotalScore,
                        maxPossibleScore = submission.MaxPossibleScore,
                        percentage = submission.Percentage,
                        grade = submission.Grade
                    },
                    // Full evaluation result from blob
                    evaluationResult = evaluationResult
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching evaluation result from blob for submission {SubmissionId}", writtenSubmissionId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Internal method to get consolidated exam result (reusable for status endpoint)
        /// </summary>
        private async Task<ConsolidatedExamResult> GetConsolidatedResultInternal(string examId, string studentId)
        {
            try
            {
                _logger.LogInformation("Fetching result for exam {ExamId}, student {StudentId}", examId, studentId);

                // Load exam from storage service
                var exam = _examStorageService.GetExam(examId);
                if (exam == null)
                {
                    _logger.LogWarning("Exam {ExamId} not found in storage", examId);
                    throw new KeyNotFoundException($"Exam {examId} not found. Please generate the exam first using /api/exam/generate");
                }

            _logger.LogInformation("Loaded exam {ExamId}: {Subject} - Parts count: {PartCount}", 
                examId, exam.Subject, exam.Parts?.Count ?? 0);
            if (exam.Parts != null)
            {
                foreach (var part in exam.Parts)
                {
                    _logger.LogInformation("  Part {PartName}: {QuestionCount} questions", 
                        part.PartName, part.Questions?.Count ?? 0);
                }
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
                // ALWAYS use blob storage ONLY for evaluation results
                if (!string.IsNullOrEmpty(writtenSubmission.EvaluationResultBlobPath))
                {
                    _logger.LogInformation("Loading subjective results from blob: {BlobPath}", writtenSubmission.EvaluationResultBlobPath);
                    
                    // Remove container prefix if present
                    var blobPath = writtenSubmission.EvaluationResultBlobPath;
                    const string containerPrefix = "evaluation-results/";
                    if (blobPath.StartsWith(containerPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        blobPath = blobPath.Substring(containerPrefix.Length);
                    }

                    // Read evaluation result from blob - NO DATABASE FALLBACK
                    var jsonContent = await _fileStorageService.ReadJsonFromBlobAsync(blobPath, "evaluation-results");
                    
                    if (string.IsNullOrEmpty(jsonContent))
                    {
                        _logger.LogError("Blob content is empty for path: {BlobPath}", writtenSubmission.EvaluationResultBlobPath);
                        throw new InvalidOperationException($"Evaluation result blob is empty. Please re-run evaluation for submission {writtenSubmission.WrittenSubmissionId}");
                    }

                    // Parse blob JSON to extract subjective evaluations
                    var blobResult = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(jsonContent);
                    
                    // Extract subjectiveResults array from blob JSON
                    if (!blobResult.TryGetProperty("subjectiveResults", out var subjectiveResultsElement))
                    {
                        _logger.LogError("Blob JSON does not contain subjectiveResults property. Blob content: {Content}", jsonContent.Substring(0, Math.Min(500, jsonContent.Length)));
                        throw new InvalidOperationException($"Invalid evaluation result format in blob. Please re-run evaluation for submission {writtenSubmission.WrittenSubmissionId}");
                    }

                    subjectiveEvaluations = System.Text.Json.JsonSerializer.Deserialize<List<SubjectiveEvaluationResult>>(
                        subjectiveResultsElement.GetRawText(),
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    ) ?? new List<SubjectiveEvaluationResult>();
                    
                    _logger.LogInformation("Successfully loaded {Count} subjective results from blob ONLY", subjectiveEvaluations.Count);
                }
                else
                {
                    // No blob path means evaluation is not complete yet
                    _logger.LogWarning("No blob path found for WrittenSubmissionId {WrittenSubmissionId}. Evaluation may still be in progress.", writtenSubmission.WrittenSubmissionId);
                    subjectiveEvaluations = new List<SubjectiveEvaluationResult>();
                }
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
                SubjectiveResults = subjectiveEvaluations.Select(e =>
                {
                    try
                    {
                        // Get question text - use multiple strategies to find it
                        var questionText = GetQuestionText(exam, e.QuestionId);
                        
                        // If not found by exact ID, try by question number
                        if (string.IsNullOrEmpty(questionText) && exam.Parts != null)
                        {
                            var allQuestions = exam.Parts.SelectMany(p => p.Questions).ToList();
                            var questionByNumber = allQuestions.ElementAtOrDefault(e.QuestionNumber - 1);
                            if (questionByNumber != null)
                            {
                                questionText = questionByNumber.QuestionText ?? string.Empty;
                                _logger.LogInformation("Found question text by number {QuestionNumber} instead of ID {QuestionId}", e.QuestionNumber, e.QuestionId);
                            }
                        }
                        
                        // Fallback: use expected answer as context if still empty
                        if (string.IsNullOrEmpty(questionText) && !string.IsNullOrEmpty(e.ExpectedAnswer))
                        {
                            questionText = $"[Question {e.QuestionNumber}] (View expected answer for context)";
                            _logger.LogWarning("Could not find question text for ID={QuestionId} Num={QuestionNumber}, using fallback", e.QuestionId, e.QuestionNumber);
                        }
                        
                        return new SubjectiveResultDto
                        {
                            QuestionId = e.QuestionId,
                            QuestionNumber = e.QuestionNumber,
                            QuestionText = questionText ?? string.Empty,
                            EarnedMarks = e.EarnedMarks,
                            MaxMarks = e.MaxMarks,
                            IsFullyCorrect = e.IsFullyCorrect,
                            ExpectedAnswer = e.ExpectedAnswer ?? string.Empty,
                            StudentAnswerEcho = e.StudentAnswerEcho ?? string.Empty,
                            StepAnalysis = e.StepAnalysis?.Select(s => new StepAnalysisDto
                            {
                                Step = s.Step,
                                Description = s.Description ?? string.Empty,
                                IsCorrect = s.IsCorrect,
                                MarksAwarded = s.MarksAwarded,
                                MaxMarksForStep = s.MaxMarksForStep,
                                Feedback = s.Feedback ?? string.Empty
                            }).ToList() ?? new List<StepAnalysisDto>(),
                            OverallFeedback = e.OverallFeedback ?? string.Empty
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing subjective result for question {QuestionId}", e.QuestionId);
                        return new SubjectiveResultDto
                        {
                            QuestionId = e.QuestionId,
                            QuestionNumber = e.QuestionNumber,
                            QuestionText = "Error loading question",
                            EarnedMarks = e.EarnedMarks,
                            MaxMarks = e.MaxMarks,
                            IsFullyCorrect = e.IsFullyCorrect,
                            ExpectedAnswer = e.ExpectedAnswer ?? string.Empty,
                            StudentAnswerEcho = e.StudentAnswerEcho ?? string.Empty,
                            StepAnalysis = new List<StepAnalysisDto>(),
                            OverallFeedback = e.OverallFeedback ?? string.Empty
                        };
                    }
                }).ToList(),
                GrandScore = grandScore,
                GrandTotalMarks = grandTotalMarks,
                Percentage = percentage,
                Grade = grade,
                Passed = passed,
                EvaluatedAt = writtenSubmission?.EvaluatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            _logger.LogInformation("Successfully built consolidated result for {ExamId}/{StudentId}", examId, studentId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetConsolidatedResultInternal for {ExamId}/{StudentId}", examId, studentId);
            throw;
        }
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
                        QuestionNumber = q.QuestionNumber,
                        QuestionText = q.QuestionText,
                        CorrectAnswer = q.CorrectAnswer,
                        Marks = part.MarksPerQuestion
                    }));
                }
            }

            return mcqQuestions;
        }

        private string GetQuestionText(GeneratedExamResponse exam, string questionId)
        {
            if (exam == null)
            {
                _logger.LogWarning("Exam is null when trying to get question text for {QuestionId}", questionId);
                return string.Empty;
            }

            // First, try to find in Parts (structured format)
            if (exam.Parts != null && exam.Parts.Any())
            {
                foreach (var part in exam.Parts)
                {
                    if (part.Questions == null) continue;
                    
                    var question = part.Questions.FirstOrDefault(q => q.QuestionId == questionId);
                    if (question != null)
                    {
                        _logger.LogDebug("Found question text in Parts for {QuestionId}: {Text}", 
                            questionId, question.QuestionText?.Substring(0, Math.Min(50, question.QuestionText?.Length ?? 0)) ?? "null");
                        return question.QuestionText ?? string.Empty;
                    }
                }
            }

            // Second, try to find in flat Questions list (legacy format)
            if (exam.Questions != null && exam.Questions.Any())
            {
                var question = exam.Questions.FirstOrDefault(q => q.QuestionId == questionId);
                if (question != null)
                {
                    _logger.LogDebug("Found question text in Questions list for {QuestionId}: {Text}", 
                        questionId, question.QuestionText?.Substring(0, Math.Min(50, question.QuestionText?.Length ?? 0)) ?? "null");
                    return question.QuestionText ?? string.Empty;
                }
            }
            
            // Log available questions for debugging
            var availableFromParts = exam.Parts?.SelectMany(p => p.Questions?.Select(q => q.QuestionId) ?? Array.Empty<string>()) ?? Array.Empty<string>();
            var availableFromQuestions = exam.Questions?.Select(q => q.QuestionId) ?? Array.Empty<string>();
            _logger.LogWarning("Question not found for QuestionId={QuestionId}. Available in Parts: [{PartsQuestions}]. Available in Questions: [{FlatQuestions}]", 
                questionId, 
                string.Join(", ", availableFromParts),
                string.Join(", ", availableFromQuestions));
            
            return string.Empty;
        }

        private string NormalizeAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
                return string.Empty;

            // Remove whitespace and convert to uppercase
            answer = answer.Trim().ToUpperInvariant();

            // Extract just the option letter if format is "A) text" or "A. text" or "A text"
            if (answer.Length >= 2 && char.IsLetter(answer[0]) && (answer[1] == ')' || answer[1] == '.' || answer[1] == ' '))
            {
                return answer[0].ToString();
            }

            // If it's just a single letter, return it
            if (answer.Length == 1 && char.IsLetter(answer[0]))
            {
                return answer;
            }

            // Otherwise return the full answer for exact matching
            return answer;
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
            public int QuestionNumber { get; set; }
            public string QuestionText { get; set; } = string.Empty;
            public string CorrectAnswer { get; set; } = string.Empty;
            public int Marks { get; set; }
        }

        /// <summary>
        /// DEBUG: Check what's stored in MCQ columns
        /// </summary>
        [HttpGet("debug/mcq-columns/{examId}/{studentId}")]
        public async Task<IActionResult> DebugMcqColumns(string examId, string studentId)
        {
            try
            {
                var submission = await _examRepository.GetWrittenSubmissionByExamAndStudentAsync(examId, studentId);
                if (submission == null)
                {
                    return NotFound(new { error = "Submission not found" });
                }

                return Ok(new
                {
                    submissionId = submission.WrittenSubmissionId,
                    examId = submission.ExamId,
                    studentId = submission.StudentId,
                    mcqAnswersJson = submission.McqAnswersJson,
                    mcqAnswersJsonLength = submission.McqAnswersJson?.Length ?? 0,
                    mcqScore = submission.McqScore,
                    mcqTotalMarks = submission.McqTotalMarks,
                    mcqAnswersList = submission.McqAnswers,
                    mcqAnswersListCount = submission.McqAnswers?.Count ?? 0,
                    filePathsJson = submission.FilePathsJson,
                    filePaths = submission.FilePaths,
                    filePathsCount = submission.FilePaths?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                return Ok(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
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
