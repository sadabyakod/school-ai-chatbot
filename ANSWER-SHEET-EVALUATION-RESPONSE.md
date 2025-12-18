# Answer Sheet Evaluation - Response Payload

## Overview
After uploading an answer sheet for evaluation, the final result is returned via:
- **Endpoint**: `GET /api/exam/result/{examId}/{studentId}`
- **Status Code**: `200 OK`
- **Content-Type**: `application/json`

---

## Complete Response Structure

```json
{
  "examId": "string",              // Exam identifier
  "studentId": "string",           // Student identifier  
  "examTitle": "string",           // e.g., "Mathematics - Matrices and Determinants"
  
  // ============ MCQ RESULTS ============
  "mcqScore": 15,                  // Total MCQ marks earned
  "mcqTotalMarks": 20,             // Total MCQ marks available
  "mcqResults": [
    {
      "questionId": "q1",
      "selectedOption": "A",        // Student's answer
      "correctAnswer": "B",         // Correct answer
      "isCorrect": false,
      "marksAwarded": 0
    },
    {
      "questionId": "q2",
      "selectedOption": "C",
      "correctAnswer": "C",
      "isCorrect": true,
      "marksAwarded": 1
    }
    // ... more MCQ results
  ],
  
  // ============ SUBJECTIVE RESULTS ============
  "subjectiveScore": 45.5,         // Total subjective marks earned
  "subjectiveTotalMarks": 60,      // Total subjective marks available
  "subjectiveResults": [
    {
      "questionId": "q3",
      "questionNumber": 3,
      "questionText": "Calculate the determinant of matrix A",
      "earnedMarks": 8.5,           // Marks awarded by AI
      "maxMarks": 10,               // Maximum marks for this question
      "isFullyCorrect": false,
      
      // Expected/Model Answer
      "expectedAnswer": "To find the determinant:\n1. Apply row operations\n2. Calculate using cofactor expansion\nResult: det(A) = -14",
      
      // Student's Answer (OCR extracted)
      "studentAnswerEcho": "det(A) = (2)(5) - (3)(4) = 10 - 12 = -2\nFinal answer: -2",
      
      // Step-by-Step Evaluation
      "stepAnalysis": [
        {
          "step": 1,
          "description": "Set up the determinant formula correctly",
          "isCorrect": true,
          "marksAwarded": 2,
          "maxMarksForStep": 2,
          "feedback": "Correct formula applied for 2×2 matrix"
        },
        {
          "step": 2,
          "description": "Perform arithmetic calculations",
          "isCorrect": true,
          "marksAwarded": 2,
          "maxMarksForStep": 2,
          "feedback": "Calculation is accurate"
        },
        {
          "step": 3,
          "description": "State the final answer with proper notation",
          "isCorrect": true,
          "marksAwarded": 1.5,
          "maxMarksForStep": 2,
          "feedback": "Final answer stated but missing units"
        },
        {
          "step": 4,
          "description": "Show verification or alternative method",
          "isCorrect": false,
          "marksAwarded": 0,
          "maxMarksForStep": 4,
          "feedback": "No verification shown"
        }
      ],
      
      // Overall AI Feedback
      "overallFeedback": "Good understanding of determinant calculation for 2×2 matrices. The formula and arithmetic are correct. However, consider showing verification or using cofactor expansion for additional marks. Also, include proper mathematical notation."
    },
    {
      "questionId": "q4",
      "questionNumber": 4,
      "questionText": "Prove that det(AB) = det(A) × det(B)",
      "earnedMarks": 12,
      "maxMarks": 15,
      "isFullyCorrect": false,
      "expectedAnswer": "Proof using properties of determinants...",
      "studentAnswerEcho": "Let A and B be matrices...",
      "stepAnalysis": [
        {
          "step": 1,
          "description": "State the theorem and assumptions",
          "isCorrect": true,
          "marksAwarded": 3,
          "maxMarksForStep": 3,
          "feedback": "Clear problem statement"
        },
        {
          "step": 2,
          "description": "Apply properties of determinants",
          "isCorrect": true,
          "marksAwarded": 5,
          "maxMarksForStep": 6,
          "feedback": "Good application but missing one property"
        },
        {
          "step": 3,
          "description": "Complete the proof with conclusion",
          "isCorrect": true,
          "marksAwarded": 4,
          "maxMarksForStep": 6,
          "feedback": "Conclusion stated but proof steps incomplete"
        }
      ],
      "overallFeedback": "Well-structured proof with good understanding of determinant properties. Include all intermediate steps for full marks."
    }
    // ... more subjective results
  ],
  
  // ============ GRAND TOTAL ============
  "grandScore": 60.5,              // MCQ + Subjective total
  "grandTotalMarks": 80,           // Total marks for entire exam
  "percentage": 75.63,             // (grandScore / grandTotalMarks) × 100
  "grade": "A",                    // A+, A, B, C, D, or F
  "passed": true,                  // Pass/Fail (>= 35% in Karnataka)
  
  "evaluatedAt": "2025-12-17 22:45:30"  // Timestamp of evaluation
}
```

---

## Grade Calculation

**Percentage → Grade Mapping:**
- **90-100%** → A+ (Distinction)
- **75-89%** → A (First Class)
- **60-74%** → B (Second Class)
- **50-59%** → C (Pass Class)
- **35-49%** → D (Pass)
- **Below 35%** → F (Fail)

**Pass Criteria:** 35% (Karnataka 2nd PUC standard)

---

## Response Fields Explained

### MCQ Fields
- `mcqScore`: Sum of marks from all correct MCQ answers
- `mcqTotalMarks`: Total marks available for MCQ section
- `mcqResults`: Array of individual MCQ question results
  - `isCorrect`: Boolean indicating if student's answer matches correct answer
  - `marksAwarded`: 0 for incorrect, 1 (or question marks) for correct

### Subjective Fields
- `subjectiveScore`: Sum of `earnedMarks` from all subjective questions
- `subjectiveTotalMarks`: Sum of `maxMarks` from all subjective questions
- `subjectiveResults`: Array of detailed evaluations for each subjective question
  - `earnedMarks`: AI-awarded marks (can be decimal, e.g., 8.5)
  - `maxMarks`: Maximum possible marks for the question
  - `isFullyCorrect`: True if student got 100% marks
  - `expectedAnswer`: Model answer or marking scheme
  - `studentAnswerEcho`: Student's actual answer (OCR-extracted from image)
  - `stepAnalysis`: Step-by-step breakdown of marking
    - Each step shows: description, correctness, marks awarded, max marks, feedback
  - `overallFeedback`: AI-generated overall feedback for the answer

### Grand Total Fields
- `grandScore`: Combined MCQ + Subjective score
- `grandTotalMarks`: Total marks for entire exam
- `percentage`: Final percentage (rounded to 2 decimals)
- `grade`: Letter grade based on percentage
- `passed`: Boolean - true if percentage >= 35%

---

## Example Real Response

```json
{
  "examId": "exam_20251217_224530_abc123",
  "studentId": "TEST-224530",
  "examTitle": "Mathematics - Matrices and Determinants",
  "mcqScore": 15,
  "mcqTotalMarks": 20,
  "mcqResults": [
    {
      "questionId": "q1_mcq",
      "selectedOption": "B",
      "correctAnswer": "B",
      "isCorrect": true,
      "marksAwarded": 1
    }
  ],
  "subjectiveScore": 45.5,
  "subjectiveTotalMarks": 60,
  "subjectiveResults": [
    {
      "questionId": "q3_subj",
      "questionNumber": 3,
      "questionText": "Calculate determinant of 2×2 matrix",
      "earnedMarks": 8.5,
      "maxMarks": 10,
      "isFullyCorrect": false,
      "expectedAnswer": "det(A) = ad - bc\nFor given matrix...",
      "studentAnswerEcho": "det(A) = (2)(5) - (3)(4) = -2",
      "stepAnalysis": [
        {
          "step": 1,
          "description": "Apply determinant formula",
          "isCorrect": true,
          "marksAwarded": 2,
          "maxMarksForStep": 2,
          "feedback": "Correct formula"
        },
        {
          "step": 2,
          "description": "Calculate result",
          "isCorrect": true,
          "marksAwarded": 2,
          "maxMarksForStep": 2,
          "feedback": "Accurate calculation"
        }
      ],
      "overallFeedback": "Good work! Minor improvements needed."
    }
  ],
  "grandScore": 60.5,
  "grandTotalMarks": 80,
  "percentage": 75.63,
  "grade": "A",
  "passed": true,
  "evaluatedAt": "2025-12-17 22:45:30"
}
```

---

## API Usage Flow

1. **Generate Exam** → `POST /api/exam/generate` → Get `examId`
2. **Upload Answer Sheet** → `POST /api/written-submission/submit-with-extraction` → Get `submissionId`
3. **Check Status** → `GET /api/written-submission/status/{submissionId}` → Wait for "Completed"
4. **Get Results** → `GET /api/exam/result/{examId}/{studentId}` → **THIS RESPONSE**

---

## Notes

- **Step Analysis**: Provided only if the exam has rubrics defined for subjective questions
- **AI Evaluation**: Uses Azure OpenAI GPT-4o-mini for marking with vision capabilities for OCR
- **Partial Marks**: AI can award partial marks (e.g., 8.5/10) based on answer quality
- **Feedback**: Each question gets overall feedback + step-wise feedback for learning
- **Pass/Fail**: Based on 35% threshold (configurable per institution)

---

## Error Responses

**404 Not Found**
```json
{
  "status": "error",
  "message": "Exam exam_xyz not found. Please generate the exam first."
}
```

**500 Internal Server Error**
```json
{
  "status": "error",
  "message": "An unexpected error occurred while fetching results."
}
```
