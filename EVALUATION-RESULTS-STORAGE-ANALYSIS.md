# Evaluation Results Storage - Backend API Reference (Updated for Current Implementation)

## Overview
This document provides backend implementation reference for the **Permanent Evaluation Results Storage** feature. This guide analyzes the current backend API and provides recommendations for Azure Blob Storage integration.

---

## 🔍 Current Implementation Analysis

### ✅ Existing API Endpoints (Working)

#### 1. Upload Answer Sheet
```
POST /api/exam/upload-written
Content-Type: multipart/form-data
```
**Status:** ✅ Implemented in `ExamSubmissionController.cs`

#### 2. Check Submission Status
```
GET /api/exam/submission-status/{writtenSubmissionId}
```
**Status:** ✅ Implemented in `ExamSubmissionController.cs`

#### 3. Get Exam Results
```
GET /api/exam/result/{examId}/{studentId}
```
**Status:** ✅ Implemented in `ExamSubmissionController.cs`

---

## ⚠️ Required Changes for Azure Blob Storage Integration

### 1. Add Missing Database Column

The `WrittenSubmission` model is **missing** the `EvaluationResultBlobPath` column needed for permanent storage.

**Current Model:** (from `Models/WrittenSubmission.cs`)
```csharp
public class WrittenSubmission
{
    // ... existing properties ...
    
    [MaxLength(500)]
    public string? ExtractedTextBlobPath { get; set; }
    
    // ❌ MISSING: EvaluationResultBlobPath property
}
```

**Required Addition:**
```csharp
/// <summary>
/// Blob path to the evaluation result JSON file
/// Format: evaluation-results/{ExamId}/{SubmissionId}/evaluation-result.json
/// </summary>
[MaxLength(500)]
[Column("EvaluationResultBlobPath")]
public string? EvaluationResultBlobPath { get; set; }
```

**Migration SQL:**
```sql
-- Add EvaluationResultBlobPath column to WrittenSubmissions table
ALTER TABLE WrittenSubmissions
ADD EvaluationResultBlobPath NVARCHAR(500) NULL;

-- Create index for fast retrieval
CREATE NONCLUSTERED INDEX IX_WrittenSubmissions_EvaluationResultBlobPath
ON WrittenSubmissions(EvaluationResultBlobPath)
WHERE EvaluationResultBlobPath IS NOT NULL;

-- Verify column was added
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'WrittenSubmissions'
AND COLUMN_NAME = 'EvaluationResultBlobPath';
```

---

### 2. Update SubmissionStatusResponse DTO

**Current DTO:** (from `DTOs/ExamSubmissionDTOs.cs`)
```csharp
public class SubmissionStatusResponse
{
    public string WrittenSubmissionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public DateTime? EvaluatedAt { get; set; }
    public bool IsComplete { get; set; }
    public string ExamId { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public ConsolidatedExamResult? Result { get; set; }
    
    // ❌ MISSING: EvaluationResultBlobPath property
}
```

**Required Addition:**
```csharp
/// <summary>
/// Blob path to evaluation result JSON (populated when evaluation is complete)
/// </summary>
public string? EvaluationResultBlobPath { get; set; }
```

---

### 3. Update GetSubmissionStatus Controller Method

**Current Implementation:** (from `ExamSubmissionController.cs` line 357)
```csharp
[HttpGet("submission-status/{writtenSubmissionId}")]
public async Task<ActionResult<SubmissionStatusResponse>> GetSubmissionStatus(string writtenSubmissionId)
{
    var submission = await _examRepository.GetWrittenSubmissionAsync(writtenSubmissionId);
    if (submission == null)
    {
        return NotFound(new { error = "Submission not found" });
    }

    var response = new SubmissionStatusResponse
    {
        WrittenSubmissionId = writtenSubmissionId,
        Status = submission.Status.ToString(),
        StatusMessage = statusMessage,
        SubmittedAt = submission.SubmittedAt,
        EvaluatedAt = submission.EvaluatedAt,
        IsComplete = submission.Status == SubmissionStatus.Completed,
        ExamId = submission.ExamId,
        StudentId = submission.StudentId
        // ❌ MISSING: EvaluationResultBlobPath assignment
    };

    return Ok(response);
}
```

**Required Change:**
```csharp
var response = new SubmissionStatusResponse
{
    WrittenSubmissionId = writtenSubmissionId,
    Status = submission.Status.ToString(),
    StatusMessage = statusMessage,
    SubmittedAt = submission.SubmittedAt,
    EvaluatedAt = submission.EvaluatedAt,
    IsComplete = submission.Status == SubmissionStatus.Completed,
    ExamId = submission.ExamId,
    StudentId = submission.StudentId,
    EvaluationResultBlobPath = submission.EvaluationResultBlobPath // ✅ ADD THIS
};
```

---

### 4. Add New API Endpoint: GetEvaluationResultFromBlob

**Missing Endpoint:** `GET /api/exam/evaluation-result/{submissionId}`

This endpoint should fetch the complete evaluation result JSON from Azure Blob Storage instead of reconstructing it from database tables.

**Implementation to Add:**
```csharp
/// <summary>
/// Get evaluation result JSON from Blob Storage
/// GET /api/exam/evaluation-result/{submissionId}
/// </summary>
[HttpGet("evaluation-result/{submissionId}")]
[ProducesResponseType(typeof(WrittenEvaluationResult), 200)]
[ProducesResponseType(404)]
[ProducesResponseType(202)] // Accepted - evaluation in progress
public async Task<IActionResult> GetEvaluationResultFromBlob(string submissionId)
{
    try
    {
        _logger.LogInformation("[RESULT_FETCH] SubmissionId={Id}", submissionId);

        // 1. Get submission from database
        var submission = await _examRepository.GetWrittenSubmissionAsync(submissionId);
        
        if (submission == null)
        {
            return NotFound(new { error = "Submission not found" });
        }

        // 2. Check if evaluation is complete
        if (submission.Status != SubmissionStatus.Completed)
        {
            return StatusCode(202, new 
            { 
                message = "Evaluation still in progress",
                status = submission.Status.ToString(),
                submissionId = submissionId
            });
        }

        // 3. Check if blob path exists
        if (string.IsNullOrEmpty(submission.EvaluationResultBlobPath))
        {
            _logger.LogWarning("[BLOB_PATH_MISSING] SubmissionId={Id}", submissionId);
            return NotFound(new { error = "Evaluation result not available in storage" });
        }

        // 4. Download from Blob Storage
        var parts = submission.EvaluationResultBlobPath.Split('/', 2);
        if (parts.Length != 2)
        {
            _logger.LogError("[INVALID_BLOB_PATH] Path={Path}", submission.EvaluationResultBlobPath);
            return StatusCode(500, new { error = "Invalid blob path format" });
        }

        var containerName = parts[0]; // e.g., "evaluation-results"
        var blobPath = parts[1];      // e.g., "EXAM-001/submission-id/evaluation-result.json"

        try
        {
            var blobServiceClient = new BlobServiceClient(_configuration["AzureBlobStorage:ConnectionString"]);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobPath);

            // Check if blob exists
            if (!await blobClient.ExistsAsync())
            {
                _logger.LogWarning("[BLOB_NOT_FOUND] Path={Path}", submission.EvaluationResultBlobPath);
                return NotFound(new { error = "Evaluation result file not found in storage" });
            }

            // Download and deserialize JSON
            var response = await blobClient.DownloadContentAsync();
            var json = response.Value.Content.ToString();
            var evaluationResult = System.Text.Json.JsonSerializer.Deserialize<WrittenEvaluationResult>(
                json,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            _logger.LogInformation("[RESULT_RETRIEVED] SubmissionId={Id}, Score={Score}/{Max}",
                submissionId, evaluationResult?.TotalScore, evaluationResult?.MaxPossibleScore);

            return Ok(evaluationResult);
        }
        catch (Azure.RequestFailedException blobEx)
        {
            _logger.LogError(blobEx, "[BLOB_DOWNLOAD_FAILED] SubmissionId={Id}", submissionId);
            return StatusCode(500, new { error = "Failed to retrieve evaluation result from storage" });
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[RESULT_FETCH_FAILED] SubmissionId={Id}", submissionId);
        return StatusCode(500, new { error = "Internal server error" });
    }
}
```

**Required NuGet Package:**
```bash
dotnet add package Azure.Storage.Blobs
```

**Required Configuration (appsettings.json):**
```json
{
  "AzureBlobStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=stsmartstudydev;AccountKey=xxx;EndpointSuffix=core.windows.net",
    "EvaluationResultsContainer": "evaluation-results"
  }
}
```

---

### 5. Add DTO for WrittenEvaluationResult

**Required New DTO:** (Add to `DTOs/ExamSubmissionDTOs.cs`)
```csharp
/// <summary>
/// Complete evaluation result stored in Azure Blob Storage
/// Matches the JSON structure saved by Azure Functions
/// </summary>
public class WrittenEvaluationResult
{
    public string WrittenSubmissionId { get; set; } = string.Empty;
    public string ExamId { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public decimal TotalScore { get; set; }
    public decimal MaxPossibleScore { get; set; }
    public decimal Percentage { get; set; }
    public string Grade { get; set; } = string.Empty;
    public DateTime EvaluatedAt { get; set; }
    public List<QuestionEvaluationDto> QuestionEvaluations { get; set; } = new();
}

/// <summary>
/// Individual question evaluation with step-wise marking
/// </summary>
public class QuestionEvaluationDto
{
    public string Id { get; set; } = string.Empty;
    public string WrittenSubmissionId { get; set; } = string.Empty;
    public string QuestionId { get; set; } = string.Empty;
    public int QuestionNumber { get; set; }
    public string ExtractedAnswer { get; set; } = string.Empty;
    public string ModelAnswer { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public decimal AwardedScore { get; set; }
    public string Feedback { get; set; } = string.Empty;
    
    /// <summary>
    /// Step-wise rubric breakdown showing marks per step
    /// Format: "Step 1: Description (2/3) ✓\nStep 2: Description (0/4) ✗"
    /// </summary>
    public string RubricBreakdown { get; set; } = string.Empty;
    public DateTime EvaluatedAt { get; set; }
}
```

---

## 📁 Updated Storage Architecture

### Blob Storage Structure
```
smartstudy-storage/
├── answer-sheets/                         (Uploaded answer images)
│   ├── Karnataka_2nd_PUC_Math_2024_25/
│   │   └── submission-id/
│   │       ├── page-1.jpg
│   │       ├── page-2.jpg
│   │       └── page-3.jpg
│
└── evaluation-results/                    (Evaluation JSON results) ⭐ NEW
    ├── Karnataka_2nd_PUC_Math_2024_25/
    │   └── submission-id/
    │       └── evaluation-result.json     ⭐ Permanent result storage
    └── SAMPLE-EXAM-001/
        └── submission-id/
            └── evaluation-result.json
```

### Database Schema
```sql
CREATE TABLE WrittenSubmissions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ExamId NVARCHAR(200) NOT NULL,
    StudentId NVARCHAR(200) NOT NULL,
    
    -- Status tracking
    Status INT NOT NULL,
    
    -- Timestamps
    SubmittedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    OcrStartedAt DATETIME2,
    OcrCompletedAt DATETIME2,
    EvaluationStartedAt DATETIME2,
    EvaluatedAt DATETIME2,
    
    -- Scores (summary data)
    TotalScore DECIMAL(10,2),
    MaxPossibleScore DECIMAL(10,2),
    Percentage DECIMAL(5,2),
    Grade NVARCHAR(10),
    
    -- Blob Storage Paths
    FilePaths NVARCHAR(MAX),                      -- Uploaded answer sheets
    ExtractedTextBlobPath NVARCHAR(500),          -- OCR text
    EvaluationResultBlobPath NVARCHAR(500),       -- ⭐ NEW: Complete evaluation JSON
    
    -- Performance metrics
    OcrProcessingTimeMs BIGINT,
    EvaluationProcessingTimeMs BIGINT,
    
    -- Error handling
    ErrorMessage NVARCHAR(MAX),
    RetryCount INT DEFAULT 0,
    
    CONSTRAINT FK_WrittenSubmissions_Exams FOREIGN KEY (ExamId) REFERENCES Exams(Id)
);
```

---

## 📄 JSON Response Format

### Evaluation Result JSON (Stored in Blob)
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
    }
  ]
}
```

---

## 🔧 Implementation Checklist

### Backend Changes Required

- [ ] **1. Database Migration**
  ```bash
  # Run SQL migration to add EvaluationResultBlobPath column
  sqlcmd -S your-server.database.windows.net -d SmartStudyDB -U sqladmin -i add-evaluation-blob-path.sql
  ```

- [ ] **2. Update WrittenSubmission Model**
  - Add `EvaluationResultBlobPath` property to `Models/WrittenSubmission.cs`

- [ ] **3. Update SubmissionStatusResponse DTO**
  - Add `EvaluationResultBlobPath` property to `DTOs/ExamSubmissionDTOs.cs`

- [ ] **4. Add WrittenEvaluationResult DTO**
  - Create new DTOs for blob-stored evaluation results

- [ ] **5. Update GetSubmissionStatus Method**
  - Include `EvaluationResultBlobPath` in response (line 357-416 in ExamSubmissionController.cs)

- [ ] **6. Add GetEvaluationResultFromBlob Endpoint**
  - New endpoint: `GET /api/exam/evaluation-result/{submissionId}`
  - Downloads evaluation JSON from Azure Blob Storage

- [ ] **7. Install Azure.Storage.Blobs Package**
  ```bash
  cd SchoolAiChatbotBackend
  dotnet add package Azure.Storage.Blobs --version 12.19.1
  ```

- [ ] **8. Update appsettings.json**
  - Add Azure Blob Storage connection string configuration

- [ ] **9. Update Repository Interface**
  - Add method to update `EvaluationResultBlobPath` after Azure Function completes evaluation

### Azure Functions Changes Required

- [ ] **10. Update ProcessWrittenSubmission Function**
  - Save evaluation result JSON to blob after AI evaluation
  - Update database with blob path

- [ ] **11. Update Repository in Azure Functions**
  - Add code to save blob path to `WrittenSubmissions.EvaluationResultBlobPath`

---

## 🌐 Updated API Endpoints

### 1. Upload Answer Sheet ✅
```
POST /api/exam/upload-written
```
**Status:** Already implemented

### 2. Check Submission Status (Needs Update)
```
GET /api/exam/submission-status/{writtenSubmissionId}
```
**Current Response:**
```json
{
  "writtenSubmissionId": "...",
  "status": "Completed",
  "statusMessage": "✅ Evaluation completed! Your results are ready.",
  "submittedAt": "2025-12-15T10:00:00Z",
  "evaluatedAt": "2025-12-15T11:30:00Z",
  "isComplete": true,
  "examId": "Karnataka_2nd_PUC_Math_2024_25",
  "studentId": "STUDENT-12345",
  "result": { /* full consolidated result */ }
}
```

**Required Update - Add:**
```json
{
  "evaluationResultBlobPath": "evaluation-results/Karnataka_2nd_PUC_Math_2024_25/submission-id/evaluation-result.json"
}
```

### 3. Get Consolidated Result ✅
```
GET /api/exam/result/{examId}/{studentId}
```
**Status:** Already implemented (reconstructs from database)

### 4. Get Evaluation Result from Blob ⭐ NEW
```
GET /api/exam/evaluation-result/{submissionId}
```
**Purpose:** Fetch pre-saved evaluation JSON from Azure Blob Storage

**Response:**
```json
{
  "writtenSubmissionId": "...",
  "examId": "...",
  "studentId": "...",
  "totalScore": 28.5,
  "maxPossibleScore": 40.0,
  "percentage": 71.25,
  "grade": "B+",
  "evaluatedAt": "2025-12-15T11:30:00.123Z",
  "questionEvaluations": [...]
}
```

---

## 🧪 Testing After Implementation

### 1. Test Complete Flow
```powershell
# Step 1: Upload answer sheet
$examId = "Karnataka_2nd_PUC_Math_2024_25"
$studentId = "STUDENT-TEST-001"
$files = @(Get-Item "C:\test\answer1.jpg", "C:\test\answer2.jpg")

$uploadResult = Invoke-RestMethod `
    -Uri "http://localhost:8080/api/exam/upload-written" `
    -Method POST `
    -Form @{
        examId = $examId
        studentId = $studentId
        files = $files
    }

$submissionId = $uploadResult.writtenSubmissionId
Write-Host "✅ Uploaded. Submission ID: $submissionId"

# Step 2: Poll status (wait for evaluation to complete)
do {
    Start-Sleep -Seconds 3
    $status = Invoke-RestMethod `
        -Uri "http://localhost:8080/api/exam/submission-status/$submissionId"
    
    Write-Host "Status: $($status.status) - $($status.statusMessage)"
    
    if ($status.isComplete) {
        Write-Host "✅ Evaluation complete!"
        Write-Host "Blob Path: $($status.evaluationResultBlobPath)"
        break
    }
} while ($true)

# Step 3: Fetch evaluation result from blob
$evaluation = Invoke-RestMethod `
    -Uri "http://localhost:8080/api/exam/evaluation-result/$submissionId"

Write-Host "=== EVALUATION RESULTS FROM BLOB ==="
Write-Host "Total Score: $($evaluation.totalScore) / $($evaluation.maxPossibleScore)"
Write-Host "Percentage: $($evaluation.percentage)%"
Write-Host "Grade: $($evaluation.grade)"

foreach ($q in $evaluation.questionEvaluations) {
    Write-Host "`nQuestion $($q.questionNumber): $($q.awardedScore)/$($q.maxScore) marks"
    Write-Host "Feedback: $($q.feedback)"
    Write-Host "Rubric Breakdown:"
    Write-Host $q.rubricBreakdown
}
```

### 2. Verify Database
```sql
-- Check if blob path is saved
SELECT 
    Id,
    ExamId,
    StudentId,
    Status,
    TotalScore,
    Grade,
    EvaluationResultBlobPath,
    EvaluatedAt
FROM WrittenSubmissions
WHERE StudentId = 'STUDENT-TEST-001'
ORDER BY SubmittedAt DESC;
```

### 3. Verify Blob Storage
```powershell
# Check if evaluation result exists in blob
az storage blob list `
    --account-name stsmartstudydev `
    --container-name evaluation-results `
    --prefix "Karnataka_2nd_PUC_Math_2024_25/$submissionId/" `
    --auth-mode login

# Download and view JSON
az storage blob download `
    --account-name stsmartstudydev `
    --container-name evaluation-results `
    --name "Karnataka_2nd_PUC_Math_2024_25/$submissionId/evaluation-result.json" `
    --file "./result.json" `
    --auth-mode login

Get-Content "./result.json" | ConvertFrom-Json | ConvertTo-Json -Depth 10
```

---

## 🔒 Security Considerations

### 1. Authentication & Authorization
- Validate student owns the submission before returning results
- Implement JWT token authentication
- Add rate limiting to prevent abuse

### 2. Example: Secure Endpoint
```csharp
[HttpGet("evaluation-result/{submissionId}")]
[Authorize] // Require authentication
public async Task<IActionResult> GetEvaluationResultFromBlob(string submissionId)
{
    // 1. Get authenticated student ID from claims
    var authenticatedStudentId = User.FindFirst("studentId")?.Value;
    if (string.IsNullOrEmpty(authenticatedStudentId))
    {
        return Unauthorized(new { error = "Authentication required" });
    }

    // 2. Get submission
    var submission = await _examRepository.GetWrittenSubmissionAsync(submissionId);
    
    if (submission == null)
    {
        return NotFound(new { error = "Submission not found" });
    }

    // 3. Verify ownership
    if (submission.StudentId != authenticatedStudentId)
    {
        _logger.LogWarning(
            "[UNAUTHORIZED_ACCESS_ATTEMPT] Student {RequestingStudent} tried to access submission for {OwnerStudent}",
            authenticatedStudentId,
            submission.StudentId);
        
        return Forbid(); // 403 Forbidden
    }

    // 4. Return result
    // ... rest of implementation
}
```

---

## 📊 Monitoring

### Key Metrics to Log
```csharp
// Log when blob path is saved
_logger.LogInformation(
    "[BLOB_PATH_SAVED] SubmissionId={Id}, BlobPath={Path}",
    submissionId, evaluationResultBlobPath);

// Log result retrieval
_logger.LogInformation(
    "[RESULT_RETRIEVED_FROM_BLOB] SubmissionId={Id}, StudentId={StudentId}, ResponseTimeMs={Time}",
    submissionId, studentId, stopwatch.ElapsedMilliseconds);

// Log missing blob path
_logger.LogWarning(
    "[BLOB_PATH_MISSING] SubmissionId={Id}, Status={Status}",
    submissionId, submission.Status);
```

---

## 📞 Support

### Common Issues

**Issue:** "EvaluationResultBlobPath is null"  
**Solution:** Run database migration to add the column

**Issue:** "Blob not found in storage"  
**Solution:** Check Azure Function logs - evaluation may have failed

**Issue:** "Endpoint 404 - evaluation-result not found"  
**Solution:** Add the new endpoint to ExamSubmissionController.cs

---

## 📚 Related Documentation
- [Azure Functions Answer Evaluation Implementation](./AZURE-FUNCTION-ANSWER-EVALUATION-README.md)
- [Mobile App Answer Sheet Upload Guide](./MOBILE-ANSWER-SHEET-UPLOAD-GUIDE.md)
- [Answer Sheet Upload Flow](./ANSWER-SHEET-UPLOAD-FLOW.md)
- [Database Setup Guide](./DATABASE_SETUP_README.md)
