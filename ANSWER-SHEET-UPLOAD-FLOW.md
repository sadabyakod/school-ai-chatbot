# 📄 Answer Sheet Upload & Evaluation Flow

## Complete Architecture: Student Upload → Azure Functions → Results

---

## 🎯 Overview

When a student uploads their handwritten answer sheet, the system processes it through a **queue-based architecture** where Azure Functions handles the heavy processing (OCR + AI Evaluation).

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Mobile App /   │────▶│  ASP.NET Core   │────▶│  Azure Queue    │────▶│ Azure Functions │
│  Web Frontend   │     │  Backend API    │     │  Storage        │     │  (Processor)    │
└─────────────────┘     └─────────────────┘     └─────────────────┘     └─────────────────┘
                                │                                               │
                                ▼                                               ▼
                        ┌─────────────────┐                           ┌─────────────────┐
                        │  Azure Blob     │                           │  Azure OpenAI   │
                        │  Storage        │                           │  (GPT-4 Vision) │
                        └─────────────────┘                           └─────────────────┘
                                                                              │
                                                                              ▼
                                                                      ┌─────────────────┐
                                                                      │  Azure SQL DB   │
                                                                      │  (Results)      │
                                                                      └─────────────────┘
```

---

## 📱 Step-by-Step Flow

### Step 1: Student Uploads Answer Sheet

**Endpoint:** `POST /api/exam/upload-written`

**Request:**
```http
POST /api/exam/upload-written
Content-Type: multipart/form-data

examId: EXAM-2024-PHYSICS-001
studentId: STU-12345
files: [answer_page1.jpg, answer_page2.jpg, ...]
```

**What happens in Backend (ASP.NET Core):**
1. ✅ Validates examId and studentId
2. ✅ Validates files (type, size, count)
3. ✅ Checks exam exists
4. ✅ Checks for duplicate submissions
5. ✅ Saves files to Azure Blob Storage
6. ✅ Creates submission record with status `PendingEvaluation`
7. ✅ Enqueues message to Azure Queue

**Response:**
```json
{
  "writtenSubmissionId": "sub-abc123",
  "status": "PendingEvaluation",
  "message": "✅ Answer sheet uploaded successfully! Processing will begin shortly."
}
```

---

### Step 2: Queue Message Structure

The backend sends this message to Azure Queue Storage:

**Queue Name:** `written-submission-processing`

**Message Format:**
```json
{
  "writtenSubmissionId": "sub-abc123",
  "examId": "EXAM-2024-PHYSICS-001",
  "studentId": "STU-12345",
  "filePaths": [
    "https://storage.blob.core.windows.net/answer-sheets/exam123/student456/page1.jpg",
    "https://storage.blob.core.windows.net/answer-sheets/exam123/student456/page2.jpg"
  ],
  "submittedAt": "2025-12-14T10:30:00Z",
  "priority": "normal",
  "retryCount": 0
}
```

---

### Step 3: Azure Function Triggers

**Function App Name:** `smartstudy-func`

**Queue Trigger Function:**

```csharp
// ProcessWrittenSubmission.cs
[FunctionName("ProcessWrittenSubmission")]
public async Task Run(
    [QueueTrigger("written-submission-processing")] WrittenSubmissionMessage message,
    ILogger log)
{
    log.LogInformation($"Processing submission: {message.WrittenSubmissionId}");
    
    try
    {
        // Step 1: Update status to OcrProcessing
        await UpdateStatusAsync(message.WrittenSubmissionId, "OcrProcessing");
        
        // Step 2: Download images from Blob Storage
        var images = await DownloadImagesAsync(message.FilePaths);
        
        // Step 3: Extract text using GPT-4 Vision (OCR)
        var extractedAnswers = await ExtractAnswersFromImagesAsync(images);
        
        // Step 4: Update status to Evaluating
        await UpdateStatusAsync(message.WrittenSubmissionId, "Evaluating");
        
        // Step 5: Load exam questions and rubrics
        var exam = await GetExamAsync(message.ExamId);
        
        // Step 6: Evaluate each answer using AI
        var evaluations = await EvaluateAnswersAsync(exam, extractedAnswers);
        
        // Step 7: Save evaluations to database
        await SaveEvaluationsAsync(message.WrittenSubmissionId, evaluations);
        
        // Step 8: Update status to Completed
        await UpdateStatusAsync(message.WrittenSubmissionId, "Completed");
        
        log.LogInformation($"✅ Submission {message.WrittenSubmissionId} evaluated successfully");
    }
    catch (Exception ex)
    {
        await UpdateStatusAsync(message.WrittenSubmissionId, "Failed");
        log.LogError(ex, $"❌ Failed to process submission: {message.WrittenSubmissionId}");
        throw; // Retry mechanism
    }
}
```

---

### Step 4: GPT-4 Vision OCR + Evaluation

**Azure Function calls Azure OpenAI GPT-4 Vision:**

```csharp
private async Task<List<ExtractedAnswer>> ExtractAnswersFromImagesAsync(List<byte[]> images)
{
    var extractedAnswers = new List<ExtractedAnswer>();
    
    foreach (var imageData in images)
    {
        var base64Image = Convert.ToBase64String(imageData);
        
        var prompt = @"
You are an exam answer sheet reader. Extract all handwritten answers from this image.
For each answer found:
1. Identify the question number
2. Extract the complete answer text
3. Note any diagrams or formulas

Return JSON format:
{
  ""answers"": [
    {
      ""questionNumber"": 1,
      ""answerText"": ""...(extracted text)..."",
      ""hasDiagram"": true/false,
      ""confidence"": 0.95
    }
  ]
}";

        var response = await _openAiClient.CreateChatCompletionAsync(
            new ChatCompletionRequest
            {
                Model = "gpt-4-vision-preview",
                Messages = new[]
                {
                    new { role = "system", content = "Extract handwritten text from exam answer sheets." },
                    new { 
                        role = "user", 
                        content = new object[]
                        {
                            new { type = "text", text = prompt },
                            new { 
                                type = "image_url", 
                                image_url = new { 
                                    url = $"data:image/jpeg;base64,{base64Image}",
                                    detail = "high"
                                }
                            }
                        }
                    }
                },
                MaxTokens = 4000
            });
        
        // Parse and add to list
        extractedAnswers.AddRange(ParseExtractedAnswers(response));
    }
    
    return extractedAnswers;
}
```

---

### Step 5: AI Evaluation with Rubric

**For each subjective answer:**

```csharp
private async Task<SubjectiveEvaluation> EvaluateSubjectiveAnswerAsync(
    Question question, 
    string studentAnswer, 
    Rubric rubric)
{
    var prompt = $@"
You are an exam evaluator for Karnataka 2nd PUC board exams.

**Question:** {question.QuestionText}
**Maximum Marks:** {question.MaxMarks}
**Expected Answer:** {question.CorrectAnswer}

**Rubric/Marking Scheme:**
{FormatRubric(rubric)}

**Student's Answer:**
{studentAnswer}

Evaluate step-by-step according to the rubric. Award partial marks fairly.

Return JSON:
{{
  ""earnedMarks"": <number>,
  ""maxMarks"": {question.MaxMarks},
  ""isFullyCorrect"": true/false,
  ""stepAnalysis"": [
    {{
      ""step"": 1,
      ""description"": ""Statement of the law"",
      ""isCorrect"": true,
      ""marksAwarded"": 2,
      ""maxMarksForStep"": 2,
      ""feedback"": ""Correctly stated""
    }}
  ],
  ""overallFeedback"": ""Good understanding. Missing permittivity explanation.""
}}";

    var response = await _openAiClient.CreateChatCompletionAsync(prompt);
    return JsonSerializer.Deserialize<SubjectiveEvaluation>(response);
}
```

---

### Step 6: Save Results to Database

**Azure Function saves to Azure SQL:**

```sql
-- WrittenSubmissions table update
UPDATE WrittenSubmissions 
SET Status = 'Completed', 
    EvaluatedAt = GETUTCDATE(),
    OcrText = @extractedText
WHERE WrittenSubmissionId = @submissionId;

-- SubjectiveEvaluations table insert
INSERT INTO SubjectiveEvaluations (
    EvaluationId, WrittenSubmissionId, QuestionId,
    EarnedMarks, MaxMarks, StudentAnswerEcho,
    StepAnalysisJson, OverallFeedback, EvaluatedAt
) VALUES (
    @evaluationId, @submissionId, @questionId,
    @earnedMarks, @maxMarks, @studentAnswer,
    @stepAnalysisJson, @overallFeedback, GETUTCDATE()
);
```

---

### Step 7: Student Checks Status

**Endpoint:** `GET /api/exam/submission-status/{writtenSubmissionId}`

**Status Progression:**
```
PendingEvaluation → OcrProcessing → Evaluating → Completed
                                              ↘ Failed (on error)
```

**Response (During Processing):**
```json
{
  "writtenSubmissionId": "sub-abc123",
  "status": "Evaluating",
  "statusMessage": "🤖 AI is evaluating your answers...",
  "submittedAt": "2025-12-14T10:30:00Z",
  "isComplete": false
}
```

**Response (After Completion):**
```json
{
  "writtenSubmissionId": "sub-abc123",
  "status": "Completed",
  "statusMessage": "✅ Evaluation completed! Your results are ready.",
  "submittedAt": "2025-12-14T10:30:00Z",
  "evaluatedAt": "2025-12-14T10:32:15Z",
  "isComplete": true,
  "result": {
    "examId": "EXAM-2024-PHYSICS-001",
    "grandScore": 43.5,
    "grandTotalMarks": 60,
    "percentage": 72.5,
    "grade": "B+",
    "passed": true
  }
}
```

---

### Step 8: Get Full Results

**Endpoint:** `GET /api/exam/result/{examId}/{studentId}`

Returns complete MCQ + Subjective results with feedback.

---

## 🔧 Azure Function App Configuration

### Required App Settings:

```json
{
  "AzureWebJobsStorage": "DefaultEndpointsProtocol=https;AccountName=smartstudystorage;...",
  "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
  
  "AzureOpenAI:Endpoint": "https://smartstudy-openai.openai.azure.com/",
  "AzureOpenAI:ApiKey": "your-api-key",
  "AzureOpenAI:ChatDeployment": "gpt-4-vision",
  
  "SqlConnectionString": "Server=tcp:smartstudysqlsrv.database.windows.net;...",
  
  "BlobStorage:ConnectionString": "DefaultEndpointsProtocol=https;...",
  "BlobStorage:AnswerSheetsContainer": "answer-sheets"
}
```

### Queue Configuration:

| Queue Name | Purpose | Visibility Timeout |
|------------|---------|-------------------|
| `written-submission-processing` | Main processing queue | 5 minutes |
| `written-submission-processing-poison` | Failed messages | Auto-created |

---

## 📊 Database Schema

### WrittenSubmissions Table:
```sql
CREATE TABLE WrittenSubmissions (
    WrittenSubmissionId NVARCHAR(50) PRIMARY KEY,
    ExamId NVARCHAR(50) NOT NULL,
    StudentId NVARCHAR(50) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'PendingEvaluation',
    FilePaths NVARCHAR(MAX), -- JSON array of blob URLs
    OcrText NVARCHAR(MAX),   -- Extracted text from images
    SubmittedAt DATETIME2 DEFAULT GETUTCDATE(),
    EvaluatedAt DATETIME2 NULL,
    ErrorMessage NVARCHAR(MAX) NULL
);
```

### SubjectiveEvaluations Table:
```sql
CREATE TABLE SubjectiveEvaluations (
    EvaluationId NVARCHAR(50) PRIMARY KEY,
    WrittenSubmissionId NVARCHAR(50) FOREIGN KEY,
    QuestionId NVARCHAR(50) NOT NULL,
    QuestionNumber INT,
    EarnedMarks DECIMAL(5,2),
    MaxMarks DECIMAL(5,2),
    IsFullyCorrect BIT,
    StudentAnswerEcho NVARCHAR(MAX),
    ExpectedAnswer NVARCHAR(MAX),
    StepAnalysisJson NVARCHAR(MAX), -- JSON array
    OverallFeedback NVARCHAR(MAX),
    EvaluatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

---

## 🔄 Retry & Error Handling

### Automatic Retries:
- Azure Queue automatically retries failed messages
- Default: 5 retries with exponential backoff
- After 5 failures → moves to poison queue

### Error States:
| Status | Meaning | Action |
|--------|---------|--------|
| `PendingEvaluation` | Queued, not started | Wait |
| `OcrProcessing` | Reading handwriting | Wait |
| `Evaluating` | AI grading in progress | Wait |
| `Completed` | Done, results ready | Fetch results |
| `Failed` | Error occurred | Contact support |

---

## ⏱️ Expected Processing Times

| Image Count | Typical Time | Max Time |
|-------------|--------------|----------|
| 1-2 pages | 15-30 seconds | 1 minute |
| 3-5 pages | 30-60 seconds | 2 minutes |
| 6-10 pages | 1-2 minutes | 5 minutes |
| 10+ pages | 2-5 minutes | 10 minutes |

---

## 📱 Mobile App Polling Example

```javascript
// Poll for status every 5 seconds
async function pollForResults(submissionId, maxAttempts = 60) {
  for (let i = 0; i < maxAttempts; i++) {
    const response = await fetch(`/api/exam/submission-status/${submissionId}`);
    const status = await response.json();
    
    // Update UI with status message
    showStatus(status.statusMessage);
    
    if (status.isComplete) {
      // Evaluation done - show results
      showResults(status.result);
      return status.result;
    }
    
    if (status.status === 'Failed') {
      throw new Error('Evaluation failed. Please try again.');
    }
    
    // Wait 5 seconds before next poll
    await new Promise(resolve => setTimeout(resolve, 5000));
  }
  
  throw new Error('Timeout waiting for evaluation');
}

// Usage
try {
  const result = await pollForResults('sub-abc123');
  console.log('Score:', result.grandScore);
} catch (error) {
  showError(error.message);
}
```

---

## 🚀 Azure Function Deployment

### Deploy to Azure:

```bash
# Navigate to function app directory
cd azure-functions/AnswerSheetProcessor

# Deploy to Azure
func azure functionapp publish smartstudy-func --dotnet-isolated

# Check logs
func azure functionapp logstream smartstudy-func
```

### Local Testing:

```bash
# Start Azure Storage Emulator (Azurite)
azurite --silent --location ./azurite-data

# Start function app locally
func start

# Send test message to queue
az storage message put \
  --queue-name written-submission-processing \
  --content '{"writtenSubmissionId":"test-123",...}'
```

---

## 📋 Checklist: Why Status Stays "PendingEvaluation"

If status doesn't change, check:

| Check | How to Verify |
|-------|---------------|
| ✅ Azure Function running? | Azure Portal → Function App → Monitor |
| ✅ Queue connected? | Storage Explorer → Queues → Check messages |
| ✅ OpenAI configured? | Function App → Configuration → API keys |
| ✅ Database accessible? | Check connection string, firewall rules |
| ✅ Blob Storage readable? | Verify SAS tokens, container access |

---

## 🔗 Related Documentation

- [MOBILE-EXAM-RESULT-API.md](./MOBILE-EXAM-RESULT-API.md) - Result endpoint details
- [EXAM-SYSTEM-API-REFERENCE.md](./EXAM-SYSTEM-API-REFERENCE.md) - Full API docs
- [AZURE_SETUP.md](./AZURE_SETUP.md) - Azure resource setup

---

**Last Updated:** December 14, 2025  
**Architecture:** ASP.NET Core Backend + Azure Functions + Azure OpenAI
