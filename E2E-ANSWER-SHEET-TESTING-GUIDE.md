# End-to-End Answer Sheet Upload API Testing Guide

## Overview
This guide demonstrates the complete flow for testing the answer sheet upload system with step-by-step evaluation, expected answers, and detailed mark breakdowns.

## Prerequisites
- Backend server running (default: `http://localhost:8080`)
- Valid Azure OpenAI credentials configured
- Azure Blob Storage configured
- Azure Queue configured (for async processing)

## Test Flow Architecture

```
┌──────────────┐      ┌──────────────┐      ┌──────────────┐      ┌──────────────┐
│   Step 1:    │      │   Step 2:    │      │   Step 3:    │      │   Step 4:    │
│   Generate   │ ───▶ │   Upload     │ ───▶ │   Poll       │ ───▶ │   Fetch      │
│   Exam       │      │   Answers    │      │   Status     │      │   Feedback   │
└──────────────┘      └──────────────┘      └──────────────┘      └──────────────┘
```

---

## Step 1: Generate Exam with Questions

### API Endpoint
```http
POST /api/exam/generate
Content-Type: application/json
```

### Request Body
```json
{
  "subject": "Mathematics",
  "grade": "2nd PUC",
  "chapter": "Integration",
  "difficulty": "medium",
  "examType": "full",
  "useCache": false,
  "fastMode": true
}
```

### Response
```json
{
  "exam": {
    "examId": "Karnataka_2nd_PUC_Math_2024_25_Integration_20241218_123045",
    "subject": "Mathematics",
    "grade": "2nd PUC",
    "chapter": "Integration",
    "questionCount": 10,
    "totalMarks": 50,
    "duration": 60,
    "parts": [
      {
        "partName": "Part A - MCQ",
        "totalQuestions": 3,
        "marksPerQuestion": 1
      },
      {
        "partName": "Part B - Short Answer",
        "totalQuestions": 4,
        "marksPerQuestion": 3
      },
      {
        "partName": "Part C - Long Answer",
        "totalQuestions": 3,
        "marksPerQuestion": 5
      }
    ],
    "questions": [
      {
        "questionNumber": 1,
        "questionText": "What is the integral of 2x?",
        "marks": 1,
        "type": "MCQ",
        "options": ["x^2 + C", "2x^2 + C", "x + C", "2x + C"],
        "correctAnswer": "x^2 + C",
        "expectedAnswer": "x^2 + C"
      },
      // ... more questions
    ]
  },
  "cached": false,
  "generationTime": "12.5s"
}
```

### Key Points
- The `examId` is required for subsequent upload
- Questions include `expectedAnswer` for each question
- Subjective questions have detailed rubrics stored separately
- MCQ questions have `correctAnswer` field

### PowerShell Example
```powershell
$examRequest = @{
    subject = "Mathematics"
    grade = "2nd PUC"
    chapter = "Integration"
    difficulty = "medium"
    examType = "full"
    useCache = $false
    fastMode = $true
} | ConvertTo-Json

$examResponse = Invoke-RestMethod -Uri "http://localhost:8080/api/exam/generate" `
    -Method POST `
    -Body $examRequest `
    -ContentType "application/json"

$examId = $examResponse.exam.examId
Write-Host "Generated Exam ID: $examId"
```

---

## Step 2: Upload Student Answer Sheet

### API Endpoint
```http
POST /api/exam/upload-written
Content-Type: multipart/form-data
```

### Request Parameters
- `examId` (form field): The exam ID from Step 1
- `studentId` (form field): Unique student identifier
- `files` (file upload): One or more image/PDF files of answer sheets

### Supported File Types
- `.jpg`, `.jpeg` - JPEG images
- `.png` - PNG images
- `.pdf` - PDF documents
- `.webp` - WebP images

### File Constraints
- Max file size: 10 MB per file
- Max files per upload: 20 files
- Total request size limit: 100 MB

### Response
```json
{
  "writtenSubmissionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": "PendingEvaluation",
  "message": "Answer sheet uploaded successfully! Processing will begin shortly."
}
```

### Key Points
- Upload is **synchronous** - validates, saves to blob, and enqueues
- Returns immediately with `PendingEvaluation` status
- Processing happens asynchronously via Azure Functions
- Duplicate submissions are prevented (409 Conflict)

### PowerShell Example
```powershell
$studentId = "student_12345"
$answerFile = "answer_sheet.jpg"

# Prepare multipart form data
$boundary = [System.Guid]::NewGuid().ToString()
$fileBin = [System.IO.File]::ReadAllBytes($answerFile)
$fileName = [System.IO.Path]::GetFileName($answerFile)

$LF = "`r`n"
$bodyLines = (
    "--$boundary",
    "Content-Disposition: form-data; name=`"examId`"",
    "",
    $examId,
    "--$boundary",
    "Content-Disposition: form-data; name=`"studentId`"",
    "",
    $studentId,
    "--$boundary",
    "Content-Disposition: form-data; name=`"files`"; filename=`"$fileName`"",
    "Content-Type: image/jpeg",
    "",
    [System.Text.Encoding]::UTF8.GetString($fileBin),
    "--$boundary--"
) -join $LF

$uploadResponse = Invoke-RestMethod -Uri "http://localhost:8080/api/exam/upload-written" `
    -Method POST `
    -ContentType "multipart/form-data; boundary=$boundary" `
    -Body $bodyLines

$submissionId = $uploadResponse.writtenSubmissionId
Write-Host "Submission ID: $submissionId"
```

### curl Example
```bash
curl -X POST http://localhost:8080/api/exam/upload-written \
  -F "examId=Karnataka_2nd_PUC_Math_2024_25_Integration_20241218_123045" \
  -F "studentId=student_12345" \
  -F "files=@answer_sheet.jpg" \
  -F "files=@answer_sheet_page2.jpg"
```

---

## Step 3: Poll Submission Status

### API Endpoint
```http
GET /api/exam/submission-status/{writtenSubmissionId}
```

### Response (Processing)
```json
{
  "writtenSubmissionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": "1",
  "statusMessage": "Reading your answer sheet...",
  "pollIntervalSeconds": 3,
  "submittedAt": "2024-12-18T12:30:45Z",
  "evaluatedAt": null,
  "isComplete": false,
  "isError": false,
  "examId": "Karnataka_2nd_PUC_Math_2024_25_Integration_20241218_123045",
  "studentId": "student_12345"
}
```

### Response (Completed)
```json
{
  "writtenSubmissionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": "3",
  "statusMessage": "Results Ready!",
  "pollIntervalSeconds": 0,
  "submittedAt": "2024-12-18T12:30:45Z",
  "evaluatedAt": "2024-12-18T12:32:15Z",
  "isComplete": true,
  "isError": false,
  "examId": "Karnataka_2nd_PUC_Math_2024_25_Integration_20241218_123045",
  "studentId": "student_12345",
  "evaluationResultBlobPath": "evaluation-results/exam123/student456/result.json",
  "totalScore": 42,
  "maxPossibleScore": 50,
  "percentage": 84.0,
  "grade": "A"
}
```

### Status Codes
| Status | Meaning | Next Action |
|--------|---------|-------------|
| `0` | Uploaded | Poll in 5 seconds |
| `1` | OCR Processing | Poll in 3 seconds |
| `2` | Evaluating | Poll in 5 seconds |
| `3` | Results Ready | Fetch results (Step 4) |
| `4` | Error | Check `errorMessage` |

### PowerShell Example
```powershell
$maxPolls = 60
$pollCount = 0
$isComplete = $false

while ($pollCount -lt $maxPolls -and -not $isComplete) {
    $pollCount++
    
    $statusResponse = Invoke-RestMethod `
        -Uri "http://localhost:8080/api/exam/submission-status/$submissionId" `
        -Method GET
    
    Write-Host "[$pollCount] Status: $($statusResponse.statusMessage)"
    
    if ($statusResponse.isComplete) {
        $isComplete = $true
        Write-Host "Score: $($statusResponse.totalScore)/$($statusResponse.maxPossibleScore) ($($statusResponse.percentage)%)"
        break
    }
    
    $waitTime = if ($statusResponse.pollIntervalSeconds -gt 0) { 
        $statusResponse.pollIntervalSeconds 
    } else { 5 }
    
    Start-Sleep -Seconds $waitTime
}
```

---

## Step 4: Fetch Detailed Feedback with Expected Answers

### API Endpoint
```http
GET /api/exam/evaluation-result/{writtenSubmissionId}
```

### Response Structure
```json
{
  "writtenSubmissionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "examId": "Karnataka_2nd_PUC_Math_2024_25_Integration_20241218_123045",
  "studentId": "student_12345",
  "evaluatedAt": "2024-12-18T12:32:15Z",
  "blobPath": "evaluation-results/exam123/student456/result.json",
  "summary": {
    "totalScore": 42,
    "maxPossibleScore": 50,
    "percentage": 84.0,
    "grade": "A"
  },
  "evaluationResult": {
    "questionResults": [
      {
        "questionNumber": 1,
        "questionText": "What is the integral of 2x?",
        "maxMarks": 1,
        "marksAwarded": 1,
        "studentAnswer": "x^2 + C",
        "expectedAnswer": "x^2 + C",
        "feedback": "Correct! Perfect answer.",
        "stepByStepEvaluation": [
          "Student correctly identified the power rule",
          "Constant of integration included",
          "Final answer matches expected"
        ]
      },
      {
        "questionNumber": 4,
        "questionText": "Evaluate ∫(3x² + 2x + 1)dx",
        "maxMarks": 3,
        "marksAwarded": 2.5,
        "studentAnswer": "x³ + x² + x + C",
        "expectedAnswer": "x³ + x² + x + C",
        "feedback": "Mostly correct. Minor notation issue.",
        "rubricBreakdown": [
          {
            "criterion": "Correct integration of 3x²",
            "maxPoints": 1,
            "pointsAwarded": 1,
            "feedback": "Perfect"
          },
          {
            "criterion": "Correct integration of 2x",
            "maxPoints": 1,
            "pointsAwarded": 1,
            "feedback": "Perfect"
          },
          {
            "criterion": "Correct integration of constant",
            "maxPoints": 0.5,
            "pointsAwarded": 0.5,
            "feedback": "Correct"
          },
          {
            "criterion": "Clear working shown",
            "maxPoints": 0.5,
            "pointsAwarded": 0,
            "feedback": "Working steps could be clearer"
          }
        ],
        "stepByStepEvaluation": [
          "Correctly applied integration rules",
          "Final answer is correct",
          "Minor: Working steps not fully shown"
        ]
      }
    ],
    "overallFeedback": "Excellent performance! Strong understanding of integration concepts.",
    "strengths": [
      "Accurate application of integration rules",
      "Correct handling of constants",
      "Good final answers"
    ],
    "areasForImprovement": [
      "Show more intermediate steps",
      "Include verification where possible"
    ]
  }
}
```

### Key Response Fields

#### Summary Level
- `totalScore`: Total marks obtained
- `maxPossibleScore`: Total marks possible
- `percentage`: Percentage score
- `grade`: Letter grade (A, B, C, etc.)

#### Question Level
- `questionNumber`: Question identifier
- `questionText`: The question asked
- `maxMarks`: Maximum marks for question
- `marksAwarded`: Marks student received
- `studentAnswer`: What student wrote
- `expectedAnswer`: Model answer / correct answer
- `feedback`: AI-generated feedback

#### Step-by-Step Evaluation
- Array of evaluation points
- Highlights strengths and mistakes
- Guides student understanding

#### Rubric Breakdown (Subjective Questions)
- `criterion`: Evaluation criterion name
- `maxPoints`: Points allocated to criterion
- `pointsAwarded`: Points student earned
- `feedback`: Specific feedback for criterion

### PowerShell Example
```powershell
$evaluationResult = Invoke-RestMethod `
    -Uri "http://localhost:8080/api/exam/evaluation-result/$submissionId" `
    -Method GET

Write-Host "Overall Score: $($evaluationResult.summary.totalScore)/$($evaluationResult.summary.maxPossibleScore)"
Write-Host "Grade: $($evaluationResult.summary.grade)"

foreach ($qResult in $evaluationResult.evaluationResult.questionResults) {
    Write-Host "`nQuestion $($qResult.questionNumber): $($qResult.marksAwarded)/$($qResult.maxMarks) marks"
    Write-Host "  Student: $($qResult.studentAnswer)"
    Write-Host "  Expected: $($qResult.expectedAnswer)"
    Write-Host "  Feedback: $($qResult.feedback)"
    
    if ($qResult.stepByStepEvaluation) {
        Write-Host "  Step-by-Step:"
        foreach ($step in $qResult.stepByStepEvaluation) {
            Write-Host "    - $step"
        }
    }
    
    if ($qResult.rubricBreakdown) {
        Write-Host "  Rubric:"
        foreach ($criterion in $qResult.rubricBreakdown) {
            Write-Host "    - $($criterion.criterion): $($criterion.pointsAwarded)/$($criterion.maxPoints)"
            Write-Host "      $($criterion.feedback)"
        }
    }
}
```

---

## Complete Test Script

A complete PowerShell test script is available at:
```
test-e2e-answer-sheet-flow.ps1
```

### Usage
```powershell
# Default (localhost)
.\test-e2e-answer-sheet-flow.ps1

# Custom server
.\test-e2e-answer-sheet-flow.ps1 -BaseUrl "https://your-api.azurewebsites.net"

# With actual image
.\test-e2e-answer-sheet-flow.ps1 -AnswerImagePath "C:\path\to\answer.jpg"

# Custom student ID
.\test-e2e-answer-sheet-flow.ps1 -StudentId "student_abc123"
```

### What the Script Does
1. ✅ Generates a Mathematics exam (Integration chapter)
2. ✅ Creates a sample answer sheet (or uses provided image)
3. ✅ Uploads the answer sheet
4. ✅ Polls status every 5 seconds (max 60 attempts)
5. ✅ Fetches detailed evaluation results
6. ✅ Displays complete feedback with:
   - Question-by-question breakdown
   - Student vs Expected answers
   - Marks awarded per question
   - Step-by-step evaluation
   - Rubric breakdowns
   - Overall strengths and weaknesses
7. ✅ Saves full JSON report to file

---

## API Response Examples

### Example 1: MCQ Question Result
```json
{
  "questionNumber": 1,
  "questionText": "What is ∫cos(x)dx?",
  "maxMarks": 1,
  "marksAwarded": 1,
  "studentAnswer": "sin(x) + C",
  "expectedAnswer": "sin(x) + C",
  "feedback": "Perfect! Correct answer.",
  "stepByStepEvaluation": [
    "Correctly recalled the antiderivative of cos(x)",
    "Included constant of integration"
  ]
}
```

### Example 2: Short Answer Question Result
```json
{
  "questionNumber": 5,
  "questionText": "Find ∫sin(2x)dx",
  "maxMarks": 3,
  "marksAwarded": 2.5,
  "studentAnswer": "-(1/2)cos(2x) + C",
  "expectedAnswer": "-(1/2)cos(2x) + C",
  "feedback": "Correct answer! Minor deduction for not showing substitution steps.",
  "stepByStepEvaluation": [
    "Correct final answer",
    "Substitution u=2x implied but not shown",
    "Missing intermediate steps"
  ],
  "rubricBreakdown": [
    {
      "criterion": "Correct substitution method",
      "maxPoints": 1,
      "pointsAwarded": 0.5,
      "feedback": "Substitution not explicitly shown"
    },
    {
      "criterion": "Correct integration",
      "maxPoints": 1,
      "pointsAwarded": 1,
      "feedback": "Perfect integration"
    },
    {
      "criterion": "Final answer accuracy",
      "maxPoints": 1,
      "pointsAwarded": 1,
      "feedback": "Correct final answer with constant"
    }
  ]
}
```

### Example 3: Long Answer Question Result
```json
{
  "questionNumber": 7,
  "questionText": "Find the area bounded by y=x² and y=4",
  "maxMarks": 5,
  "marksAwarded": 4,
  "studentAnswer": "Area = 32/3 square units",
  "expectedAnswer": "32/3 square units",
  "feedback": "Excellent work! Minor arithmetic slip in one step.",
  "stepByStepEvaluation": [
    "Correctly identified intersection points x=±2",
    "Set up correct definite integral ∫₋₂²(4-x²)dx",
    "Evaluated antiderivative correctly",
    "Minor arithmetic error in intermediate step (self-corrected)",
    "Final answer is correct"
  ],
  "rubricBreakdown": [
    {
      "criterion": "Finding intersection points",
      "maxPoints": 1,
      "pointsAwarded": 1,
      "feedback": "Correctly solved x²=4"
    },
    {
      "criterion": "Setting up integral",
      "maxPoints": 1,
      "pointsAwarded": 1,
      "feedback": "Correct integral setup with limits"
    },
    {
      "criterion": "Integration technique",
      "maxPoints": 1,
      "pointsAwarded": 1,
      "feedback": "Proper application of power rule"
    },
    {
      "criterion": "Calculation accuracy",
      "maxPoints": 1,
      "pointsAwarded": 0.5,
      "feedback": "Minor arithmetic slip in one step"
    },
    {
      "criterion": "Final answer with units",
      "maxPoints": 1,
      "pointsAwarded": 0.5,
      "feedback": "Correct answer but working could be neater"
    }
  ]
}
```

---

## Testing Checklist

### Pre-Test
- [ ] Backend server is running
- [ ] Azure OpenAI credentials configured
- [ ] Azure Blob Storage accessible
- [ ] Azure Queue accessible
- [ ] Test answer sheets prepared (images/PDFs)

### Test Execution
- [ ] Step 1: Exam generation successful
- [ ] Step 2: Answer sheet upload successful
- [ ] Step 3: Status polling works (max 5 minutes)
- [ ] Step 4: Detailed results retrieved

### Validation
- [ ] All questions have `expectedAnswer`
- [ ] Each question has `marksAwarded` and `maxMarks`
- [ ] Subjective questions have `rubricBreakdown`
- [ ] Step-by-step evaluation present
- [ ] Overall feedback and strengths/weaknesses included
- [ ] Total score matches sum of question scores
- [ ] Percentage calculated correctly
- [ ] Grade assigned appropriately

---

## Common Issues & Solutions

### Issue: Upload fails with 409 Conflict
**Cause**: Duplicate submission for same exam + student
**Solution**: Use a different `studentId` or delete existing submission

### Issue: Status stuck in "Processing"
**Cause**: Azure Function not running or queue not processed
**Solution**: 
- Check Azure Function logs
- Verify queue message exists
- Restart Azure Function if needed

### Issue: No expected answers in results
**Cause**: Exam generation didn't store expected answers
**Solution**: Regenerate exam with `useCache=false`

### Issue: Rubric breakdown missing
**Cause**: Only subjective questions have rubrics
**Solution**: This is expected for MCQ questions

### Issue: Percentage doesn't match calculation
**Cause**: Rounding differences
**Solution**: Use `Math.Round()` consistently to 2 decimal places

---

## Advanced Scenarios

### Testing with Real Images
```powershell
.\test-e2e-answer-sheet-flow.ps1 `
    -AnswerImagePath "C:\scans\student_answer_page1.jpg" `
    -StudentId "student_john_doe"
```

### Testing Multiple Pages
```bash
curl -X POST http://localhost:8080/api/exam/upload-written \
  -F "examId=exam_123" \
  -F "studentId=student_456" \
  -F "files=@page1.jpg" \
  -F "files=@page2.jpg" \
  -F "files=@page3.jpg"
```

### Parallel Testing (Multiple Students)
```powershell
$students = @("alice", "bob", "charlie")
$students | ForEach-Object -Parallel {
    .\test-e2e-answer-sheet-flow.ps1 -StudentId "student_$_"
} -ThrottleLimit 3
```

---

## API Rate Limits

| Operation | Limit | Recommendation |
|-----------|-------|----------------|
| Exam Generation | 10/min | Cache exams when possible |
| Answer Upload | 100/min | Normal usage |
| Status Polling | 1000/min | Use recommended poll intervals |
| Results Fetch | 500/min | Cache results after first fetch |

---

## Next Steps

1. **Start Backend**: Ensure ASP.NET Core backend is running
2. **Run Test Script**: Execute `test-e2e-answer-sheet-flow.ps1`
3. **Review Output**: Check console for detailed progress
4. **Examine JSON Report**: Open saved report file for full details
5. **Integrate**: Use these APIs in your frontend/mobile app

---

## Support

For issues or questions:
- Check console logs in backend
- Review Azure Function logs
- Examine queue messages in Azure Storage Explorer
- Verify blob storage contents

## Related Documentation
- [API_REFERENCE_UPDATED.md](./API_REFERENCE_UPDATED.md) - Complete API documentation
- [ANSWER-SHEET-UPLOAD-FLOW.md](./ANSWER-SHEET-UPLOAD-FLOW.md) - Architecture details
- [EXAM_SYSTEM_README.md](./EXAM_SYSTEM_README.md) - Exam system overview
