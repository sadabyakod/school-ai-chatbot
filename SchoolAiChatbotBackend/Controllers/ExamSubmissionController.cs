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
    [ApiController]
    [Route("api/exam")]
    public class ExamSubmissionController : ControllerBase
    {
        private readonly IExamRepository _examRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IOcrService _ocrService;
        private readonly ISubjectiveEvaluator _subjectiveEvaluator;
        private readonly IMathOcrNormalizer _mathNormalizer;
        private readonly ISubjectiveRubricService _rubricService;
        private readonly IExamStorageService _examStorageService;
        private readonly IMcqExtractionService _mcqExtractionService;
        private readonly IMcqEvaluationService _mcqEvaluationService;
        private readonly ILogger<ExamSubmissionController> _logger;

        public ExamSubmissionController(
            IExamRepository examRepository,
            IFileStorageService fileStorageService,
            IOcrService ocrService,
            ISubjectiveEvaluator subjectiveEvaluator,
            IMathOcrNormalizer mathNormalizer,
            ISubjectiveRubricService rubricService,
            IExamStorageService examStorageService,
            IMcqExtractionService mcqExtractionService,
            IMcqEvaluationService mcqEvaluationService,
            ILogger<ExamSubmissionController> logger)
        {
            _examRepository = examRepository;
            _fileStorageService = fileStorageService;
            _ocrService = ocrService;
            _subjectiveEvaluator = subjectiveEvaluator;
            _mathNormalizer = mathNormalizer;
            _rubricService = rubricService;
            _examStorageService = examStorageService;
            _mcqExtractionService = mcqExtractionService;
            _mcqEvaluationService = mcqEvaluationService;
            _logger = logger;
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
        /// Upload written answers for subjective questions
        /// </summary>
        /// <param name="examId">Exam ID</param>
        /// <param name="studentId">Student ID</param>
        /// <param name="files">Scanned answer images or PDF</param>
        /// <returns>Submission ID and status</returns>
        [HttpPost("upload-written")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(UploadWrittenResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<UploadWrittenResponse>> UploadWrittenAnswers(
            [FromForm] string examId,
            [FromForm] string studentId,
            [FromForm] List<IFormFile> files)
        {
            try
            {
                _logger.LogInformation(
                    "Received written answers upload for exam {ExamId} from student {StudentId} with {FileCount} files",
                    examId,
                    studentId,
                    files?.Count ?? 0);

                // Validate input
                if (string.IsNullOrWhiteSpace(examId) || string.IsNullOrWhiteSpace(studentId))
                {
                    return BadRequest(new { error = "ExamId and StudentId are required" });
                }

                if (files == null || files.Count == 0)
                {
                    return BadRequest(new { error = "At least one file is required" });
                }

                // Validate exam exists in storage service
                if (!_examStorageService.ExamExists(examId))
                {
                    return NotFound(new { error = $"Exam {examId} not found. Please generate the exam first using /api/exam/generate" });
                }

                // Save files
                var filePaths = new List<string>();
                foreach (var file in files)
                {
                    var path = await _fileStorageService.SaveFileAsync(file, examId, studentId);
                    filePaths.Add(path);
                }

                // Create submission record
                var submission = new WrittenSubmission
                {
                    WrittenSubmissionId = Guid.NewGuid().ToString(),
                    ExamId = examId,
                    StudentId = studentId,
                    FilePaths = filePaths,
                    Status = SubmissionStatus.PendingEvaluation
                };

                await _examRepository.SaveWrittenSubmissionAsync(submission);

                // Trigger evaluation asynchronously (fire and forget)
                _ = Task.Run(async () => await ProcessWrittenSubmissionAsync(submission.WrittenSubmissionId));

                var response = new UploadWrittenResponse
                {
                    WrittenSubmissionId = submission.WrittenSubmissionId,
                    Status = "PendingEvaluation",
                    Message = "Written answers uploaded successfully. Evaluation in progress."
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading written answers");
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
                _logger.LogInformation("Fetching result for exam {ExamId}, student {StudentId}", examId, studentId);

                // Load exam from storage service
                var exam = _examStorageService.GetExam(examId);
                if (exam == null)
                {
                    return NotFound(new { error = $"Exam {examId} not found. Please generate the exam first using /api/exam/generate" });
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
                    return NotFound(new { error = "No submission found for this exam and student" });
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

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching exam result");
                return StatusCode(500, new { error = "Internal server error" });
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

        private async Task ProcessWrittenSubmissionAsync(string writtenSubmissionId)
        {
            try
            {
                _logger.LogInformation("Processing written submission {SubmissionId}", writtenSubmissionId);

                // Load submission
                var submission = await _examRepository.GetWrittenSubmissionAsync(writtenSubmissionId);
                if (submission == null)
                {
                    _logger.LogError("Submission {SubmissionId} not found", writtenSubmissionId);
                    return;
                }

                // Update status to OCR processing
                await _examRepository.UpdateWrittenSubmissionStatusAsync(
                    writtenSubmissionId,
                    SubmissionStatus.OcrProcessing);

                // Load exam from storage service
                var exam = _examStorageService.GetExam(submission.ExamId);
                if (exam == null)
                {
                    _logger.LogError("Exam {ExamId} not found in storage", submission.ExamId);
                    await _examRepository.UpdateWrittenSubmissionStatusAsync(
                        writtenSubmissionId,
                        SubmissionStatus.Failed);
                    return;
                }

                // STEP 1: Extract MCQ answers from uploaded images
                _logger.LogInformation("Extracting MCQ answers from submission {SubmissionId}", writtenSubmissionId);
                var mcqExtraction = await _mcqExtractionService.ExtractMcqAnswersAsync(submission);
                await _examRepository.SaveMcqExtractionAsync(mcqExtraction);

                // STEP 2: Evaluate extracted MCQ answers
                if (mcqExtraction.Status == ExtractionStatus.Completed && mcqExtraction.ExtractedAnswers.Count > 0)
                {
                    _logger.LogInformation(
                        "Evaluating {Count} extracted MCQ answers for submission {SubmissionId}",
                        mcqExtraction.ExtractedAnswers.Count,
                        writtenSubmissionId);

                    var mcqEvaluation = await _mcqEvaluationService.EvaluateExtractedAnswersAsync(mcqExtraction, exam);
                    await _examRepository.SaveMcqEvaluationFromSheetAsync(mcqEvaluation);

                    _logger.LogInformation(
                        "MCQ evaluation completed: Score {Score}/{Total}",
                        mcqEvaluation.TotalScore,
                        mcqEvaluation.TotalMarks);
                }
                else
                {
                    _logger.LogWarning(
                        "No MCQ answers extracted from submission {SubmissionId}, status: {Status}",
                        writtenSubmissionId,
                        mcqExtraction.Status);
                }

                // STEP 3: Extract text using OCR (for subjective answers)
                var ocrText = mcqExtraction.RawOcrText ?? string.Empty;
                if (string.IsNullOrWhiteSpace(ocrText))
                {
                    ocrText = await _ocrService.ExtractStudentAnswersTextAsync(submission);
                }
                await _examRepository.UpdateWrittenSubmissionOcrTextAsync(writtenSubmissionId, ocrText);

                // Update status to evaluating (subjective questions)
                await _examRepository.UpdateWrittenSubmissionStatusAsync(
                    writtenSubmissionId,
                    SubmissionStatus.Evaluating);

                // STEP 4: Evaluate subjective answers
                var evaluations = await _subjectiveEvaluator.EvaluateSubjectiveAnswersAsync(exam, ocrText);

                // Save evaluations
                await _examRepository.SaveSubjectiveEvaluationsAsync(writtenSubmissionId, evaluations);

                // Update status to completed
                await _examRepository.UpdateWrittenSubmissionStatusAsync(
                    writtenSubmissionId,
                    SubmissionStatus.Completed);

                _logger.LogInformation(
                    "Written submission {SubmissionId} processed successfully with {Count} subjective evaluations",
                    writtenSubmissionId,
                    evaluations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing written submission {SubmissionId}", writtenSubmissionId);
                
                try
                {
                    await _examRepository.UpdateWrittenSubmissionStatusAsync(
                        writtenSubmissionId,
                        SubmissionStatus.Failed);
                }
                catch { }
            }
        }

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
                return BadRequest(new { 
                    error = $"Sum of step marks ({stepMarksSum}) must equal total marks ({request.TotalMarks})" 
                });
            }

            _logger.LogInformation("Creating rubric for Exam={ExamId}, Question={QuestionId} with {StepCount} steps",
                request.ExamId, request.QuestionId, request.Steps.Count);

            await _rubricService.SaveRubricAsync(request);

            return Ok(new { 
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
                    return BadRequest(new { 
                        error = $"Rubric for question {rubric.QuestionId} must have at least one step" 
                    });
                }

                var stepMarksSum = rubric.Steps.Sum(s => s.Marks);
                if (stepMarksSum != rubric.TotalMarks)
                {
                    return BadRequest(new { 
                        error = $"Question {rubric.QuestionId}: Sum of step marks ({stepMarksSum}) must equal total marks ({rubric.TotalMarks})" 
                    });
                }
            }

            _logger.LogInformation("Batch creating {Count} rubrics for Exam={ExamId}",
                request.Rubrics.Count, request.ExamId);

            await _rubricService.SaveRubricsBatchAsync(request.ExamId, request.Rubrics);

            return Ok(new { 
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

            return Ok(new { 
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
