# Exam Analytics API Documentation

## Overview
The Exam Analytics API provides endpoints for teachers and administrators to review student submissions, analyze exam performance, and generate statistical reports. These endpoints complement the existing exam generation and submission APIs.

## Endpoints

### 1. Get All Submissions for an Exam
Retrieves a paginated list of all student submissions for a specific exam.

**Endpoint:** `GET /api/exam/{examId}/submissions`

**Parameters:**
- `examId` (path, required): The unique identifier of the exam
- `page` (query, optional): Page number (default: 1)
- `pageSize` (query, optional): Number of items per page (default: 20, max: 100)

**Response:** `200 OK`
```json
{
  "items": [
    {
      "examId": "exam-123",
      "studentId": "student-001",
      "submissionType": "Both",
      "mcqSubmittedAt": "2025-12-08T10:30:00Z",
      "writtenSubmittedAt": "2025-12-08T10:45:00Z",
      "latestSubmissionTime": "2025-12-08T10:45:00Z",
      "totalScore": 85.5,
      "totalMaxScore": 100,
      "percentage": 85.5,
      "status": "Completed"
    }
  ],
  "totalCount": 25,
  "page": 1,
  "pageSize": 20,
  "totalPages": 2,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

**Error Responses:**
- `404 Not Found`: Exam does not exist
```json
{
  "error": "Exam exam-123 not found"
}
```

**Example Usage:**
```powershell
$response = Invoke-RestMethod -Uri "http://localhost:8080/api/exam/exam-123/submissions?page=1&pageSize=10" -Method GET
```

---

### 2. Get Detailed Submission for a Student
Retrieves comprehensive details of a specific student's submission including all answers, evaluations, and scores.

**Endpoint:** `GET /api/exam/{examId}/submissions/{studentId}`

**Parameters:**
- `examId` (path, required): The unique identifier of the exam
- `studentId` (path, required): The unique identifier of the student

**Response:** `200 OK`
```json
{
  "examId": "exam-123",
  "studentId": "student-001",
  "examTitle": "2nd PUC Mathematics Model Paper",
  "subject": "Mathematics",
  "gradeLevel": "2nd PUC",
  "chapter": "Calculus",
  
  "hasMcqSubmission": true,
  "mcqSubmittedAt": "2025-12-08T10:30:00Z",
  "mcqScore": 15,
  "mcqTotalMarks": 20,
  "mcqAnswers": [
    {
      "questionId": "q1",
      "questionNumber": 1,
      "selectedAnswer": "Option B",
      "correctAnswer": "Option B",
      "isCorrect": true,
      "marksAwarded": 1,
      "maxMarks": 1
    }
  ],
  
  "hasWrittenSubmission": true,
  "writtenSubmittedAt": "2025-12-08T10:45:00Z",
  "writtenSubmissionId": "written-sub-456",
  "writtenStatus": "Completed",
  "subjectiveScore": 70.5,
  "subjectiveTotalMarks": 80,
  "subjectiveEvaluations": [
    {
      "questionId": "q10",
      "questionNumber": 10,
      "earnedMarks": 8.5,
      "maxMarks": 10,
      "isFullyCorrect": false,
      "expectedAnswer": "Detailed explanation...",
      "studentAnswerEcho": "Student's answer...",
      "overallFeedback": "Good understanding with minor errors",
      "stepAnalysis": [
        {
          "stepNumber": 1,
          "description": "Initial setup",
          "marksAwarded": 2,
          "maxMarks": 2,
          "feedback": "Correct approach"
        }
      ]
    }
  ],
  
  "percentage": 85.5,
  "letterGrade": "A",
  "status": "Completed"
}
```

**Error Responses:**
- `404 Not Found`: Exam or submission does not exist
```json
{
  "error": "No submission found for student student-001 in exam exam-123"
}
```

**Example Usage:**
```powershell
$response = Invoke-RestMethod -Uri "http://localhost:8080/api/exam/exam-123/submissions/student-001" -Method GET
```

---

### 3. Get Student's Exam History
Retrieves all exams attempted by a specific student with their performance summary.

**Endpoint:** `GET /api/exam/submissions/by-student/{studentId}`

**Parameters:**
- `studentId` (path, required): The unique identifier of the student
- `page` (query, optional): Page number (default: 1)
- `pageSize` (query, optional): Number of items per page (default: 20, max: 100)

**Response:** `200 OK`
```json
{
  "items": [
    {
      "examId": "exam-123",
      "examTitle": "2nd PUC Mathematics Model Paper",
      "subject": "Mathematics",
      "gradeLevel": "2nd PUC",
      "chapter": "Calculus",
      "attemptedAt": "2025-12-08T10:45:00Z",
      "score": 85.5,
      "totalMarks": 100,
      "percentage": 85.5,
      "status": "Completed",
      "submissionType": "Both"
    },
    {
      "examId": "exam-124",
      "examTitle": "Physics Model Test",
      "subject": "Physics",
      "gradeLevel": "2nd PUC",
      "chapter": "Thermodynamics",
      "attemptedAt": "2025-12-07T14:20:00Z",
      "score": 72.0,
      "totalMarks": 100,
      "percentage": 72.0,
      "status": "Completed",
      "submissionType": "MCQOnly"
    }
  ],
  "totalCount": 15,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

**Example Usage:**
```powershell
$response = Invoke-RestMethod -Uri "http://localhost:8080/api/exam/submissions/by-student/student-001?page=1&pageSize=10" -Method GET
```

---

### 4. Get Exam Summary Statistics
Retrieves high-level analytics and statistics for an exam including submission counts, average scores, and status breakdowns.

**Endpoint:** `GET /api/exam/{examId}/summary`

**Parameters:**
- `examId` (path, required): The unique identifier of the exam

**Response:** `200 OK`
```json
{
  "examId": "exam-123",
  "examTitle": "2nd PUC Mathematics Model Paper",
  "subject": "Mathematics",
  "totalSubmissions": 150,
  "completedSubmissions": 145,
  "pendingEvaluations": 3,
  "partialSubmissions": 2,
  "averageScore": 78.5,
  "minScore": 45.0,
  "maxScore": 98.5,
  "averagePercentage": 78.5,
  "statusBreakdown": {
    "Completed": 145,
    "PendingEvaluation": 3,
    "PartiallyCompleted": 2
  },
  "submissionTypeBreakdown": {
    "Both": 120,
    "MCQOnly": 25,
    "WrittenOnly": 5
  }
}
```

**Error Responses:**
- `404 Not Found`: Exam does not exist
```json
{
  "error": "Exam exam-123 not found"
}
```

**Example Usage:**
```powershell
$response = Invoke-RestMethod -Uri "http://localhost:8080/api/exam/exam-123/summary" -Method GET
```

---

## Data Models

### SubmissionType Enum
- `None`: No submission made
- `MCQOnly`: Only MCQ answers submitted
- `WrittenOnly`: Only written answers submitted
- `Both`: Both MCQ and written answers submitted

### SubmissionStatusType Enum
- `NotStarted`: Student has not begun the exam
- `PartiallyCompleted`: Student has submitted only one part (MCQ or written)
- `PendingEvaluation`: Submission received, awaiting AI evaluation
- `Evaluating`: Currently being evaluated by AI
- `Completed`: All submissions received and evaluated
- `Failed`: Evaluation or submission failed

### Letter Grades
Calculated based on percentage:
- `A+`: 95-100%
- `A`: 85-94.99%
- `B+`: 75-84.99%
- `B`: 65-74.99%
- `C`: 55-64.99%
- `D`: 45-54.99%
- `F`: Below 45%

---

## Integration with Existing APIs

### Exam Generation Flow
1. **Generate Exam**: `POST /api/exam/generate` → Creates exam with unique ID
2. **Student Takes Exam**: Exam is displayed to student
3. **Submit MCQ**: `POST /api/exam/submit-mcq` → Student submits multiple choice answers
4. **Upload Written Answers**: `POST /api/exam/upload-written` → Student uploads handwritten answers
5. **View Results**: Student can view their results

### Analytics Flow (NEW)
6. **Teacher Views All Submissions**: `GET /api/exam/{examId}/submissions`
7. **Teacher Reviews Individual Submission**: `GET /api/exam/{examId}/submissions/{studentId}`
8. **Teacher Checks Student History**: `GET /api/exam/submissions/by-student/{studentId}`
9. **Teacher Views Exam Analytics**: `GET /api/exam/{examId}/summary`

---

## Testing

### Unit Tests
The `ExamAnalyticsControllerTests.cs` file contains comprehensive unit tests covering:
- 404 handling for non-existent exams and submissions
- Empty result sets
- Pagination logic (min/max validation, page calculation)
- Data aggregation and statistics calculation
- Multiple submission types (MCQ only, written only, both)

Run tests with:
```powershell
cd SchoolAiChatbotBackend.Tests
dotnet test
```

### Integration Testing
Use the provided PowerShell script for end-to-end testing:
```powershell
# Start the backend server
cd SchoolAiChatbotBackend
dotnet run --urls="http://0.0.0.0:8080"

# In another terminal, run the test script
.\test-analytics-api.ps1
```

The test script:
1. Generates a sample exam
2. Submits MCQ answers for 3 students
3. Tests all 4 analytics endpoints
4. Verifies pagination and error handling
5. Outputs color-coded results

---

## Error Handling

All endpoints return appropriate HTTP status codes:
- `200 OK`: Successful request
- `404 Not Found`: Exam or submission not found
- `500 Internal Server Error`: Server-side error occurred

Error responses follow a consistent format:
```json
{
  "error": "Description of the error"
}
```

---

## Performance Considerations

- **Pagination**: Always use pagination for large result sets to avoid memory issues
- **Caching**: Consider implementing caching for frequently accessed summaries
- **Indexing**: Ensure database indexes on `ExamId` and `StudentId` for fast lookups
- **Async Operations**: All endpoints use async/await for non-blocking I/O

---

## Security Considerations (Future Enhancements)

Current implementation focuses on functionality. For production deployment, consider:
- **Authentication**: JWT or OAuth2 token validation
- **Authorization**: Role-based access control (teacher, admin, student)
- **Rate Limiting**: Prevent abuse of analytics endpoints
- **Data Privacy**: Ensure teachers can only access exams they created
- **Audit Logging**: Track who accesses student submissions

---

## Architecture

### Repository Pattern
Analytics endpoints use the `IExamRepository` interface:
- `GetAllSubmissionsByExamAsync()`: Retrieves all submissions for an exam
- `GetMcqSubmissionAsync()`: Retrieves MCQ submission
- `GetWrittenSubmissionByExamAndStudentAsync()`: Retrieves written submission
- `GetSubjectiveEvaluationsAsync()`: Retrieves AI evaluations
- `GetAllMcqSubmissionsByStudentAsync()`: Retrieves student's MCQ history
- `GetAllWrittenSubmissionsByStudentAsync()`: Retrieves student's written history

### Storage Services
- `IExamStorageService`: Retrieves exam metadata and structure
- `IExamRepository`: Handles submission and evaluation data

### Data Flow
```
Request → ExamAnalyticsController 
        → IExamStorageService (exam metadata)
        → IExamRepository (submission data)
        → DTO Mapping
        → Response
```

---

## Example: Building a Teacher Dashboard

```javascript
// Frontend code example (React/Vue/Angular)
async function loadExamDashboard(examId) {
  // 1. Get exam summary
  const summary = await fetch(`/api/exam/${examId}/summary`).then(r => r.json());
  
  // Display: Total submissions, average score, status breakdown
  renderSummaryCards(summary);
  
  // 2. Load submissions (first page)
  const submissions = await fetch(`/api/exam/${examId}/submissions?page=1&pageSize=20`)
    .then(r => r.json());
  
  // Display: Table of all students with scores
  renderSubmissionsTable(submissions);
  
  // 3. On row click, load detailed submission
  async function viewDetails(studentId) {
    const details = await fetch(`/api/exam/${examId}/submissions/${studentId}`)
      .then(r => r.json());
    
    // Display: Full answer sheet with evaluations
    renderDetailedView(details);
  }
}
```

---

## Changelog

### Version 1.0 (December 2025)
- Initial release of Exam Analytics API
- Four main endpoints: submissions list, submission details, student history, exam summary
- Support for pagination on list endpoints
- Comprehensive DTOs for all response types
- Unit tests with 95% coverage
- Integration test script provided

---

## Support

For issues or questions:
1. Check the unit tests for usage examples
2. Run `test-analytics-api.ps1` for integration testing
3. Review controller XML comments for detailed parameter descriptions
4. Check logs for error details (`Serilog` configured)

---

## Related Documentation

- [API Reference](./API_REFERENCE_UPDATED.md): Complete API documentation
- [Architecture](./ARCHITECTURE_UPDATED.md): System architecture overview
- [Database Setup](./DATABASE_SETUP_README.md): Database schema and setup
- [Deployment Guide](./DEPLOY-NOW.md): Production deployment instructions
