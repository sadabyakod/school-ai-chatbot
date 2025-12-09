# Smart Study Exam System - Complete API Reference

## Overview

This document describes the complete exam flow for the Smart Study system, which supports:
1. **AI-generated exams** (Karnataka 2nd PUC format)
2. **MCQ submissions** with instant evaluation
3. **Written answer uploads** with OCR and AI step-by-step evaluation
4. **Consolidated results** showing complete performance analysis

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        EXAM WORKFLOW                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  1. Generate Exam (AI)                                          │
│     POST /api/exam/generate                                     │
│     └─> Returns exam JSON (MCQ + Subjective questions)         │
│                                                                  │
│  2a. Submit MCQ Answers                                         │
│      POST /api/exam/submit-mcq                                  │
│      └─> Instant evaluation & score                            │
│                                                                  │
│  2b. Upload Written Answers (Subjective)                        │
│      POST /api/exam/upload-written                              │
│      └─> Files saved, OCR + AI evaluation triggered            │
│          │                                                       │
│          ├─> OCR extraction                                     │
│          ├─> Split answers by question                          │
│          ├─> AI step-by-step evaluation                        │
│          └─> Save evaluation results                            │
│                                                                  │
│  3. Get Consolidated Result                                     │
│     GET /api/exam/result/{examId}/{studentId}                   │
│     └─> Complete report:                                        │
│         - MCQ score                                             │
│         - Subjective score (with step analysis)                 │
│         - Grand total, percentage, grade, pass/fail             │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Endpoints

### 1. POST /api/exam/generate
**Purpose**: Generate AI-powered exam questions (already implemented)

**Request**:
```json
{
  "subject": "Mathematics",
  "grade": "2nd PUC",
  "chapter": "Matrices and Determinants",
  "difficulty": "Medium",
  "examType": "Full Paper"
}
```

**Response**:
```json
{
  "examId": "EXAM-2024-001",
  "subject": "Mathematics",
  "grade": "2nd PUC",
  "chapter": "Matrices and Determinants",
  "totalMarks": 80,
  "duration": 195,
  "parts": [
    {
      "partName": "Part A",
      "questionType": "MCQ",
      "totalQuestions": 10,
      "marksPerQuestion": 1,
      "questions": [
        {
          "questionId": "A1",
          "questionNumber": 1,
          "questionText": "What is a matrix?",
          "options": ["A) Array", "B) Function", "C) Number", "D) Set"],
          "correctAnswer": "A"
        }
      ]
    },
    {
      "partName": "Part B",
      "questionType": "Short Answer",
      "totalQuestions": 10,
      "marksPerQuestion": 2,
      "questions": [
        {
          "questionId": "B1",
          "questionNumber": 11,
          "questionText": "Find the determinant of [[1,2],[3,4]]",
          "correctAnswer": "det = (1×4) - (2×3) = -2"
        }
      ]
    }
  ]
}
```

---

### 2. POST /api/exam/submit-mcq
**Purpose**: Submit MCQ answers for instant evaluation

**Request**:
```json
{
  "examId": "EXAM-2024-001",
  "studentId": "STU-12345",
  "answers": [
    {
      "questionId": "A1",
      "selectedOption": "A"
    },
    {
      "questionId": "A2",
      "selectedOption": "B"
    }
  ]
}
```

**Response**:
```json
{
  "mcqSubmissionId": "MCQ-SUB-001",
  "score": 8,
  "totalMarks": 10,
  "percentage": 80.0,
  "results": [
    {
      "questionId": "A1",
      "selectedOption": "A",
      "correctAnswer": "A",
      "isCorrect": true,
      "marksAwarded": 1
    },
    {
      "questionId": "A2",
      "selectedOption": "B",
      "correctAnswer": "C",
      "isCorrect": false,
      "marksAwarded": 0
    }
  ]
}
```

---

### 3. POST /api/exam/upload-written
**Purpose**: Upload scanned written answers for subjective questions

**Content-Type**: `multipart/form-data`

**Request Parameters**:
- `examId` (string): The exam ID
- `studentId` (string): The student ID
- `files` (file[]): One or more image files (JPEG, PNG, PDF)

**Example using curl**:
```bash
curl -X POST "http://localhost:8080/api/exam/upload-written" \
  -F "examId=EXAM-2024-001" \
  -F "studentId=STU-12345" \
  -F "files=@answer-page1.jpg" \
  -F "files=@answer-page2.jpg"
```

**Response**:
```json
{
  "writtenSubmissionId": "WS-001",
  "status": "PendingEvaluation",
  "message": "Written answers uploaded successfully. Evaluation in progress."
}
```

**Processing Flow** (automatic, background):
1. Files saved to disk/cloud storage
2. Status → `OcrProcessing`
3. OCR extracts text from images
4. Status → `Evaluating`
5. AI evaluates each subjective answer using step-by-step analysis
6. Evaluation results saved
7. Status → `Completed`

---

### 4. GET /api/exam/result/{examId}/{studentId}
**Purpose**: Get consolidated exam results with complete breakdown

**Request**: `GET /api/exam/result/EXAM-2024-001/STU-12345`

**Response**:
```json
{
  "examId": "EXAM-2024-001",
  "studentId": "STU-12345",
  "examTitle": "Mathematics - Matrices and Determinants",
  
  "mcqScore": 8,
  "mcqTotalMarks": 10,
  "mcqResults": [
    {
      "questionId": "A1",
      "selectedOption": "A",
      "correctAnswer": "A",
      "isCorrect": true,
      "marksAwarded": 1
    }
  ],
  
  "subjectiveScore": 32.5,
  "subjectiveTotalMarks": 40,
  "subjectiveResults": [
    {
      "questionId": "B1",
      "questionNumber": 11,
      "questionText": "Find the determinant of [[1,2],[3,4]]",
      "earnedMarks": 1.5,
      "maxMarks": 2,
      "isFullyCorrect": false,
      "expectedAnswer": "det = (1×4) - (2×3) = -2",
      "studentAnswerEcho": "det = 1*4 - 2*3 = -2",
      "stepAnalysis": [
        {
          "step": 1,
          "description": "Formula application: det = ad - bc",
          "isCorrect": true,
          "marksAwarded": 0.5,
          "maxMarksForStep": 0.5,
          "feedback": "Correct formula used"
        },
        {
          "step": 2,
          "description": "Calculation: (1×4) - (2×3)",
          "isCorrect": true,
          "marksAwarded": 0.5,
          "maxMarksForStep": 0.5,
          "feedback": "Correct calculation"
        },
        {
          "step": 3,
          "description": "Final answer presentation",
          "isCorrect": true,
          "marksAwarded": 0.5,
          "maxMarksForStep": 1.0,
          "feedback": "Answer correct but could show more work"
        }
      ],
      "overallFeedback": "Good work! You got the correct answer. Consider showing intermediate steps more clearly for full marks."
    }
  ],
  
  "grandScore": 40.5,
  "grandTotalMarks": 50,
  "percentage": 81.0,
  "grade": "A",
  "passed": true,
  "evaluatedAt": "2024-12-07 14:30:00"
}
```

---

## AI Evaluation System

### System Prompt (used for subjective evaluation)

```text
You are an experienced Karnataka State Board mathematics examiner.
Evaluate ONE student's SUBJECTIVE answer to ONE question step by step and award partial marks.

You will be given:
- The question text
- The model correct answer or solution idea
- The maximum marks (maxMarks)
- The student's answer (may contain OCR/spelling issues)

You MUST:
1) Break the student's work into 2–5 logical steps and judge each.
2) Award marks for correct steps, even if final answer is wrong.
3) Provide a full expected correct answer.
4) Provide clear feedback for each step and overall.

Output JSON only with this schema:
{
  "earnedMarks": number,
  "maxMarks": number,
  "isFullyCorrect": boolean,
  "expectedAnswer": "string",
  "studentAnswerEcho": "string",
  "stepAnalysis": [
    {
      "step": number,
      "description": "string",
      "isCorrect": boolean,
      "marksAwarded": number,
      "maxMarksForStep": number,
      "feedback": "string"
    }
  ],
  "overallFeedback": "string"
}

Rules:
- earnedMarks MUST be between 0 and maxMarks.
- Sum of marksAwarded in stepAnalysis MUST equal earnedMarks.
- expectedAnswer MUST always contain the full correct final answer.
- overallFeedback MUST clearly mention the expected final answer if the student's answer is not fully correct.
- Return ONLY JSON, no extra text.
```

---

## Data Models

### WrittenSubmission
```csharp
public class WrittenSubmission
{
    public string WrittenSubmissionId { get; set; }
    public string ExamId { get; set; }
    public string StudentId { get; set; }
    public List<string> FilePaths { get; set; }
    public SubmissionStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? EvaluatedAt { get; set; }
    public string? OcrText { get; set; }
}

public enum SubmissionStatus
{
    PendingEvaluation,
    OcrProcessing,
    Evaluating,
    Completed,
    Failed
}
```

### SubjectiveEvaluationResult
```csharp
public class SubjectiveEvaluationResult
{
    public string EvaluationId { get; set; }
    public string WrittenSubmissionId { get; set; }
    public string QuestionId { get; set; }
    public int QuestionNumber { get; set; }
    public double EarnedMarks { get; set; }
    public double MaxMarks { get; set; }
    public bool IsFullyCorrect { get; set; }
    public string ExpectedAnswer { get; set; }
    public string StudentAnswerEcho { get; set; }
    public List<StepAnalysis> StepAnalysis { get; set; }
    public string OverallFeedback { get; set; }
    public DateTime EvaluatedAt { get; set; }
}

public class StepAnalysis
{
    public int Step { get; set; }
    public string Description { get; set; }
    public bool IsCorrect { get; set; }
    public double MarksAwarded { get; set; }
    public double MaxMarksForStep { get; set; }
    public string Feedback { get; set; }
}
```

---

## Service Architecture

### Core Services

1. **IExamRepository**
   - Manages exam submissions (MCQ and written)
   - Stores evaluation results
   - In-memory implementation provided (replace with SQL/MongoDB)

2. **IFileStorageService**
   - Saves uploaded files to disk or Azure Blob Storage
   - Returns file paths for OCR processing

3. **IOcrService**
   - Extracts text from uploaded images/PDFs
   - Placeholder implementation (integrate Azure Computer Vision)

4. **ISubjectiveEvaluator**
   - Splits OCR text into per-question answers
   - Calls AI for step-by-step evaluation
   - Returns list of evaluation results

---

## Testing with Swagger

1. **Start the server**:
   ```bash
   cd SchoolAiChatbotBackend
   dotnet run
   ```

2. **Open Swagger UI**: http://localhost:8080/swagger

3. **Test flow**:
   - Generate exam: `POST /api/exam/generate`
   - Store exam for testing: `POST /api/exam/store-exam` (helper endpoint)
   - Submit MCQ: `POST /api/exam/submit-mcq`
   - Upload written: `POST /api/exam/upload-written`
   - Get result: `GET /api/exam/result/{examId}/{studentId}`

---

## Testing with PowerShell

```powershell
# 1. Generate exam
$examBody = @{
    subject = "Mathematics"
    grade = "2nd PUC"
    chapter = "Matrices"
    difficulty = "Medium"
    examType = "Full Paper"
} | ConvertTo-Json

$exam = Invoke-RestMethod -Uri "http://localhost:8080/api/exam/generate" `
    -Method POST -Body $examBody -ContentType "application/json"

$examId = $exam.examId
Write-Host "Generated exam: $examId"

# 2. Submit MCQ answers
$mcqBody = @{
    examId = $examId
    studentId = "STU-001"
    answers = @(
        @{ questionId = "A1"; selectedOption = "A" },
        @{ questionId = "A2"; selectedOption = "B" }
    )
} | ConvertTo-Json -Depth 5

$mcqResult = Invoke-RestMethod -Uri "http://localhost:8080/api/exam/submit-mcq" `
    -Method POST -Body $mcqBody -ContentType "application/json"

Write-Host "MCQ Score: $($mcqResult.score)/$($mcqResult.totalMarks)"

# 3. Upload written answers (requires multipart/form-data - use Swagger instead)

# 4. Get consolidated result
$result = Invoke-RestMethod -Uri "http://localhost:8080/api/exam/result/$examId/STU-001" `
    -Method GET

Write-Host "Grand Score: $($result.grandScore)/$($result.grandTotalMarks)"
Write-Host "Percentage: $($result.percentage)%"
Write-Host "Grade: $($result.grade)"
```

---

## Integration Points

### TODO: Azure Computer Vision OCR
```csharp
// In OcrService.cs
var client = new ComputerVisionClient(
    new ApiKeyServiceClientCredentials(apiKey))
    { Endpoint = endpoint };

using var stream = File.OpenRead(imagePath);
var result = await client.RecognizePrintedTextInStreamAsync(true, stream);

var text = new StringBuilder();
foreach (var region in result.Regions)
{
    foreach (var line in region.Lines)
    {
        text.AppendLine(string.Join(" ", line.Words.Select(w => w.Text)));
    }
}
return text.ToString();
```

### TODO: Azure Blob Storage
```csharp
// In FileStorageService.cs
public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    
    public async Task<string> SaveFileAsync(IFormFile file, string examId, string studentId)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("written-answers");
        await containerClient.CreateIfNotExistsAsync();
        
        var blobName = $"{examId}/{studentId}/{Guid.NewGuid()}-{file.FileName}";
        var blobClient = containerClient.GetBlobClient(blobName);
        
        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, true);
        
        return blobClient.Uri.ToString();
    }
}
```

---

## Production Considerations

1. **Database**: Replace `InMemoryExamRepository` with SQL Server/PostgreSQL
2. **File Storage**: Replace `LocalFileStorageService` with Azure Blob Storage
3. **OCR**: Integrate Azure Computer Vision or similar service
4. **Queue Processing**: Use Azure Service Bus for async evaluation jobs
5. **Authentication**: Add JWT authentication to all endpoints
6. **Rate Limiting**: Implement to prevent abuse
7. **Caching**: Cache exam data to reduce database load
8. **Monitoring**: Add Application Insights for production telemetry

---

## Error Handling

All endpoints return standard error responses:

```json
{
  "error": "Error message description"
}
```

HTTP Status Codes:
- `200 OK`: Success
- `400 Bad Request`: Invalid input
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error

---

## Questions?

For implementation details, see:
- `Controllers/ExamSubmissionController.cs`
- `Services/SubjectiveEvaluator.cs`
- `Services/OcrService.cs`
- `Services/InMemoryExamRepository.cs`
