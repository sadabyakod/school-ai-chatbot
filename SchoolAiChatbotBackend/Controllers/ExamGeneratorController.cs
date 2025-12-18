using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using SchoolAiChatbotBackend.Services;
using SchoolAiChatbotBackend.DTOs;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;

namespace SchoolAiChatbotBackend.Controllers
{
    [ApiController]
    [Route("api/exam")]
    [Produces("application/json")]
    [Tags("AI Exam Generator")]
    public class ExamGeneratorController : ControllerBase
    {
        private readonly IOpenAIService _openAIService;
        private readonly ISubjectiveRubricService _rubricService;
        private readonly IExamStorageService _examStorageService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ExamGeneratorController> _logger;
        
        // Track generation progress for streaming
        private static readonly ConcurrentDictionary<string, ExamGenerationProgress> _generationProgress = new();
        
        // Cache expiration (24 hours)
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24);

        public ExamGeneratorController(
            IOpenAIService openAIService,
            ISubjectiveRubricService rubricService,
            IExamStorageService examStorageService,
            IMemoryCache cache,
            ILogger<ExamGeneratorController> logger)
        {
            _openAIService = openAIService;
            _rubricService = rubricService;
            _examStorageService = examStorageService;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Generate a Karnataka 2nd PUC style exam paper using AI
        /// POST /api/exam/generate or /api/exam-generator/generate-exam
        /// </summary>
        /// <remarks>
        /// Generates ORIGINAL MODEL QUESTION PAPERS following Karnataka State Government 2nd PUC Mathematics exam style.
        /// Returns a mix of MCQ, fill-in-the-blank, short answer, and long answer questions.
        /// Follows the official Karnataka 2nd PUC model paper format with 1-mark to 5-mark questions.
        /// </remarks>
        [HttpPost("generate")]
        [HttpPost("/api/exam-generator/generate-exam")] // Alternate route for mobile app
        public async Task<IActionResult> GenerateExam([FromBody] GenerateExamRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // === REQUEST LOGGING ===
            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine("📥 EXAM GENERATE - REQUEST RECEIVED");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine($"⏰ Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"📚 Subject: {request.Subject}");
            Console.WriteLine($"🎓 Grade: {request.Grade}");
            Console.WriteLine($"🔄 Use Cache: {request.UseCache} | Fast Mode: {request.FastMode}");
            Console.WriteLine(new string('-', 80));

            _logger.LogInformation(
                "Generating Karnataka 2nd PUC exam: Subject={Subject}, Grade={Grade}, UseCache={UseCache}, FastMode={FastMode}",
                request.Subject, request.Grade, request.UseCache, request.FastMode);

            try
            {
                // Check cache first if caching is enabled
                var chapter = request.Chapter ?? "all";
                var difficulty = request.Difficulty ?? "medium";
                var examType = request.ExamType ?? "full";
                var cacheKey = $"exam_{request.Subject}_{request.Grade}_{chapter}_{difficulty}_{examType}".ToLowerInvariant().Replace(" ", "_");
                
                if (request.UseCache && _cache.TryGetValue(cacheKey, out GeneratedExamResponse? cachedExam) && cachedExam != null)
                {
                    Console.WriteLine($"✅ CACHE HIT - Returning cached exam: {cachedExam.ExamId}");
                    _logger.LogInformation("Cache hit for {CacheKey}, returning cached exam {ExamId}", cacheKey, cachedExam.ExamId);
                    
                    // Clone and update the exam ID to make it unique for this request
                    var clonedExam = CloneExam(cachedExam);
                    clonedExam.ExamId = $"{cachedExam.ExamId}_cached_{DateTime.UtcNow:HHmmss}";
                    clonedExam.CreatedAt = DateTime.UtcNow.ToString("o");
                    
                    // Store the cloned exam
                    _examStorageService.StoreExam(clonedExam);
                    
                    // Return exam WITHOUT model answers (students should not see answers upfront)
                    var publicExam = StripModelAnswers(clonedExam);
                    return Ok(publicExam);
                }
                
                var startTime = DateTime.UtcNow;
                
                // === AI GENERATION: Generate exam using OpenAI ===
                var prompt = BuildExamGenerationPrompt(request);
                var aiResponse = await _openAIService.GetExamGenerationAsync(prompt, request.FastMode);
                var examPaper = ParseExamResponse(aiResponse, request);

                // Generate and save rubrics for all subjective questions
                await GenerateAndSaveRubricsAsync(examPaper);

                // Store the exam for later retrieval (for MCQ submission, written answer upload, etc.)
                _examStorageService.StoreExam(examPaper);
                _logger.LogInformation("Exam {ExamId} stored for later retrieval", examPaper.ExamId);

                _logger.LogInformation(
                    "Exam generated successfully: ExamId={ExamId}, Questions={QuestionCount}, TotalMarks={TotalMarks}",
                    examPaper.ExamId, examPaper.Questions?.Count ?? 0, examPaper.TotalMarks);

                // === RESPONSE LOGGING ===
                Console.WriteLine("📤 EXAM GENERATE - RESPONSE");
                Console.WriteLine(new string('-', 80));
                Console.WriteLine($"✅ Exam ID: {examPaper.ExamId}");
                Console.WriteLine($"📚 Subject: {examPaper.Subject}");
                Console.WriteLine($"🎓 Grade: {examPaper.Grade}");
                Console.WriteLine($"📝 Total Questions: {examPaper.QuestionCount}");
                Console.WriteLine($"📊 Total Marks: {examPaper.TotalMarks}");
                Console.WriteLine($"⏱️ Duration: {examPaper.Duration} mins");
                if (examPaper.Parts != null)
                {
                    Console.WriteLine($"📋 Parts:");
                    foreach (var part in examPaper.Parts)
                        Console.WriteLine($"   - {part.PartName}: {part.TotalQuestions} questions ({part.MarksPerQuestion} marks each)");
                }
                Console.WriteLine(new string('=', 80) + "\n");

                // Cache the exam for future requests
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(CacheExpiration)
                    .SetSlidingExpiration(TimeSpan.FromHours(6));
                _cache.Set(cacheKey, examPaper, cacheOptions);
                _logger.LogInformation("Exam cached with key {CacheKey}", cacheKey);
                
                var generationTime = (DateTime.UtcNow - startTime).TotalSeconds;
                Console.WriteLine($"⏱️ Total Generation Time: {generationTime:F1}s");

                // Return exam WITHOUT model answers (students should not see answers upfront)
                var strippedExam = StripModelAnswers(examPaper);
                return Ok(strippedExam);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse AI response as JSON");
                return StatusCode(500, new
                {
                    status = "error",
                    message = "Failed to generate valid exam format.",
                    details = "AI response was not valid JSON."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating exam");
                return StatusCode(500, new
                {
                    status = "error",
                    message = "Failed to generate exam.",
                    details = ex.Message
                });
            }
        }
        
        /// <summary>
        /// Clone an exam for cache return
        /// </summary>
        private GeneratedExamResponse CloneExam(GeneratedExamResponse original)
        {
            var json = JsonSerializer.Serialize(original);
            return JsonSerializer.Deserialize<GeneratedExamResponse>(json) ?? original;
        }
        
        /// <summary>
        /// Start async exam generation with progress tracking
        /// POST /api/exam/generate-async
        /// </summary>
        /// <remarks>
        /// Starts exam generation in background and returns a request ID.
        /// Use GET /api/exam/generate-progress/{requestId} to track progress.
        /// </remarks>
        [HttpPost("generate-async")]
        public IActionResult GenerateExamAsync([FromBody] GenerateExamRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
                
            var requestId = Guid.NewGuid().ToString("N")[..12];
            
            // Check cache first
            var chapter = request.Chapter ?? "all";
            var difficulty = request.Difficulty ?? "medium";
            var examType = request.ExamType ?? "full";
            var cacheKey = $"exam_{request.Subject}_{request.Grade}_{chapter}_{difficulty}_{examType}".ToLowerInvariant().Replace(" ", "_");
            
            if (request.UseCache && _cache.TryGetValue(cacheKey, out GeneratedExamResponse? cachedExam) && cachedExam != null)
            {
                var clonedExam = CloneExam(cachedExam);
                clonedExam.ExamId = $"{cachedExam.ExamId}_cached_{DateTime.UtcNow:HHmmss}";
                _examStorageService.StoreExam(clonedExam);
                
                return Ok(new {
                    requestId = requestId,
                    status = "complete",
                    cached = true,
                    exam = clonedExam
                });
            }
            
            // Initialize progress tracking
            var progress = new ExamGenerationProgress
            {
                RequestId = requestId,
                Status = "generating",
                ProgressPercent = 10,
                Message = "Starting AI exam generation..."
            };
            _generationProgress[requestId] = progress;
            
            // Start background generation
            _ = Task.Run(async () =>
            {
                try
                {
                    progress.Status = "generating";
                    progress.ProgressPercent = 20;
                    progress.Message = $"Generating {request.Subject} exam using AI (Fast Mode: {request.FastMode})...";
                    
                    var prompt = BuildExamGenerationPrompt(request);
                    var aiResponse = await _openAIService.GetExamGenerationAsync(prompt, request.FastMode);
                    
                    progress.ProgressPercent = 60;
                    progress.Status = "parsing";
                    progress.Message = "Parsing exam structure...";
                    
                    var examPaper = ParseExamResponse(aiResponse, request);
                    
                    progress.ProgressPercent = 80;
                    progress.Status = "rubrics";
                    progress.Message = "Generating marking rubrics...";
                    
                    await GenerateAndSaveRubricsAsync(examPaper);
                    _examStorageService.StoreExam(examPaper);
                    
                    // Cache the exam
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(CacheExpiration)
                        .SetSlidingExpiration(TimeSpan.FromHours(6));
                    _cache.Set(cacheKey, examPaper, cacheOptions);
                    
                    progress.ProgressPercent = 100;
                    progress.Status = "complete";
                    progress.Message = "Exam generation complete!";
                    progress.Result = examPaper;
                }
                catch (Exception ex)
                {
                    progress.Status = "error";
                    progress.Error = ex.Message;
                    progress.Message = "Failed to generate exam";
                    _logger.LogError(ex, "Async exam generation failed for request {RequestId}", requestId);
                }
            });
            
            return Accepted(new {
                requestId = requestId,
                status = "generating",
                progressUrl = $"/api/exam/generate-progress/{requestId}",
                message = "Exam generation started. Poll progress URL for status.",
                estimatedTime = request.FastMode ? "15-30 seconds" : "60-90 seconds"
            });
        }
        
        /// <summary>
        /// Get exam generation progress
        /// GET /api/exam/generate-progress/{requestId}
        /// </summary>
        [HttpGet("generate-progress/{requestId}")]
        public IActionResult GetGenerationProgress(string requestId)
        {
            if (!_generationProgress.TryGetValue(requestId, out var progress))
            {
                return NotFound(new { error = "Request not found", requestId });
            }
            
            var elapsedSeconds = (DateTime.UtcNow - progress.StartedAt).TotalSeconds;
            
            var response = new
            {
                requestId = progress.RequestId,
                status = progress.Status,
                progressPercent = progress.ProgressPercent,
                message = progress.Message,
                elapsedSeconds = Math.Round(elapsedSeconds, 1),
                exam = progress.Result,
                error = progress.Error
            };
            
            // Cleanup completed/errored requests after returning
            if (progress.Status == "complete" || progress.Status == "error")
            {
                _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(_ => 
                {
                    _generationProgress.TryRemove(requestId, out ExamGenerationProgress? _);
                });
            }
            
            return Ok(response);
        }
        
        /// <summary>
        /// Clear exam cache (for testing)
        /// DELETE /api/exam/cache
        /// </summary>
        [HttpDelete("cache")]
        public IActionResult ClearCache([FromQuery] string? subject = null, [FromQuery] string? grade = null)
        {
            if (!string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(grade))
            {
                var cacheKey = $"exam_{subject}_{grade}".ToLowerInvariant().Replace(" ", "_");
                _cache.Remove(cacheKey);
                return Ok(new { message = $"Cache cleared for {subject} - {grade}", cacheKey });
            }
            
            // Note: IMemoryCache doesn't have a clear all method, so we just return info
            return Ok(new { message = "Specific cache entries must be cleared by subject/grade" });
        }

        /// <summary>
        /// Submit and evaluate exam answers (text or image)
        /// POST /api/exam/submit
        /// </summary>
        /// <remarks>
        /// Students can submit their answers as text or upload images of handwritten answers.
        /// Each answer is evaluated by AI and scored against the correct answer.
        /// Returns individual scores per question and total score.
        /// </remarks>
        [HttpPost("submit")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubmitExamAnswers([FromForm] SubmitExamRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _logger.LogInformation(
                "Evaluating exam submission: ExamId={ExamId}, AnswerCount={AnswerCount}",
                request.ExamId, request.Answers?.Count ?? 0);

            try
            {
                var results = new List<AnswerEvaluationResult>();
                var totalScore = 0;
                var totalMaxScore = 0;

                if (request.Answers == null || request.Answers.Count == 0)
                {
                    return BadRequest(new { status = "error", message = "No answers provided" });
                }

                foreach (var answer in request.Answers)
                {
                    try
                    {
                        string evaluationJson;

                        // Check if image is provided
                        if (answer.ImageFile != null && answer.ImageFile.Length > 0)
                        {
                            // Read image data
                            using var memoryStream = new MemoryStream();
                            await answer.ImageFile.CopyToAsync(memoryStream);
                            var imageData = memoryStream.ToArray();
                            var mimeType = answer.ImageFile.ContentType ?? "image/jpeg";

                            _logger.LogInformation(
                                "Evaluating image answer for question {QuestionId}, ImageSize={Size} bytes",
                                answer.QuestionId, imageData.Length);

                            evaluationJson = await _openAIService.EvaluateAnswerFromImageAsync(
                                answer.QuestionText,
                                answer.CorrectAnswer,
                                imageData,
                                mimeType,
                                answer.MaxMarks);
                        }
                        else if (!string.IsNullOrWhiteSpace(answer.TextAnswer))
                        {
                            // Evaluate text answer
                            _logger.LogInformation(
                                "Evaluating text answer for question {QuestionId}",
                                answer.QuestionId);

                            evaluationJson = await _openAIService.EvaluateAnswerAsync(
                                answer.QuestionText,
                                answer.CorrectAnswer,
                                answer.TextAnswer,
                                answer.MaxMarks);
                        }
                        else
                        {
                            // No answer provided - score 0
                            results.Add(new AnswerEvaluationResult
                            {
                                QuestionId = answer.QuestionId,
                                QuestionNumber = answer.QuestionNumber,
                                Score = 0,
                                MaxMarks = answer.MaxMarks,
                                Feedback = "No answer provided",
                                IsCorrect = false,
                                ExtractedText = null
                            });
                            totalMaxScore += answer.MaxMarks;
                            continue;
                        }

                        // Parse the evaluation response
                        var evalResult = JsonSerializer.Deserialize<EvaluationJsonResponse>(
                            evaluationJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        var score = Math.Min(evalResult?.Score ?? 0, answer.MaxMarks);
                        totalScore += score;
                        totalMaxScore += answer.MaxMarks;

                        results.Add(new AnswerEvaluationResult
                        {
                            QuestionId = answer.QuestionId,
                            QuestionNumber = answer.QuestionNumber,
                            Score = score,
                            MaxMarks = answer.MaxMarks,
                            Feedback = evalResult?.Feedback ?? "Evaluation completed",
                            IsCorrect = evalResult?.IsCorrect ?? false,
                            ExtractedText = evalResult?.ExtractedText
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error evaluating answer for question {QuestionId}", answer.QuestionId);

                        // Add error result but continue with other answers
                        results.Add(new AnswerEvaluationResult
                        {
                            QuestionId = answer.QuestionId,
                            QuestionNumber = answer.QuestionNumber,
                            Score = 0,
                            MaxMarks = answer.MaxMarks,
                            Feedback = "Error evaluating answer. Please try again.",
                            IsCorrect = false,
                            ExtractedText = null
                        });
                        totalMaxScore += answer.MaxMarks;
                    }
                }

                // Calculate percentage
                var percentage = totalMaxScore > 0 ? Math.Round((double)totalScore / totalMaxScore * 100, 1) : 0;

                // Determine grade
                var grade = percentage switch
                {
                    >= 90 => "A+",
                    >= 80 => "A",
                    >= 70 => "B+",
                    >= 60 => "B",
                    >= 50 => "C",
                    >= 35 => "D",
                    _ => "F"
                };

                var response = new ExamSubmissionResponse
                {
                    ExamId = request.ExamId,
                    TotalScore = totalScore,
                    TotalMaxScore = totalMaxScore,
                    Percentage = percentage,
                    Grade = grade,
                    QuestionsAnswered = results.Count(r => r.Score > 0 || !string.IsNullOrEmpty(r.ExtractedText)),
                    TotalQuestions = results.Count,
                    Results = results,
                    EvaluatedAt = DateTime.UtcNow.ToString("o")
                };

                _logger.LogInformation(
                    "Exam evaluated: ExamId={ExamId}, Score={Score}/{MaxScore}, Grade={Grade}",
                    request.ExamId, totalScore, totalMaxScore, grade);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing exam submission");
                return StatusCode(500, new
                {
                    status = "error",
                    message = "Failed to evaluate exam.",
                    details = ex.Message
                });
            }
        }

        private string BuildExamGenerationPrompt(GenerateExamRequest request)
        {
            return $@"You are an exam paper generator for a school mobile app.

Your job is to generate ORIGINAL model question papers that follow the EXACT format and style of
Karnataka State Government 2nd PUC Mathematics board exams (Model Question Paper 2024-25).

Generate an exam with the following specifications:
- Subject: {request.Subject}
- Grade: {request.Grade}
- Syllabus Scope: Full Syllabus (cover all chapters)
- Overall Difficulty: Medium
- Exam Type: Full Paper

You MUST return exactly one VALID JSON object following this schema:

{{
  ""examId"": ""string"",
  ""subject"": ""{request.Subject}"",
  ""grade"": ""{request.Grade}"",
  ""chapter"": ""Full Syllabus"",
  ""difficulty"": ""Medium"",
  ""examType"": ""Full Paper"",
  ""totalMarks"": 80,
  ""duration"": 195,
  ""instructions"": [
    ""Answer ALL 20 questions in Part A (15 MCQ + 5 Fill-in-the-Blanks)"",
    ""Answer any SIX questions from Part B (out of 11)"",
    ""Answer any SIX questions from Part C (out of 11)"",
    ""Answer any FOUR questions from Part D (out of 8)"",
    ""Answer any ONE question from Part E""
  ],
  ""parts"": [
    {{
      ""partName"": ""Part A"",
      ""partDescription"": ""Answer ALL the following questions (15 MCQ + 5 Fill-in-the-Blanks)"",
      ""questionType"": ""MCQ / Fill-in-the-Blanks"",
      ""marksPerQuestion"": 1,
      ""totalQuestions"": 20,
      ""questionsToAnswer"": 20,
      ""questions"": [
        {{
          ""questionId"": ""A1"",
          ""questionNumber"": 1,
          ""questionText"": ""string"",
          ""options"": [""A) option1"", ""B) option2"", ""C) option3"", ""D) option4""],
          ""correctAnswer"": ""A) option1"",
          ""topic"": ""string""
        }}
      ]
    }},
    {{
      ""partName"": ""Part B"",
      ""partDescription"": ""Answer any SIX of the following questions"",
      ""questionType"": ""Short Answer (2 marks)"",
      ""marksPerQuestion"": 2,
      ""totalQuestions"": 11,
      ""questionsToAnswer"": 6,
      ""questions"": [
        {{
          ""questionId"": ""B1"",
          ""questionNumber"": 21,
          ""questionText"": ""string"",
          ""options"": [],
          ""correctAnswer"": ""Model answer text"",
          ""topic"": ""string""
        }}
      ]
    }},
    {{
      ""partName"": ""Part C"",
      ""partDescription"": ""Answer any SIX of the following questions"",
      ""questionType"": ""Short Answer (3 marks)"",
      ""marksPerQuestion"": 3,
      ""totalQuestions"": 11,
      ""questionsToAnswer"": 6,
      ""questions"": [
        {{
          ""questionId"": ""C1"",
          ""questionNumber"": 32,
          ""questionText"": ""string"",
          ""options"": [],
          ""correctAnswer"": ""Model answer text"",
          ""topic"": ""string""
        }}
      ]
    }},
    {{
      ""partName"": ""Part D"",
      ""partDescription"": ""Answer any FOUR of the following questions"",
      ""questionType"": ""Long Answer (5 marks)"",
      ""marksPerQuestion"": 5,
      ""totalQuestions"": 8,
      ""questionsToAnswer"": 4,
      ""questions"": [
        {{
          ""questionId"": ""D1"",
          ""questionNumber"": 43,
          ""questionText"": ""string"",
          ""options"": [],
          ""correctAnswer"": ""Model answer with steps"",
          ""topic"": ""string""
        }}
      ]
    }},
    {{
      ""partName"": ""Part E"",
      ""partDescription"": ""Answer any ONE of the following questions"",
      ""questionType"": ""Long Answer (10 marks)"",
      ""marksPerQuestion"": 10,
      ""totalQuestions"": 2,
      ""questionsToAnswer"": 1,
      ""questions"": [
        {{
          ""questionId"": ""E1"",
          ""questionNumber"": 51,
          ""questionText"": ""string"",
          ""subParts"": [
            {{
              ""partLabel"": ""a"",
              ""questionText"": ""sub-question a (6 marks)"",
              ""correctAnswer"": ""Model answer for part a""
            }},
            {{
              ""partLabel"": ""b"",
              ""questionText"": ""sub-question b (4 marks)"",
              ""correctAnswer"": ""Model answer for part b""
            }}
          ],
          ""options"": [],
          ""correctAnswer"": ""Combined model answer"",
          ""topic"": ""string""
        }}
      ]
    }}
  ],
  ""questionCount"": 52,
  ""createdAt"": ""ISO 8601 string""
}}

--------------------
KARNATAKA 2nd PUC MATHEMATICS MODEL PAPER FORMAT (EXACT):
--------------------

TOTAL MARKS: 80 marks (Theory Paper)
INTERNAL ASSESSMENT: 20 marks
TIME: 3 hours 15 minutes (195 minutes)

STRUCTURE:
1) PART A - MCQ & Fill-in-the-Blanks:
   - 20 questions × 1 mark each = 20 marks (Compulsory)
   - 15 Multiple Choice Questions (MCQ) with 4 options (A, B, C, D)
   - 5 Fill-in-the-Blanks questions
   - Topics: Relations & Functions, Inverse Trigonometry, Matrices, Determinants, Continuity, Differentiability, Integrals, Differential Equations, Vectors, 3D Geometry, Linear Programming, Probability

2) PART B - Short Answer Type (2 marks each):
   - 11 questions given, answer ANY 6 = 12 marks
   - Questions 21-31
   - Direct calculation / short proof type
   - Topics from all chapters

3) PART C - Short Answer Type (3 marks each):
   - 11 questions given, answer ANY 6 = 18 marks  
   - Questions 32-42
   - Application-based problems, proofs, derivations
   - Topics from all chapters

4) PART D - Long Answer Type (5 marks each):
   - 8 questions given, answer ANY 4 = 20 marks
   - Questions 43-50
   - Detailed problem solving, proofs with steps
   - Topics: Matrices (inverse), Calculus (maxima/minima), Integration, Differential Equations, Vectors/3D Geometry, Linear Programming

5) PART E - Long Answer Type (10 marks each):
   - 2 questions given, answer ANY 1 = 10 marks
   - Questions 51-52
   - Each question has 2 sub-parts: (a) 6 marks + (b) 4 marks
   - OR choice between 2 full questions
   - Topics: Integration with graphs, Area under curves, 3D Geometry, Linear Programming

TOTAL = 20 + 12 + 18 + 20 + 10 = 80 marks (Theory Paper)

NOTE: Marks breakdown:
- Part A: 20 marks (15 MCQ + 5 Fill-in-blanks, ALL compulsory)
- Part B: 12 marks (answer 6 out of 11 questions × 2 marks)
- Part C: 18 marks (answer 6 out of 11 questions × 3 marks)
- Part D: 20 marks (answer 4 out of 8 questions × 5 marks)
- Part E: 10 marks (answer 1 out of 2 questions: (a) 6 marks + (b) 4 marks)
- Internal Assessment: 20 marks (awarded separately)
- TOTAL: 100 marks (80 Theory + 20 Internal)
- PASSING: Minimum 35 marks out of 100 (35%)

--------------------
IMPORTANT RULES:
--------------------

1) GENERATE EXACTLY THIS STRUCTURE:
   - Part A: Generate exactly 20 questions (15 MCQ + 5 Fill-in-blanks, 1 mark each)
   - Part B: Generate exactly 11 questions (2 marks each), student answers 6
   - Part C: Generate exactly 11 questions (3 marks each), student answers 6
   - Part D: Generate exactly 8 questions (5 marks each), student answers 4
   - Part E: Generate exactly 2 questions (10 marks each with 2 sub-parts), student answers 1

2) QUESTION NUMBERING:
   - Part A: Questions 1-20 (1-15 MCQ, 16-20 Fill-in-blanks)
   - Part B: Questions 21-31
   - Part C: Questions 32-42
   - Part D: Questions 43-50
   - Part E: Questions 51-52

3) PART A FORMAT:
   - Questions 1-15 (MCQ): options MUST contain EXACTLY 4 choices formatted as: [""A) ..."", ""B) ..."", ""C) ..."", ""D) ...""]
     correctAnswer MUST be the full option text like ""A) answer""
   - Questions 16-20 (Fill-in-blanks): options MUST be empty array [], correctAnswer contains the blank answer

4) NON-MCQ FORMAT (Parts B, C, D, E):
   - options MUST be an empty array []
   - correctAnswer MUST contain a DETAILED model answer/solution with ALL steps
   - Model answers MUST be proportional to marks:
     * 2 marks: Brief answer with 2-3 key steps/points
     * 3 marks: Clear explanation with 3-4 detailed steps/points
     * 4 marks: Detailed solution with 4-5 steps, formulas, and explanations
     * 5 marks: Comprehensive solution with 5-7 detailed steps, formulas, and reasoning
     * 6 marks: Thorough solution with 6-8 detailed steps, complete working
     * 10 marks: Complete answer with multiple sub-sections, all steps shown, complete derivations
   - Include ALL intermediate calculations, formulas, and explanations
   - For mathematical problems: Show step-by-step work with clear labeling
   - For theory questions: Provide complete definitions, explanations, and examples

5) PART E SUB-PARTS:
   - Each Part E question MUST have a ""subParts"" array with 2 sub-questions
   - Sub-part (a) is worth 6 marks
   - Sub-part (b) is worth 4 marks
   - Total per question: 10 marks

6) TOPICS TO COVER (Karnataka 2nd PUC Syllabus):
   - Relations and Functions
   - Inverse Trigonometric Functions
   - Matrices and Determinants
   - Continuity and Differentiability
   - Applications of Derivatives
   - Integrals (Indefinite and Definite)
   - Applications of Integrals
   - Differential Equations
   - Vector Algebra
   - Three Dimensional Geometry
   - Linear Programming
   - Probability

7) SYLLABUS COVERAGE:
   - Cover ALL topics from the full syllabus
   - Distribute questions evenly across all chapters
   - Ensure comprehensive coverage of the entire curriculum

8) JSON-ONLY OUTPUT:
   - Output MUST be valid JSON only
   - No comments, no markdown, no extra text, no trailing commas

Generate the complete Karnataka 2nd PUC Mathematics Model Question Paper now:";
        }

        /// <summary>
        /// Generate a simple test exam with 2 MCQ and 3 subjective questions for testing purposes
        /// Includes scenarios: correct answer, wrong answer, not attempted
        /// </summary>
        private GeneratedExamResponse GenerateSimpleTestExam(GenerateExamRequest request)
        {
            var examId = $"TEST-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            
            return new GeneratedExamResponse
            {
                ExamId = examId,
                Subject = request.Subject,
                Grade = request.Grade,
                Chapter = "Test Chapter",
                Difficulty = "Medium",
                ExamType = "Test",
                TotalMarks = 32,
                Duration = 45,
                Instructions = new List<string>
                {
                    "Answer ALL questions",
                    "This is a test exam with 2 MCQs and 3 subjective questions"
                },
                Parts = new List<ExamPart>
                {
                    new ExamPart
                    {
                        PartName = "Part A",
                        PartDescription = "Multiple Choice Questions",
                        QuestionType = "MCQ",
                        MarksPerQuestion = 1,
                        TotalQuestions = 2,
                        QuestionsToAnswer = 2,
                        Questions = new List<PartQuestion>
                        {
                            new PartQuestion
                            {
                                QuestionId = "A1",
                                QuestionNumber = 1,
                                QuestionText = "What is 2 + 2?",
                                Options = new List<string>
                                {
                                    "A) 3",
                                    "B) 4",
                                    "C) 5",
                                    "D) 6"
                                },
                                CorrectAnswer = "B) 4",
                                Topic = "Basic Arithmetic"
                            },
                            new PartQuestion
                            {
                                QuestionId = "A2",
                                QuestionNumber = 2,
                                QuestionText = "What is the value of π (pi) approximately?",
                                Options = new List<string>
                                {
                                    "A) 3.14",
                                    "B) 2.71",
                                    "C) 1.41",
                                    "D) 4.20"
                                },
                                CorrectAnswer = "A) 3.14",
                                Topic = "Mathematics Constants"
                            }
                        }
                    },
                    new ExamPart
                    {
                        PartName = "Part B",
                        PartDescription = "Subjective Questions",
                        QuestionType = "Short Answer (10 marks)",
                        MarksPerQuestion = 10,
                        TotalQuestions = 3,
                        QuestionsToAnswer = 3,
                        Questions = new List<PartQuestion>
                        {
                            new PartQuestion
                            {
                                QuestionId = "B1",
                                QuestionNumber = 3,
                                QuestionText = "Explain the Pythagorean theorem and provide an example calculation for a right triangle with sides 3 and 4.",
                                Options = new List<string>(),
                                CorrectAnswer = @"The Pythagorean theorem states that in a right triangle, the square of the hypotenuse (c) equals the sum of squares of the other two sides (a and b): a² + b² = c²

Example with sides 3 and 4:
Step 1: Apply the formula: 3² + 4² = c²
Step 2: Calculate: 9 + 16 = c²
Step 3: Simplify: 25 = c²
Step 4: Solve for c: c = √25 = 5

Therefore, the hypotenuse is 5 units.",
                                Topic = "Geometry - Pythagorean Theorem"
                            },
                            new PartQuestion
                            {
                                QuestionId = "B2",
                                QuestionNumber = 4,
                                QuestionText = "Solve the quadratic equation: x² - 5x + 6 = 0. Show all steps.",
                                Options = new List<string>(),
                                CorrectAnswer = @"To solve x² - 5x + 6 = 0, we can factor or use the quadratic formula.

Method 1: Factoring
Step 1: Find two numbers that multiply to 6 and add to -5: -2 and -3
Step 2: Factor: (x - 2)(x - 3) = 0
Step 3: Solve: x - 2 = 0 or x - 3 = 0
Step 4: Solutions: x = 2 or x = 3

Method 2: Quadratic Formula
x = (-b ± √(b² - 4ac)) / 2a where a=1, b=-5, c=6
x = (5 ± √(25 - 24)) / 2
x = (5 ± 1) / 2
x = 3 or x = 2

Therefore, x = 2 or x = 3",
                                Topic = "Algebra - Quadratic Equations"
                            },
                            new PartQuestion
                            {
                                QuestionId = "B3",
                                QuestionNumber = 5,
                                QuestionText = "Calculate the area of a circle with radius 5 cm. Use π = 3.14. Show your work.",
                                Options = new List<string>(),
                                CorrectAnswer = @"To find the area of a circle, use the formula A = πr²

Given:
- Radius (r) = 5 cm
- π = 3.14

Step 1: Identify the formula: A = πr²
Step 2: Substitute values: A = 3.14 × 5²
Step 3: Calculate 5²: A = 3.14 × 25
Step 4: Multiply: A = 78.5

Therefore, the area is 78.5 cm²",
                                Topic = "Geometry - Circle Area"
                            }
                        }
                    }
                },
                Questions = new List<GeneratedQuestion>(),
                QuestionCount = 5,
                CreatedAt = DateTime.UtcNow.ToString("o")
            };
        }

        private GeneratedExamResponse ParseExamResponse(string aiResponse, GenerateExamRequest request)
        {
            // Clean the response - remove any markdown or extra text
            var cleanedResponse = aiResponse.Trim();

            // Remove markdown code blocks if present
            if (cleanedResponse.StartsWith("```json"))
                cleanedResponse = cleanedResponse.Substring(7);
            else if (cleanedResponse.StartsWith("```"))
                cleanedResponse = cleanedResponse.Substring(3);

            if (cleanedResponse.EndsWith("```"))
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);

            cleanedResponse = cleanedResponse.Trim();

            // Find the JSON object
            var startIndex = cleanedResponse.IndexOf('{');
            var endIndex = cleanedResponse.LastIndexOf('}');

            if (startIndex >= 0 && endIndex > startIndex)
            {
                cleanedResponse = cleanedResponse.Substring(startIndex, endIndex - startIndex + 1);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var exam = JsonSerializer.Deserialize<GeneratedExamResponse>(cleanedResponse, options);

            if (exam == null)
                throw new JsonException("Failed to deserialize exam response");

            // Ensure required fields match request
            exam.Subject = request.Subject;
            exam.Grade = request.Grade;
            exam.Chapter = "Full Syllabus";
            exam.Difficulty = "Medium";
            exam.ExamType = "Full Paper";

            // Generate examId if not present
            if (string.IsNullOrEmpty(exam.ExamId))
                exam.ExamId = $"KAR-2PUC-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            // Set createdAt if not present
            if (string.IsNullOrEmpty(exam.CreatedAt))
                exam.CreatedAt = DateTime.UtcNow.ToString("o");

            // Calculate totalMarks from parts if available
            if (exam.Parts != null && exam.Parts.Count > 0)
            {
                var calculatedTotal = 0;
                var totalQuestions = 0;
                foreach (var part in exam.Parts)
                {
                    calculatedTotal += part.MarksPerQuestion * part.QuestionsToAnswer;
                    totalQuestions += part.Questions?.Count ?? 0;
                }
                exam.TotalMarks = calculatedTotal;
                exam.QuestionCount = totalQuestions;
            }
            // Fallback to old format if parts not present
            else if (exam.Questions != null && exam.Questions.Count > 0)
            {
                var calculatedTotal = 0;
                foreach (var q in exam.Questions)
                {
                    calculatedTotal += q.Marks;
                }
                exam.TotalMarks = calculatedTotal;
                exam.QuestionCount = exam.Questions.Count;
            }

            return exam;
        }

        /// <summary>
        /// Generate and save rubrics for all subjective questions in the exam.
        /// This ensures consistent, auditable marking for all subjective questions.
        /// </summary>
        private async Task GenerateAndSaveRubricsAsync(GeneratedExamResponse exam)
        {
            if (exam?.Parts == null || exam.Parts.Count == 0)
            {
                _logger.LogInformation("No parts found in exam {ExamId}, skipping rubric generation", exam?.ExamId);
                return;
            }

            var rubrics = new List<QuestionRubricDto>();
            var subjectivePartsProcessed = 0;

            foreach (var part in exam.Parts)
            {
                // Skip MCQ parts (Part A typically)
                if (part.QuestionType.Contains("MCQ", StringComparison.OrdinalIgnoreCase) ||
                    part.QuestionType.Contains("Multiple Choice", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Skipping MCQ part {PartName} for rubric generation", part.PartName);
                    continue;
                }

                subjectivePartsProcessed++;
                var marksPerQuestion = part.MarksPerQuestion;

                foreach (var question in part.Questions)
                {
                    try
                    {
                        // Generate default rubric steps for this question
                        var steps = await _rubricService.GenerateDefaultRubricAsync(
                            question.QuestionText,
                            question.CorrectAnswer,
                            marksPerQuestion);

                        rubrics.Add(new QuestionRubricDto
                        {
                            QuestionId = question.QuestionId,
                            TotalMarks = marksPerQuestion,
                            Steps = steps.Select(s => new StepRubricItemDto
                            {
                                StepNumber = s.StepNumber,
                                Description = s.Description,
                                Marks = s.Marks
                            }).ToList(),
                            QuestionText = question.QuestionText,
                            ModelAnswer = question.CorrectAnswer
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate rubric for question {QuestionId} in exam {ExamId}",
                            question.QuestionId, exam.ExamId);
                        // Continue with other questions even if one fails
                    }
                }
            }

            if (rubrics.Count > 0)
            {
                try
                {
                    await _rubricService.SaveRubricsBatchAsync(exam.ExamId, rubrics);
                    _logger.LogInformation(
                        "Generated and saved {RubricCount} rubrics for exam {ExamId} ({PartsProcessed} subjective parts)",
                        rubrics.Count, exam.ExamId, subjectivePartsProcessed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save rubrics for exam {ExamId}", exam.ExamId);
                    // Don't throw - exam generation should still succeed even if rubric save fails
                }
            }
            else
            {
                _logger.LogInformation("No subjective questions found in exam {ExamId}, no rubrics generated", exam.ExamId);
            }
        }

        /// <summary>
        /// Helper method to strip model answers from exam response
        /// Students should NOT see model answers when exam is generated
        /// Answers are only shown AFTER evaluation in the results endpoint
        /// </summary>
        private GeneratedExamResponse StripModelAnswers(GeneratedExamResponse exam)
        {
            // Create a deep copy to avoid modifying the cached/stored version
            var publicExam = new GeneratedExamResponse
            {
                ExamId = exam.ExamId,
                Subject = exam.Subject,
                Grade = exam.Grade,
                Chapter = exam.Chapter,
                Difficulty = exam.Difficulty,
                ExamType = exam.ExamType,
                TotalMarks = exam.TotalMarks,
                Duration = exam.Duration,
                Instructions = exam.Instructions,
                QuestionCount = exam.QuestionCount,
                CreatedAt = exam.CreatedAt,
                Parts = new List<ExamPart>(),
                Questions = new List<GeneratedQuestion>()
            };

            // Strip correctAnswer from Parts structure
            if (exam.Parts != null)
            {
                foreach (var part in exam.Parts)
                {
                    var publicPart = new ExamPart
                    {
                        PartName = part.PartName,
                        PartDescription = part.PartDescription,
                        QuestionType = part.QuestionType,
                        MarksPerQuestion = part.MarksPerQuestion,
                        TotalQuestions = part.TotalQuestions,
                        QuestionsToAnswer = part.QuestionsToAnswer,
                        Questions = new List<PartQuestion>()
                    };

                    foreach (var q in part.Questions)
                    {
                        var publicQ = new PartQuestion
                        {
                            QuestionId = q.QuestionId,
                            QuestionNumber = q.QuestionNumber,
                            QuestionText = q.QuestionText,
                            Options = q.Options,
                            CorrectAnswer = string.Empty, // REMOVED - don't show to students
                            Topic = q.Topic,
                            SubParts = q.SubParts?.Select(sp => new SubPart
                            {
                                PartLabel = sp.PartLabel,
                                QuestionText = sp.QuestionText,
                                CorrectAnswer = string.Empty // REMOVED - don't show to students
                            }).ToList()
                        };
                        publicPart.Questions.Add(publicQ);
                    }
                    publicExam.Parts.Add(publicPart);
                }
            }

            // Strip correctAnswer from flat Questions array (legacy support)
            if (exam.Questions != null)
            {
                foreach (var q in exam.Questions)
                {
                    publicExam.Questions.Add(new GeneratedQuestion
                    {
                        QuestionId = q.QuestionId,
                        QuestionText = q.QuestionText,
                        Options = q.Options,
                        CorrectAnswer = string.Empty, // REMOVED - don't show to students
                        Difficulty = q.Difficulty,
                        Marks = q.Marks
                    });
                }
            }

            return publicExam;
        }
    }

    #region Request/Response Models

    /// <summary>
    /// Request model for generating Karnataka 2nd PUC style exam
    /// </summary>
    public class GenerateExamRequest
    {
        /// <summary>
        /// Subject name (e.g., "Mathematics", "Physics", "Chemistry")
        /// </summary>
        [Required]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Grade/Class (e.g., "12", "2nd PUC", "II PUC")
        /// </summary>
        [Required]
        public string Grade { get; set; } = string.Empty;
        
        /// <summary>
        /// Chapter to cover (optional, default: "All Chapters")
        /// </summary>
        public string? Chapter { get; set; }
        
        /// <summary>
        /// Difficulty level (optional, default: "Medium")
        /// </summary>
        public string? Difficulty { get; set; }
        
        /// <summary>
        /// Exam type (optional, default: "Full Paper")
        /// </summary>
        public string? ExamType { get; set; }
        
        /// <summary>
        /// Use cached exam if available (default: false to always generate fresh questions)
        /// </summary>
        public bool UseCache { get; set; } = false;
        
        /// <summary>
        /// Use faster model for generation (~15-30s vs ~60-90s)
        /// Default: true for faster generation
        /// </summary>
        public bool FastMode { get; set; } = true;
    }
    
    /// <summary>
    /// Progress tracking for exam generation
    /// </summary>
    public class ExamGenerationProgress
    {
        public string RequestId { get; set; } = string.Empty;
        public string Status { get; set; } = "pending"; // pending, generating, parsing, rubrics, complete, error
        public int ProgressPercent { get; set; } = 0;
        public string Message { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public GeneratedExamResponse? Result { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Generated exam paper response following Karnataka 2nd PUC format
    /// Structured with 5 parts (A, B, C, D, E) as per official model paper
    /// </summary>
    public class GeneratedExamResponse
    {
        [JsonPropertyName("examId")]
        public string ExamId { get; set; } = string.Empty;

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("grade")]
        public string Grade { get; set; } = string.Empty;

        [JsonPropertyName("chapter")]
        public string Chapter { get; set; } = string.Empty;

        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; } = string.Empty;

        [JsonPropertyName("examType")]
        public string ExamType { get; set; } = string.Empty;

        [JsonPropertyName("totalMarks")]
        public int TotalMarks { get; set; } = 80;

        [JsonPropertyName("duration")]
        public int Duration { get; set; } = 195;

        [JsonPropertyName("instructions")]
        public List<string> Instructions { get; set; } = new();

        [JsonPropertyName("parts")]
        public List<ExamPart> Parts { get; set; } = new();

        [JsonPropertyName("questionCount")]
        public int QuestionCount { get; set; } = 52;

        [JsonPropertyName("questions")]
        public List<GeneratedQuestion> Questions { get; set; } = new();

        [JsonPropertyName("createdAt")]
        public string CreatedAt { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a part of the Karnataka 2nd PUC exam (Part A, B, C, D, or E)
    /// </summary>
    public class ExamPart
    {
        [JsonPropertyName("partName")]
        public string PartName { get; set; } = string.Empty;

        [JsonPropertyName("partDescription")]
        public string PartDescription { get; set; } = string.Empty;

        [JsonPropertyName("questionType")]
        public string QuestionType { get; set; } = string.Empty;

        [JsonPropertyName("marksPerQuestion")]
        public int MarksPerQuestion { get; set; }

        [JsonPropertyName("totalQuestions")]
        public int TotalQuestions { get; set; }

        [JsonPropertyName("questionsToAnswer")]
        public int QuestionsToAnswer { get; set; }

        [JsonPropertyName("questions")]
        public List<PartQuestion> Questions { get; set; } = new();
    }

    /// <summary>
    /// Question within an exam part
    /// </summary>
    public class PartQuestion
    {
        [JsonPropertyName("questionId")]
        public string QuestionId { get; set; } = string.Empty;

        [JsonPropertyName("questionNumber")]
        public int QuestionNumber { get; set; }

        [JsonPropertyName("questionText")]
        public string QuestionText { get; set; } = string.Empty;

        [JsonPropertyName("options")]
        public List<string> Options { get; set; } = new();

        [JsonPropertyName("correctAnswer")]
        public string CorrectAnswer { get; set; } = string.Empty;

        [JsonPropertyName("topic")]
        public string Topic { get; set; } = string.Empty;

        [JsonPropertyName("subParts")]
        public List<SubPart>? SubParts { get; set; }
    }

    /// <summary>
    /// Sub-part of a question (for Part E questions with a and b parts)
    /// </summary>
    public class SubPart
    {
        [JsonPropertyName("partLabel")]
        public string PartLabel { get; set; } = string.Empty;

        [JsonPropertyName("questionText")]
        public string QuestionText { get; set; } = string.Empty;

        [JsonPropertyName("correctAnswer")]
        public string CorrectAnswer { get; set; } = string.Empty;
    }

    /// <summary>
    /// Individual question (legacy format support)
    /// For MCQ: options contains 4 choices, correctAnswer is one of the options
    /// For Non-MCQ: options is empty array, correctAnswer contains the model answer
    /// </summary>
    public class GeneratedQuestion
    {
        [JsonPropertyName("questionId")]
        public string QuestionId { get; set; } = string.Empty;

        [JsonPropertyName("questionText")]
        public string QuestionText { get; set; } = string.Empty;

        [JsonPropertyName("options")]
        public List<string> Options { get; set; } = new();

        [JsonPropertyName("correctAnswer")]
        public string CorrectAnswer { get; set; } = string.Empty;

        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; } = "Medium";

        [JsonPropertyName("marks")]
        public int Marks { get; set; } = 1;
    }

    #region Exam Submission Models

    /// <summary>
    /// Request model for submitting exam answers
    /// Supports both text answers and image uploads of handwritten answers
    /// </summary>
    public class SubmitExamRequest
    {
        /// <summary>
        /// The exam ID from the generated exam
        /// </summary>
        [Required]
        public string ExamId { get; set; } = string.Empty;

        /// <summary>
        /// List of answers submitted by the student
        /// </summary>
        [Required]
        public List<AnswerSubmission> Answers { get; set; } = new();
    }

    /// <summary>
    /// Individual answer submission - can be text or image
    /// </summary>
    public class AnswerSubmission
    {
        /// <summary>
        /// The question ID (e.g., "A1", "B2", "C3")
        /// </summary>
        [Required]
        public string QuestionId { get; set; } = string.Empty;

        /// <summary>
        /// The question number (1-39)
        /// </summary>
        public int QuestionNumber { get; set; }

        /// <summary>
        /// The original question text (for AI evaluation context)
        /// </summary>
        [Required]
        public string QuestionText { get; set; } = string.Empty;

        /// <summary>
        /// The correct/model answer (for AI evaluation comparison)
        /// </summary>
        [Required]
        public string CorrectAnswer { get; set; } = string.Empty;

        /// <summary>
        /// Maximum marks for this question
        /// </summary>
        public int MaxMarks { get; set; } = 1;

        /// <summary>
        /// Student's text answer (optional - use either TextAnswer or ImageFile)
        /// </summary>
        public string? TextAnswer { get; set; }

        /// <summary>
        /// Image file of handwritten answer (optional - use either TextAnswer or ImageFile)
        /// Supported formats: JPEG, PNG, GIF, WebP
        /// </summary>
        public IFormFile? ImageFile { get; set; }
    }

    /// <summary>
    /// Response from AI evaluation (internal use)
    /// </summary>
    internal class EvaluationJsonResponse
    {
        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("feedback")]
        public string Feedback { get; set; } = string.Empty;

        [JsonPropertyName("isCorrect")]
        public bool IsCorrect { get; set; }

        [JsonPropertyName("extractedText")]
        public string? ExtractedText { get; set; }
    }

    /// <summary>
    /// Complete exam submission response with scores
    /// </summary>
    public class ExamSubmissionResponse
    {
        [JsonPropertyName("examId")]
        public string ExamId { get; set; } = string.Empty;

        [JsonPropertyName("totalScore")]
        public int TotalScore { get; set; }

        [JsonPropertyName("totalMaxScore")]
        public int TotalMaxScore { get; set; }

        [JsonPropertyName("percentage")]
        public double Percentage { get; set; }

        [JsonPropertyName("grade")]
        public string Grade { get; set; } = string.Empty;

        [JsonPropertyName("questionsAnswered")]
        public int QuestionsAnswered { get; set; }

        [JsonPropertyName("totalQuestions")]
        public int TotalQuestions { get; set; }

        [JsonPropertyName("results")]
        public List<AnswerEvaluationResult> Results { get; set; } = new();

        [JsonPropertyName("evaluatedAt")]
        public string EvaluatedAt { get; set; } = string.Empty;
    }

    /// <summary>
    /// Individual answer evaluation result
    /// </summary>
    public class AnswerEvaluationResult
    {
        [JsonPropertyName("questionId")]
        public string QuestionId { get; set; } = string.Empty;

        [JsonPropertyName("questionNumber")]
        public int QuestionNumber { get; set; }

        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("maxMarks")]
        public int MaxMarks { get; set; }

        [JsonPropertyName("feedback")]
        public string Feedback { get; set; } = string.Empty;

        [JsonPropertyName("isCorrect")]
        public bool IsCorrect { get; set; }

        /// <summary>
        /// Text extracted from image (only for image submissions)
        /// </summary>
        [JsonPropertyName("extractedText")]
        public string? ExtractedText { get; set; }
    }

    #endregion

    #endregion
}
