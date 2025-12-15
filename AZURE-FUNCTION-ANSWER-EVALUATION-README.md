# Azure Functions - Answer Sheet Evaluation System

Complete guide for Azure Functions implementation of the answer sheet evaluation and results storage system.

---

## 📋 Overview

This document provides comprehensive Azure Functions implementation for:
- **Answer sheet upload and processing**
- **OCR text extraction** 
- **AI-powered evaluation**
- **Permanent results storage in Azure Blob**
- **Real-time status tracking**

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         Mobile/Web App                          │
└──────────────────┬──────────────────────────────────────────────┘
                   │
                   ↓ HTTP Trigger (Upload Answer Sheet)
┌─────────────────────────────────────────────────────────────────┐
│              Azure Function: UploadAnswerSheet                  │
│  - Validates files (size, format, count)                       │
│  - Stores files in Blob Storage (answer-sheets container)      │
│  - Creates WrittenSubmission record (Status: PendingEvaluation)│
│  - Queues message for processing                                │
└──────────────────┬──────────────────────────────────────────────┘
                   │
                   ↓ Queue Trigger
┌─────────────────────────────────────────────────────────────────┐
│         Azure Function: ProcessWrittenSubmission                │
│                                                                  │
│  Step 1: Update Status → OcrProcessing                         │
│  Step 2: Extract text from images (Azure Computer Vision OCR)  │
│  Step 3: Update Status → Evaluating                            │
│  Step 4: Send to Azure OpenAI for evaluation                   │
│  Step 5: Save result JSON to Blob Storage                      │
│  Step 6: Update Status → Completed (with blob path)            │
│  Step 7: Store score/grade in database                         │
└──────────────────┬──────────────────────────────────────────────┘
                   │
                   ↓ HTTP Trigger (Get Results)
┌─────────────────────────────────────────────────────────────────┐
│         Azure Function: GetEvaluationResult                     │
│  - Fetches blob path from database                             │
│  - Downloads JSON from Blob Storage                             │
│  - Returns complete evaluation with step-wise marks            │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📁 Storage Architecture

### Blob Storage Structure
```
smartstudy-storage/
├── answer-sheets/                         (Uploaded answer images)
│   ├── Karnataka_2nd_PUC_Math_2024_25/
│   │   └── a1b2c3d4-submission-id/
│   │       ├── page-1.jpg
│   │       ├── page-2.jpg
│   │       └── page-3.jpg
│   └── SAMPLE-EXAM-001/
│       └── b2c3d4e5-submission-id/
│           └── answer.pdf
│
└── evaluation-results/                    (Evaluation JSON results)
    ├── Karnataka_2nd_PUC_Math_2024_25/
    │   └── a1b2c3d4-submission-id/
    │       └── evaluation-result.json
    └── SAMPLE-EXAM-001/
        └── b2c3d4e5-submission-id/
            └── evaluation-result.json
```

### Database Schema
```sql
CREATE TABLE WrittenSubmissions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ExamId NVARCHAR(200) NOT NULL,
    StudentId NVARCHAR(200) NOT NULL,
    
    -- Status tracking
    Status INT NOT NULL,                          -- 0=PendingEvaluation, 1=OcrProcessing, 2=Evaluating, 3=Completed, 4=Failed
    
    -- Timestamps
    SubmittedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    OcrStartedAt DATETIME2,
    OcrCompletedAt DATETIME2,
    EvaluationStartedAt DATETIME2,
    EvaluatedAt DATETIME2,
    
    -- Scores
    TotalScore DECIMAL(10,2),
    MaxPossibleScore DECIMAL(10,2),
    Percentage DECIMAL(5,2),
    Grade NVARCHAR(10),
    
    -- Storage paths
    AnswerSheetBlobPath NVARCHAR(500),            -- Path to uploaded answer sheets
    EvaluationResultBlobPath NVARCHAR(500),       -- Path to evaluation result JSON
    
    -- Performance metrics
    OcrProcessingTimeMs BIGINT,
    EvaluationProcessingTimeMs BIGINT,
    
    -- Error handling
    ErrorMessage NVARCHAR(MAX),
    RetryCount INT DEFAULT 0,
    
    CONSTRAINT FK_WrittenSubmissions_Exams FOREIGN KEY (ExamId) REFERENCES Exams(Id),
    CONSTRAINT CK_WrittenSubmissions_Status CHECK (Status BETWEEN 0 AND 4)
);

CREATE NONCLUSTERED INDEX IX_WrittenSubmissions_ExamStudent 
ON WrittenSubmissions(ExamId, StudentId);

CREATE NONCLUSTERED INDEX IX_WrittenSubmissions_Status 
ON WrittenSubmissions(Status) WHERE Status < 3;

CREATE NONCLUSTERED INDEX IX_WrittenSubmissions_EvaluationResultBlobPath
ON WrittenSubmissions(EvaluationResultBlobPath)
WHERE EvaluationResultBlobPath IS NOT NULL;
```

---

## 🔧 Azure Function Implementations

### 1. UploadAnswerSheet Function

**Trigger:** HTTP POST  
**Route:** `/api/exam/upload-written`  
**Purpose:** Accept answer sheet files from students

```csharp
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class UploadAnswerSheet
{
    private readonly ILogger<UploadAnswerSheet> _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IWrittenSubmissionRepository _repository;
    private readonly IQueueService _queueService;

    public UploadAnswerSheet(
        ILogger<UploadAnswerSheet> logger,
        BlobServiceClient blobServiceClient,
        IWrittenSubmissionRepository repository,
        IQueueService queueService)
    {
        _logger = logger;
        _blobServiceClient = blobServiceClient;
        _repository = repository;
        _queueService = queueService;
    }

    [Function("UploadAnswerSheet")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "exam/upload-written")] 
        HttpRequestData req,
        CancellationToken ct)
    {
        _logger.LogInformation("[UPLOAD_STARTED] Processing answer sheet upload");

        try
        {
            // 1. Parse multipart form data
            var formData = await req.ReadFormDataAsync(ct);
            var examId = formData["examId"];
            var studentId = formData["studentId"];
            var files = formData.Files.ToList();

            // 2. Validate inputs
            if (string.IsNullOrEmpty(examId) || string.IsNullOrEmpty(studentId))
            {
                return await CreateErrorResponse(req, 400, "ExamId and StudentId are required");
            }

            if (files.Count == 0)
            {
                return await CreateErrorResponse(req, 400, "No files provided");
            }

            if (files.Count > 20)
            {
                return await CreateErrorResponse(req, 400, "Maximum 20 files allowed");
            }

            // Validate file sizes and types
            foreach (var file in files)
            {
                if (file.Length > 10 * 1024 * 1024) // 10MB
                {
                    return await CreateErrorResponse(req, 400, $"File {file.FileName} exceeds 10MB limit");
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!new[] { ".jpg", ".jpeg", ".png", ".pdf" }.Contains(extension))
                {
                    return await CreateErrorResponse(req, 400, $"Invalid file type: {extension}");
                }
            }

            // 3. Check for duplicate submission
            var existing = await _repository.GetByExamAndStudentAsync(examId, studentId, ct);
            if (existing != null)
            {
                return await CreateErrorResponse(req, 409, "Student has already submitted answers for this exam");
            }

            // 4. Verify exam exists
            var exam = await _repository.GetExamByIdAsync(examId, ct);
            if (exam == null)
            {
                return await CreateErrorResponse(req, 404, $"Exam {examId} not found");
            }

            // 5. Create submission record
            var submissionId = Guid.NewGuid();
            var submission = new WrittenSubmission
            {
                Id = submissionId,
                ExamId = examId,
                StudentId = studentId,
                Status = WrittenSubmissionStatus.PendingEvaluation,
                SubmittedAt = DateTime.UtcNow,
                MaxPossibleScore = exam.TotalMarks
            };

            // 6. Upload files to Blob Storage
            var containerName = "answer-sheets";
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);

            var uploadedFiles = new List<string>();
            foreach (var (file, index) in files.Select((f, i) => (f, i)))
            {
                var blobName = $"{examId}/{submissionId}/page-{index + 1}{Path.GetExtension(file.FileName)}";
                var blobClient = containerClient.GetBlobClient(blobName);

                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: ct);
                
                uploadedFiles.Add(blobName);
                _logger.LogInformation("[FILE_UPLOADED] Blob={BlobName}, Size={Size}", blobName, file.Length);
            }

            submission.AnswerSheetBlobPath = string.Join(";", uploadedFiles);

            // 7. Save to database
            await _repository.CreateAsync(submission, ct);
            _logger.LogInformation("[SUBMISSION_CREATED] Id={Id}, ExamId={ExamId}, StudentId={StudentId}", 
                submissionId, examId, studentId);

            // 8. Queue for processing
            await _queueService.EnqueueSubmissionAsync(submissionId, ct);
            _logger.LogInformation("[QUEUED_FOR_PROCESSING] SubmissionId={Id}", submissionId);

            // 9. Return success response
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                writtenSubmissionId = submissionId,
                status = "PendingEvaluation",
                message = "✅ Answer sheet uploaded successfully! Processing will begin shortly.",
                filesUploaded = files.Count,
                examId,
                studentId
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UPLOAD_FAILED] Error uploading answer sheet");
            return await CreateErrorResponse(req, 500, "Internal server error during upload");
        }
    }

    private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, int statusCode, string error)
    {
        var response = req.CreateResponse((System.Net.HttpStatusCode)statusCode);
        await response.WriteAsJsonAsync(new { error });
        return response;
    }
}
```

---

### 2. ProcessWrittenSubmission Function

**Trigger:** Queue Message  
**Queue:** `written-submissions-queue`  
**Purpose:** OCR extraction and AI evaluation

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public class ProcessWrittenSubmission
{
    private readonly ILogger<ProcessWrittenSubmission> _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly DocumentAnalysisClient _documentAnalysisClient;
    private readonly IAzureOpenAIService _openAIService;
    private readonly IWrittenSubmissionRepository _repository;

    public ProcessWrittenSubmission(
        ILogger<ProcessWrittenSubmission> logger,
        BlobServiceClient blobServiceClient,
        DocumentAnalysisClient documentAnalysisClient,
        IAzureOpenAIService openAIService,
        IWrittenSubmissionRepository repository)
    {
        _logger = logger;
        _blobServiceClient = blobServiceClient;
        _documentAnalysisClient = documentAnalysisClient;
        _openAIService = openAIService;
        _repository = repository;
    }

    [Function("ProcessWrittenSubmission")]
    public async Task Run(
        [QueueTrigger("written-submissions-queue")] string message,
        CancellationToken ct)
    {
        var submissionId = Guid.Parse(message);
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[PROCESSING_STARTED] SubmissionId={Id}", submissionId);

        try
        {
            // 1. Get submission from database
            var submission = await _repository.GetByIdAsync(submissionId, ct);
            if (submission == null)
            {
                _logger.LogWarning("[SUBMISSION_NOT_FOUND] Id={Id}", submissionId);
                return;
            }

            // 2. Update status to OcrProcessing
            await _repository.UpdateStatusAsync(
                submissionId, 
                WrittenSubmissionStatus.OcrProcessing, 
                ocrStartedAt: DateTime.UtcNow, 
                ct);

            // 3. Extract text from images using OCR
            var extractedText = await ExtractTextFromAnswerSheetsAsync(
                submission.AnswerSheetBlobPath, 
                ct);
            
            var ocrCompletedAt = DateTime.UtcNow;
            var ocrTimeMs = (long)(ocrCompletedAt - submission.OcrStartedAt.Value).TotalMilliseconds;

            _logger.LogInformation("[OCR_COMPLETED] SubmissionId={Id}, TimeMs={Time}, TextLength={Length}", 
                submissionId, ocrTimeMs, extractedText.Length);

            // 4. Update status to Evaluating
            await _repository.UpdateStatusAsync(
                submissionId,
                WrittenSubmissionStatus.Evaluating,
                ocrCompletedAt: ocrCompletedAt,
                evaluationStartedAt: DateTime.UtcNow,
                ct);

            // 5. Get exam questions for evaluation
            var exam = await _repository.GetExamWithQuestionsAsync(submission.ExamId, ct);

            // 6. Evaluate using Azure OpenAI
            var evaluationResult = await EvaluateAnswersAsync(
                exam,
                extractedText,
                submission,
                ct);

            var evaluatedAt = DateTime.UtcNow;
            evaluationResult.EvaluatedAt = evaluatedAt;

            var evaluationTimeMs = (long)(evaluatedAt - submission.EvaluationStartedAt.Value).TotalMilliseconds;

            _logger.LogInformation("[EVALUATION_COMPLETED] SubmissionId={Id}, TimeMs={Time}, Score={Score}/{Max}", 
                submissionId, evaluationTimeMs, evaluationResult.TotalScore, evaluationResult.MaxPossibleScore);

            // 7. Save evaluation result to Blob Storage as JSON
            var resultBlobPath = await SaveEvaluationResultToBlobAsync(
                submissionId,
                submission.ExamId,
                evaluationResult,
                ct);

            _logger.LogInformation("[RESULT_SAVED_TO_BLOB] SubmissionId={Id}, BlobPath={Path}", 
                submissionId, resultBlobPath);

            // 8. Update database with final results
            await _repository.SaveEvaluationResultAsync(
                evaluationResult,
                resultBlobPath,
                evaluationTimeMs,
                ct);

            var totalTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[PROCESSING_COMPLETED] SubmissionId={Id}, TotalTimeMs={Time}", 
                submissionId, totalTimeMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PROCESSING_FAILED] SubmissionId={Id}", submissionId);

            // Mark as failed in database
            await _repository.UpdateStatusAsync(
                submissionId,
                WrittenSubmissionStatus.Failed,
                errorMessage: ex.Message,
                ct: ct);
        }
    }

    /// <summary>
    /// Extracts text from answer sheet images using Azure Computer Vision OCR
    /// </summary>
    private async Task<string> ExtractTextFromAnswerSheetsAsync(
        string blobPaths,
        CancellationToken ct)
    {
        var paths = blobPaths.Split(';');
        var extractedTexts = new List<string>();

        var containerClient = _blobServiceClient.GetBlobContainerClient("answer-sheets");

        foreach (var path in paths)
        {
            var blobClient = containerClient.GetBlobClient(path);
            var blobUri = blobClient.Uri;

            // Start OCR analysis
            var operation = await _documentAnalysisClient.AnalyzeDocumentFromUriAsync(
                WaitUntil.Completed,
                "prebuilt-read",
                blobUri,
                cancellationToken: ct);

            var result = operation.Value;

            // Extract all text
            var pageTexts = result.Pages
                .Select(page => string.Join("\n", page.Lines.Select(line => line.Content)))
                .ToList();

            extractedTexts.Add(string.Join("\n\n", pageTexts));
        }

        return string.Join("\n\n=== PAGE BREAK ===\n\n", extractedTexts);
    }

    /// <summary>
    /// Evaluates student answers using Azure OpenAI
    /// </summary>
    private async Task<WrittenEvaluationResult> EvaluateAnswersAsync(
        Exam exam,
        string extractedText,
        WrittenSubmission submission,
        CancellationToken ct)
    {
        var subjectiveQuestions = exam.Questions
            .Where(q => q.Type == QuestionType.Subjective)
            .ToList();

        var questionEvaluations = new List<QuestionEvaluation>();
        decimal totalScore = 0;
        decimal maxPossibleScore = 0;

        foreach (var question in subjectiveQuestions)
        {
            var prompt = BuildEvaluationPrompt(question, extractedText);

            var aiResponse = await _openAIService.EvaluateAnswerAsync(prompt, ct);

            var evaluation = new QuestionEvaluation
            {
                Id = Guid.NewGuid(),
                WrittenSubmissionId = submission.Id,
                QuestionId = question.Id,
                QuestionNumber = question.QuestionNumber,
                ExtractedAnswer = ExtractStudentAnswerForQuestion(extractedText, question.QuestionNumber),
                ModelAnswer = question.ExpectedAnswer,
                MaxScore = question.Marks,
                AwardedScore = aiResponse.Score,
                Feedback = aiResponse.Feedback,
                RubricBreakdown = aiResponse.StepwiseBreakdown,
                EvaluatedAt = DateTime.UtcNow
            };

            questionEvaluations.Add(evaluation);
            totalScore += aiResponse.Score;
            maxPossibleScore += question.Marks;
        }

        var percentage = maxPossibleScore > 0 ? (totalScore / maxPossibleScore) * 100 : 0;
        var grade = CalculateGrade(percentage);

        return new WrittenEvaluationResult
        {
            WrittenSubmissionId = submission.Id,
            ExamId = submission.ExamId,
            StudentId = submission.StudentId,
            TotalScore = totalScore,
            MaxPossibleScore = maxPossibleScore,
            Percentage = percentage,
            Grade = grade,
            QuestionEvaluations = questionEvaluations
        };
    }

    /// <summary>
    /// Builds evaluation prompt for Azure OpenAI
    /// </summary>
    private string BuildEvaluationPrompt(Question question, string extractedText)
    {
        return $@"
You are an expert exam evaluator. Evaluate the student's answer with step-wise marking.

**Question:** {question.QuestionText}

**Expected Answer (Model Answer):**
{question.ExpectedAnswer}

**Maximum Marks:** {question.Marks}

**Student's Extracted Answer:**
{extractedText}

**Instructions:**
1. Identify the student's answer for this question from the extracted text
2. Evaluate step-by-step based on the marking rubric
3. Award partial marks for partially correct steps
4. Provide specific feedback on what was correct and what was missing
5. Return JSON in this format:

{{
  ""score"": <decimal score>,
  ""feedback"": ""<overall feedback>"",
  ""stepwiseBreakdown"": ""Step 1: <description> (<marks awarded>/<max marks>) ✓/✗\nStep 2: ...\n...""
}}

Be fair, accurate, and provide constructive feedback.
";
    }

    /// <summary>
    /// Saves complete evaluation result as JSON to Blob Storage
    /// </summary>
    private async Task<string> SaveEvaluationResultToBlobAsync(
        Guid submissionId,
        string examId,
        WrittenEvaluationResult result,
        CancellationToken ct)
    {
        var containerName = "evaluation-results";
        var blobPath = $"{examId}/{submissionId}/evaluation-result.json";

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);

        var blobClient = containerClient.GetBlobClient(blobPath);

        // Serialize with pretty formatting
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var json = JsonSerializer.Serialize(result, jsonOptions);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = "application/json"
            }
        };

        await blobClient.UploadAsync(stream, uploadOptions, ct);

        return $"{containerName}/{blobPath}";
    }

    private string CalculateGrade(decimal percentage)
    {
        return percentage switch
        {
            >= 90 => "A+",
            >= 80 => "A",
            >= 70 => "B+",
            >= 60 => "B",
            >= 50 => "C",
            >= 40 => "D",
            _ => "F"
        };
    }

    private string ExtractStudentAnswerForQuestion(string fullText, int questionNumber)
    {
        // Logic to extract specific question answer from full extracted text
        // This could use regex patterns like "Question 1:", "Q1:", etc.
        var lines = fullText.Split('\n');
        var answerLines = new List<string>();
        bool capturing = false;

        foreach (var line in lines)
        {
            if (line.Contains($"Question {questionNumber}") || line.Contains($"Q{questionNumber}"))
            {
                capturing = true;
                continue;
            }

            if (capturing)
            {
                if (line.Contains($"Question {questionNumber + 1}") || line.Contains($"Q{questionNumber + 1}"))
                {
                    break;
                }
                answerLines.Add(line);
            }
        }

        return string.Join("\n", answerLines).Trim();
    }
}
```

---

### 3. GetSubmissionStatus Function

**Trigger:** HTTP GET  
**Route:** `/api/exam/submission-status/{submissionId}`  
**Purpose:** Check evaluation progress

```csharp
[Function("GetSubmissionStatus")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "exam/submission-status/{submissionId}")] 
    HttpRequestData req,
    string submissionId,
    CancellationToken ct)
{
    _logger.LogInformation("[STATUS_CHECK] SubmissionId={Id}", submissionId);

    try
    {
        var submission = await _repository.GetByIdAsync(Guid.Parse(submissionId), ct);

        if (submission == null)
        {
            var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(new { error = "Submission not found" });
            return notFoundResponse;
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            writtenSubmissionId = submission.Id,
            status = submission.Status.ToString(),
            statusMessage = GetStatusMessage(submission.Status),
            submittedAt = submission.SubmittedAt,
            ocrStartedAt = submission.OcrStartedAt,
            ocrCompletedAt = submission.OcrCompletedAt,
            evaluationStartedAt = submission.EvaluationStartedAt,
            evaluatedAt = submission.EvaluatedAt,
            isComplete = submission.Status == WrittenSubmissionStatus.Completed,
            examId = submission.ExamId,
            studentId = submission.StudentId,
            totalScore = submission.TotalScore,
            percentage = submission.Percentage,
            grade = submission.Grade,
            evaluationResultBlobPath = submission.EvaluationResultBlobPath
        });

        return response;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[STATUS_CHECK_FAILED] SubmissionId={Id}", submissionId);
        var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
        await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
        return errorResponse;
    }
}

private string GetStatusMessage(WrittenSubmissionStatus status)
{
    return status switch
    {
        WrittenSubmissionStatus.PendingEvaluation => "⏳ Answer sheet uploaded, waiting for processing...",
        WrittenSubmissionStatus.OcrProcessing => "📄 Extracting text from your answer sheet...",
        WrittenSubmissionStatus.Evaluating => "🤖 AI is evaluating your answers...",
        WrittenSubmissionStatus.Completed => "✅ Evaluation completed successfully!",
        WrittenSubmissionStatus.Failed => "❌ Evaluation failed. Please contact support.",
        _ => "Unknown status"
    };
}
```

---

### 4. GetEvaluationResult Function

**Trigger:** HTTP GET  
**Route:** `/api/exam/result/{examId}/{studentId}`  
**Purpose:** Retrieve detailed evaluation results from Blob Storage

```csharp
[Function("GetEvaluationResult")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "exam/result/{examId}/{studentId}")] 
    HttpRequestData req,
    string examId,
    string studentId,
    CancellationToken ct)
{
    _logger.LogInformation("[RESULT_REQUEST] ExamId={ExamId}, StudentId={StudentId}", examId, studentId);

    try
    {
        // 1. Get submission from database
        var submission = await _repository.GetByExamAndStudentAsync(examId, studentId, ct);

        if (submission == null)
        {
            var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(new { error = "No submission found for this exam and student" });
            return notFoundResponse;
        }

        if (submission.Status != WrittenSubmissionStatus.Completed)
        {
            var pendingResponse = req.CreateResponse(System.Net.HttpStatusCode.Accepted);
            await pendingResponse.WriteAsJsonAsync(new 
            { 
                message = "Evaluation still in progress",
                status = submission.Status.ToString(),
                submissionId = submission.Id
            });
            return pendingResponse;
        }

        if (string.IsNullOrEmpty(submission.EvaluationResultBlobPath))
        {
            var noResultResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
            await noResultResponse.WriteAsJsonAsync(new { error = "Evaluation result not available" });
            return noResultResponse;
        }

        // 2. Download evaluation result JSON from Blob Storage
        var parts = submission.EvaluationResultBlobPath.Split('/', 2);
        var containerName = parts[0];
        var blobPath = parts[1];

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        if (!await blobClient.ExistsAsync(ct))
        {
            _logger.LogWarning("[BLOB_NOT_FOUND] Path={Path}", submission.EvaluationResultBlobPath);
            var blobNotFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
            await blobNotFoundResponse.WriteAsJsonAsync(new { error = "Evaluation result file not found in storage" });
            return blobNotFoundResponse;
        }

        // 3. Read and parse JSON
        var downloadResponse = await blobClient.DownloadContentAsync(ct);
        var json = downloadResponse.Value.Content.ToString();
        var evaluationResult = JsonSerializer.Deserialize<WrittenEvaluationResult>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        _logger.LogInformation("[RESULT_RETRIEVED] SubmissionId={Id}, Score={Score}/{Max}", 
            submission.Id, evaluationResult.TotalScore, evaluationResult.MaxPossibleScore);

        // 4. Return result
        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(evaluationResult);
        return response;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[RESULT_REQUEST_FAILED] ExamId={ExamId}, StudentId={StudentId}", examId, studentId);
        var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
        await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
        return errorResponse;
    }
}
```

---

### 5. GetEvaluationDownloadUrl Function (Optional - Direct Blob Access)

**Trigger:** HTTP GET  
**Route:** `/api/evaluations/{submissionId}/download-url`  
**Purpose:** Generate SAS token for direct blob access

```csharp
[Function("GetEvaluationDownloadUrl")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "evaluations/{submissionId}/download-url")] 
    HttpRequestData req,
    string submissionId,
    CancellationToken ct)
{
    _logger.LogInformation("[DOWNLOAD_URL_REQUEST] SubmissionId={Id}", submissionId);

    try
    {
        var submission = await _repository.GetByIdAsync(Guid.Parse(submissionId), ct);

        if (submission == null || string.IsNullOrEmpty(submission.EvaluationResultBlobPath))
        {
            var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(new { error = "Evaluation result not available" });
            return notFoundResponse;
        }

        var parts = submission.EvaluationResultBlobPath.Split('/', 2);
        var containerClient = _blobServiceClient.GetBlobContainerClient(parts[0]);
        var blobClient = containerClient.GetBlobClient(parts[1]);

        // Generate SAS token valid for 24 hours
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = parts[0],
            BlobName = parts[1],
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow,
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(24)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);

        _logger.LogInformation("[SAS_GENERATED] SubmissionId={Id}, ExpiresAt={ExpiresAt}", 
            submissionId, sasBuilder.ExpiresOn);

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            submissionId,
            downloadUrl = sasUri.ToString(),
            expiresAt = sasBuilder.ExpiresOn
        });
        return response;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[DOWNLOAD_URL_FAILED] SubmissionId={Id}", submissionId);
        var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
        await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
        return errorResponse;
    }
}
```

---

## 📄 JSON Response Format

### Complete Evaluation Result JSON (Stored in Blob)
```json
{
  "writtenSubmissionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "examId": "Karnataka_2nd_PUC_Math_2024_25",
  "studentId": "STUDENT-12345",
  "totalScore": 28.5,
  "maxPossibleScore": 40.0,
  "percentage": 71.25,
  "grade": "B+",
  "evaluatedAt": "2025-12-15T11:30:00.123Z",
  "questionEvaluations": [
    {
      "id": "eval-uuid-1",
      "writtenSubmissionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "questionId": "q-uuid-1",
      "questionNumber": 1,
      "extractedAnswer": "det(A) = 2*5 - 3*4 = 10 - 12 = -2",
      "modelAnswer": "Step 1: Identify matrix elements a=2, b=3, c=4, d=5\nStep 2: Apply formula det(A) = ad - bc\nStep 3: det(A) = (2)(5) - (3)(4) = 10 - 12 = -2\nAnswer: -2",
      "maxScore": 5.0,
      "awardedScore": 4.5,
      "feedback": "Good work! Calculation is correct. Consider showing matrix element identification explicitly for full marks.",
      "rubricBreakdown": "Step 1: Matrix identification (0/1) ✗ - Not shown\nStep 2: Formula application (2/2) ✓ - Correct\nStep 3: Calculation (2.5/2) ✓ - Perfect calculation",
      "evaluatedAt": "2025-12-15T11:30:00.123Z"
    },
    {
      "id": "eval-uuid-2",
      "writtenSubmissionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "questionId": "q-uuid-2",
      "questionNumber": 2,
      "extractedAnswer": "Incomplete answer extracted...",
      "modelAnswer": "Complete expected answer with all steps...",
      "maxScore": 10.0,
      "awardedScore": 3.5,
      "feedback": "Answer is incomplete. Missing key steps and final conclusion.",
      "rubricBreakdown": "Step 1: (1/3) Partial\nStep 2: (2.5/4) Good attempt\nStep 3: (0/3) Not attempted",
      "evaluatedAt": "2025-12-15T11:30:00.123Z"
    }
  ]
}
```

---

## ⚙️ Configuration (local.settings.json)

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    
    "DatabaseConnectionString": "Server=tcp:smartstudy-server.database.windows.net,1433;Initial Catalog=SmartStudyDB;Persist Security Info=False;User ID=sqladmin;Password=YourPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;",
    
    "BlobStorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=stsmartstudydev;AccountKey=xxx;EndpointSuffix=core.windows.net",
    
    "ComputerVisionEndpoint": "https://smartstudy-cv.cognitiveservices.azure.com/",
    "ComputerVisionKey": "your-computer-vision-key",
    
    "OpenAIEndpoint": "https://smartstudy-openai.openai.azure.com/",
    "OpenAIKey": "your-openai-key",
    "OpenAIDeploymentName": "gpt-4",
    
    "QueueName": "written-submissions-queue"
  }
}
```

---

## 🧪 Testing

### 1. Test Upload Function

```powershell
# Upload answer sheet
$examId = "Karnataka_2nd_PUC_Math_2024_25"
$studentId = "STUDENT-TEST-001"
$file1 = "C:\test-answers\page1.jpg"
$file2 = "C:\test-answers\page2.jpg"

$form = @{
    examId = $examId
    studentId = $studentId
    files = Get-Item $file1, $file2
}

$result = Invoke-RestMethod `
    -Uri "http://localhost:7071/api/exam/upload-written" `
    -Method POST `
    -Form $form

Write-Host "Submission ID: $($result.writtenSubmissionId)"
Write-Host "Status: $($result.status)"
```

### 2. Poll Status

```powershell
$submissionId = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"

for ($i = 1; $i -le 30; $i++) {
    $status = Invoke-RestMethod `
        -Uri "http://localhost:7071/api/exam/submission-status/$submissionId"
    
    Write-Host "[$i] Status: $($status.status) - $($status.statusMessage)"
    
    if ($status.isComplete) {
        Write-Host "✅ Evaluation completed!"
        Write-Host "Score: $($status.totalScore) / $($status.maxPossibleScore)"
        Write-Host "Grade: $($status.grade)"
        break
    }
    
    Start-Sleep -Seconds 3
}
```

### 3. Get Results

```powershell
$examId = "Karnataka_2nd_PUC_Math_2024_25"
$studentId = "STUDENT-TEST-001"

$result = Invoke-RestMethod `
    -Uri "http://localhost:7071/api/exam/result/$examId/$studentId"

Write-Host "=== EVALUATION RESULTS ==="
Write-Host "Total Score: $($result.totalScore) / $($result.maxPossibleScore)"
Write-Host "Percentage: $($result.percentage)%"
Write-Host "Grade: $($result.grade)"
Write-Host ""

foreach ($q in $result.questionEvaluations) {
    Write-Host "Question $($q.questionNumber): $($q.awardedScore)/$($q.maxScore) marks"
    Write-Host "Feedback: $($q.feedback)"
    Write-Host "Rubric:"
    Write-Host $q.rubricBreakdown
    Write-Host ""
}
```

---

## 🚀 Deployment

### 1. Create Azure Resources

```bash
# Resource Group
az group create --name smartstudy-rg --location eastus

# Storage Account
az storage account create \
  --name stsmartstudydev \
  --resource-group smartstudy-rg \
  --location eastus \
  --sku Standard_LRS

# Function App
az functionapp create \
  --name smartstudy-functions \
  --resource-group smartstudy-rg \
  --consumption-plan-location eastus \
  --runtime dotnet-isolated \
  --runtime-version 8 \
  --functions-version 4 \
  --storage-account stsmartstudydev

# Computer Vision
az cognitiveservices account create \
  --name smartstudy-cv \
  --resource-group smartstudy-rg \
  --kind ComputerVision \
  --sku S1 \
  --location eastus

# OpenAI
az cognitiveservices account create \
  --name smartstudy-openai \
  --resource-group smartstudy-rg \
  --kind OpenAI \
  --sku S0 \
  --location eastus
```

### 2. Deploy Function App

```bash
# Build and publish
dotnet publish -c Release -o ./publish

# Deploy
func azure functionapp publish smartstudy-functions
```

### 3. Configure App Settings

```bash
# Database connection
az functionapp config appsettings set \
  --name smartstudy-functions \
  --resource-group smartstudy-rg \
  --settings DatabaseConnectionString="Server=tcp:..."

# Blob storage
az functionapp config appsettings set \
  --name smartstudy-functions \
  --resource-group smartstudy-rg \
  --settings BlobStorageConnectionString="DefaultEndpointsProtocol=https..."

# Computer Vision
az functionapp config appsettings set \
  --name smartstudy-functions \
  --resource-group smartstudy-rg \
  --settings ComputerVisionEndpoint="https://..." ComputerVisionKey="xxx"

# OpenAI
az functionapp config appsettings set \
  --name smartstudy-functions \
  --resource-group smartstudy-rg \
  --settings OpenAIEndpoint="https://..." OpenAIKey="xxx" OpenAIDeploymentName="gpt-4"
```

---

## 📊 Monitoring

### Application Insights Queries

```kusto
// Evaluation processing times
traces
| where timestamp > ago(24h)
| where message contains "PROCESSING_COMPLETED"
| extend SubmissionId = tostring(customDimensions.SubmissionId)
| extend TotalTimeMs = tolong(customDimensions.TotalTimeMs)
| summarize 
    AvgTimeMs = avg(TotalTimeMs),
    MedianTimeMs = percentile(TotalTimeMs, 50),
    P95TimeMs = percentile(TotalTimeMs, 95),
    Count = count()
| project AvgTimeMs, MedianTimeMs, P95TimeMs, Count

// Error rate
traces
| where timestamp > ago(1h)
| where message contains "PROCESSING_FAILED" or message contains "UPLOAD_FAILED"
| summarize ErrorCount = count() by bin(timestamp, 5m)
| render timechart

// OCR performance
traces
| where timestamp > ago(24h)
| where message contains "OCR_COMPLETED"
| extend TimeMs = tolong(customDimensions.TimeMs)
| extend TextLength = tolong(customDimensions.TextLength)
| project timestamp, TimeMs, TextLength
| render scatterchart
```

---

## 🔒 Security Best Practices

1. **Authentication**: Use Azure AD authentication for functions
2. **Authorization**: Validate student ownership before returning results
3. **SAS Tokens**: Use time-limited SAS tokens for blob access
4. **Secrets**: Store keys in Azure Key Vault
5. **CORS**: Configure CORS for allowed mobile/web origins
6. **Input Validation**: Validate all file uploads and parameters
7. **Rate Limiting**: Implement throttling to prevent abuse

---

## 📞 Support & Troubleshooting

### Common Issues

**Issue**: "Submission not found"  
**Solution**: Check if submission ID is correct and exists in database

**Issue**: "Evaluation result file not found"  
**Solution**: Verify blob path in database and blob exists in storage

**Issue**: "OCR timeout"  
**Solution**: Increase function timeout, check Computer Vision quotas

**Issue**: "OpenAI evaluation failed"  
**Solution**: Check API key, deployment name, and rate limits

### Logs Location
- Azure Functions logs: Application Insights
- Blob operations: Storage account diagnostic logs
- Database queries: SQL Database query performance insights

---

## 📚 Related Documentation
- [Mobile App Answer Sheet Upload Guide](./MOBILE-ANSWER-SHEET-UPLOAD-GUIDE.md)
- [Answer Sheet Evaluation Flow](./ANSWER-SHEET-UPLOAD-FLOW.md)
- [Database Schema](./DATABASE_SETUP_README.md)
- [Azure Functions Documentation](https://learn.microsoft.com/azure/azure-functions/)
- [Azure Computer Vision](https://learn.microsoft.com/azure/cognitive-services/computer-vision/)
- [Azure OpenAI](https://learn.microsoft.com/azure/cognitive-services/openai/)
