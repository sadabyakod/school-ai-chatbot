# End-to-End Answer Sheet Upload Flow - Quick Reference

## Complete Flow Summary

```
1. GENERATE EXAM
   POST /api/exam/generate
   → Returns examId + questions with expected answers

2. UPLOAD ANSWERS
   POST /api/exam/upload-written
   → Returns submissionId + status "PendingEvaluation"

3. POLL STATUS
   GET /api/exam/submission-status/{submissionId}
   → Keep polling until status = "3" (Results Ready)

4. GET FEEDBACK
   GET /api/exam/evaluation-result/{submissionId}
   → Returns detailed marks, expected answers, step-by-step feedback
```

## Test Script Available

**File**: `test-e2e-answer-sheet-flow.ps1`

**Usage**:
```powershell
.\test-e2e-answer-sheet-flow.ps1
```

**What it tests**:
1. ✅ Generate exam with Integration questions
2. ✅ Create sample answer sheet
3. ✅ Upload to server
4. ✅ Poll status until complete
5. ✅ Fetch detailed results with:
   - Marks awarded per question
   - Expected answers vs student answers
   - Step-by-step evaluation
   - Rubric breakdown for subjective questions
   - Overall strengths and weaknesses

## Key API Endpoints

### 1. Generate Exam
```http
POST /api/exam/generate
Content-Type: application/json

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

**Response**: Exam with questions, each including `expectedAnswer`

### 2. Upload Answer Sheet
```http
POST /api/exam/upload-written
Content-Type: multipart/form-data

examId={examId}
studentId={studentId}
files={image/pdf files}
```

**Response**: `submissionId` for tracking

### 3. Check Status
```http
GET /api/exam/submission-status/{submissionId}
```

**Response**: Status codes:
- `0` = Uploaded
- `1` = OCR Processing
- `2` = Evaluating
- `3` = Results Ready ✅
- `4` = Error

### 4. Get Detailed Feedback
```http
GET /api/exam/evaluation-result/{submissionId}
```

**Response**: Complete evaluation with:
```json
{
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
        "questionText": "...",
        "marksAwarded": 1,
        "maxMarks": 1,
        "studentAnswer": "x^2 + C",
        "expectedAnswer": "x^2 + C",
        "feedback": "Perfect!",
        "stepByStepEvaluation": ["..."],
        "rubricBreakdown": [...]
      }
    ],
    "overallFeedback": "...",
    "strengths": [...],
    "areasForImprovement": [...]
  }
}
```

## Example Results

### MCQ Question
```
Question 1: What is ∫cos(x)dx?
Student Answer: sin(x) + C
Expected Answer: sin(x) + C
Marks: 1/1
Feedback: ✅ Perfect! Correct answer.
```

### Subjective Question
```
Question 4: Evaluate ∫(3x² + 2x + 1)dx
Student Answer: x³ + x² + x + C
Expected Answer: x³ + x² + x + C
Marks: 2.5/3

Rubric Breakdown:
✅ Correct integration of 3x²: 1/1
✅ Correct integration of 2x: 1/1
✅ Correct integration of constant: 0.5/0.5
⚠️  Clear working shown: 0/0.5 (Working steps could be clearer)

Feedback: Mostly correct. Minor notation issue.
Step-by-Step:
• Correctly applied integration rules
• Final answer is correct
• Minor: Working steps not fully shown
```

## Prerequisites to Run Test

1. Backend server running on port 8080
2. Azure OpenAI configured
3. Azure Blob Storage configured
4. Azure Queue configured

## Files Created

1. **test-e2e-answer-sheet-flow.ps1** - Complete automated test script
2. **E2E-ANSWER-SHEET-TESTING-GUIDE.md** - Detailed documentation with examples
3. **E2E-ANSWER-SHEET-QUICK-REFERENCE.md** - This quick reference

## Next Steps

To run the test:

```powershell
# Make sure backend is running first
cd SchoolAiChatbotBackend
dotnet run

# In another terminal, run the test
cd ..
.\test-e2e-answer-sheet-flow.ps1
```

## Expected Output

```
================================================================================
  STEP 1: Generating Exam Paper
================================================================================
  Sending exam generation request...
   Subject : Mathematics
   Grade : 2nd PUC
   Chapter : Integration
 Exam generated successfully!
   Exam ID : Karnataka_2nd_PUC_Math_2024_25_Integration_20241218_123045
   Total Questions : 10
   Total Marks : 50

================================================================================
  STEP 2: Uploading Student Answer Sheet
================================================================================
  Preparing answer sheet upload...
   Student ID : student_123456
   Exam ID : Karnataka_2nd_PUC_Math_2024_25_Integration_20241218_123045
  No image provided. Creating text-based answer file...
 Created sample answer file: temp_answer_sheet_123456.txt
  Uploading answer sheet...
 Answer sheet uploaded successfully!
   Submission ID : a1b2c3d4-e5f6-7890-abcd-ef1234567890
   Status : PendingEvaluation
   Message : Answer sheet uploaded successfully! Processing will begin shortly.

================================================================================
  STEP 3: Monitoring Evaluation Status
================================================================================
  Polling for evaluation status (max 60 attempts)...
[1/60] [UPLOADED] Uploaded - Processing will start soon
[2/60] [OCR] Reading your answer sheet...
[3/60] [OCR] Reading your answer sheet...
[4/60] [EVAL] Evaluating your answers...
[5/60] [EVAL] Evaluating your answers...
[6/60] [READY] Results Ready!
 Evaluation complete!
   Total Score : 42/50
   Percentage : 84.0%
   Grade : A

================================================================================
  STEP 4: Fetching Detailed Evaluation Results
================================================================================
  Retrieving complete feedback with expected answers...
 Evaluation results retrieved!

EVALUATION SUMMARY
================================================================================
   Submission ID : a1b2c3d4-e5f6-7890-abcd-ef1234567890
   Exam ID : Karnataka_2nd_PUC_Math_2024_25_Integration_20241218_123045
   Student ID : student_123456
   Evaluated At : 2024-12-18T12:32:15Z

   Total Score : 42/50
   Percentage : 84.0%
   Grade : A

QUESTION-BY-QUESTION FEEDBACK
================================================================================

Question 1
--------------------------------------------------------------------------------
 Question: What is the integral of 2x?
 Marks Awarded: 1 / 1
 Student Answer: x squared + C
 Expected Answer: x^2 + C
 Feedback: Correct! Perfect answer.

Question 4
--------------------------------------------------------------------------------
 Question: Evaluate integral of 3x squared plus 2x plus 1 dx
 Marks Awarded: 2.5 / 3
 Student Answer: x cubed + x squared + x + C
 Expected Answer: x³ + x² + x + C
 Feedback: Mostly correct. Minor notation issue.
 Rubric Breakdown:
   - Correct integration of 3x²: 1/1
     → Perfect
   - Correct integration of 2x: 1/1
     → Perfect
   - Correct integration of constant: 0.5/0.5
     → Correct
   - Clear working shown: 0/0.5
     → Working steps could be clearer
 Step-by-Step Evaluation:
   - Correctly applied integration rules
   - Final answer is correct
   - Minor: Working steps not fully shown

[... more questions ...]

OVERALL FEEDBACK
================================================================================
Excellent performance! Strong understanding of integration concepts.

STRENGTHS
  ✓ Accurate application of integration rules
  ✓ Correct handling of constants
  ✓ Good final answers

AREAS FOR IMPROVEMENT
  → Show more intermediate steps
  → Include verification where possible

 Full report saved to: evaluation_report_a1b2c3d4-e5f6-7890_20241218_123456.json

================================================================================
   END-TO-END TEST COMPLETED SUCCESSFULLY!
================================================================================

 Test Summary:
   1. Exam Generated : Karnataka_2nd_PUC_Math_2024_25_Integration_20241218_123045
   2. Answer Uploaded : a1b2c3d4-e5f6-7890-abcd-ef1234567890
   3. Evaluation Status : Completed after 6 polls
   4. Results Retrieved : Done - Score: 42/50 - 84.0%

 API Endpoints Used:
   1. POST http://localhost:8080/api/exam/generate
   2. POST http://localhost:8080/api/exam/upload-written
   3. GET  http://localhost:8080/api/exam/submission-status/a1b2c3d4-e5f6-7890
   4. GET  http://localhost:8080/api/exam/evaluation-result/a1b2c3d4-e5f6-7890

All steps executed successfully!
```

## Documentation

For complete details, see [E2E-ANSWER-SHEET-TESTING-GUIDE.md](./E2E-ANSWER-SHEET-TESTING-GUIDE.md)
