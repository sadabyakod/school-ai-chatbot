# Exam System API Documentation

## Overview
Complete exam system with adaptive difficulty that adjusts based on student performance in real-time.

## Architecture

### Entities
1. **Question** - Exam questions with subject, chapter, difficulty
2. **QuestionOption** - Multiple choice options with correct answer flag
3. **ExamTemplate** - Reusable exam configurations
4. **ExamAttempt** - Student exam sessions with scores
5. **ExamAnswer** - Individual question responses

### Service Layer
**ExamService** provides:
- Template management
- Exam lifecycle (start, answer submission, completion)
- **Adaptive difficulty algorithm** - adjusts based on last 5 answers
- Statistics calculation per difficulty level

### API Controller
**ExamController** exposes RESTful endpoints with DTOs, validation, and logging.

---

## API Endpoints

### 1️⃣ Create Exam Template
**POST** `/api/exams/templates`

Creates a reusable exam configuration.

**Request Body:**
```json
{
  "name": "Math Chapter 1 Test",
  "subject": "Mathematics",
  "chapter": "Algebra",
  "totalQuestions": 10,
  "durationMinutes": 30,
  "adaptiveEnabled": true
}
```

**Response (200 OK):**
```json
{
  "id": 1,
  "name": "Math Chapter 1 Test",
  "subject": "Mathematics",
  "chapter": "Algebra",
  "totalQuestions": 10,
  "durationMinutes": 30,
  "adaptiveEnabled": true,
  "createdAt": "2025-11-21T10:00:00Z"
}
```

**Validation:**
- `name` is required
- `subject` is required
- `totalQuestions` must be > 0
- `durationMinutes` must be > 0

---

### 2️⃣ Start Exam
**POST** `/api/exams/start`

Begins a new exam attempt for a student.

**Request Body:**
```json
{
  "studentId": "student123",
  "examTemplateId": 1
}
```

**Response (200 OK):**
```json
{
  "attemptId": 5,
  "template": {
    "id": 1,
    "name": "Math Chapter 1 Test",
    "subject": "Mathematics",
    "chapter": "Algebra",
    "totalQuestions": 10,
    "durationMinutes": 30,
    "adaptiveEnabled": true,
    "createdAt": "2025-11-21T10:00:00Z"
  },
  "firstQuestion": {
    "id": 42,
    "subject": "Mathematics",
    "chapter": "Algebra",
    "topic": "Linear Equations",
    "text": "Solve for x: 3x + 5 = 14",
    "type": "MultipleChoice",
    "difficulty": "Medium",
    "options": [
      { "id": 101, "optionText": "x = 2" },
      { "id": 102, "optionText": "x = 3" },
      { "id": 103, "optionText": "x = 4" }
    ]
  }
}
```

**Behavior:**
- Creates `ExamAttempt` with status "InProgress"
- Returns first question at **Medium** difficulty
- Starts adaptive difficulty tracking

**Errors:**
- `400` - Invalid studentId or templateId
- `404` - Template not found

---

### 3️⃣ Submit Answer
**POST** `/api/exams/{attemptId}/answer`

Submits answer and receives next question with adaptive difficulty.

**Request Body:**
```json
{
  "questionId": 42,
  "selectedOptionId": 102,
  "timeTakenSeconds": 45
}
```

**Response (200 OK):**
```json
{
  "isCorrect": true,
  "isCompleted": false,
  "nextQuestion": {
    "id": 45,
    "subject": "Mathematics",
    "chapter": "Algebra",
    "topic": "Calculus",
    "text": "Find derivative of x² + 3x + 2",
    "type": "MultipleChoice",
    "difficulty": "Hard",
    "options": [
      { "id": 110, "optionText": "2x" },
      { "id": 111, "optionText": "2x + 3" },
      { "id": 112, "optionText": "x² + 3" }
    ]
  },
  "currentStats": {
    "answeredCount": 3,
    "correctCount": 2,
    "wrongCount": 1,
    "currentAccuracy": 66.67
  }
}
```

**Adaptive Algorithm:**
Analyzes last 5 answers to determine next question difficulty:
- **>80% accuracy** → Hard questions
- **50-80% accuracy** → Medium questions  
- **<50% accuracy** → Easy questions

**Behavior:**
- Saves answer to database
- Validates correctness against `QuestionOption.IsCorrect`
- Calculates next difficulty using adaptive algorithm
- Returns next question (or `null` if exam complete)
- Auto-completes exam when reaching `TotalQuestions`

**Errors:**
- `400` - Invalid attemptId or questionId
- `404` - Attempt not found
- `400` - Exam not in progress (already completed/abandoned)

---

### 4️⃣ Get Exam Summary
**GET** `/api/exams/{attemptId}/summary`

Retrieves complete exam results with statistics.

**Response (200 OK):**
```json
{
  "attemptId": 5,
  "studentId": "student123",
  "template": {
    "id": 1,
    "name": "Math Chapter 1 Test",
    "subject": "Mathematics",
    "chapter": "Algebra",
    "totalQuestions": 10,
    "durationMinutes": 30,
    "adaptiveEnabled": true,
    "createdAt": "2025-11-21T10:00:00Z"
  },
  "scorePercent": 75.00,
  "correctCount": 6,
  "wrongCount": 2,
  "totalQuestions": 8,
  "startedAt": "2025-11-21T10:05:00Z",
  "completedAt": "2025-11-21T10:20:00Z",
  "status": "Completed",
  "perDifficultyStats": {
    "Easy": {
      "totalQuestions": 2,
      "correctAnswers": 2,
      "accuracy": 100.00
    },
    "Medium": {
      "totalQuestions": 3,
      "correctAnswers": 2,
      "accuracy": 66.67
    },
    "Hard": {
      "totalQuestions": 3,
      "correctAnswers": 2,
      "accuracy": 66.67
    }
  }
}
```

**Behavior:**
- Auto-completes exam if status is "InProgress"
- Calculates per-difficulty statistics
- Shows breakdown of performance by difficulty level

**Errors:**
- `400` - Invalid attemptId
- `404` - Attempt not found

---

### 5️⃣ Get Exam History
**GET** `/api/exams/history?studentId={studentId}`

Retrieves student's recent exam attempts (last 20).

**Query Parameters:**
- `studentId` (required) - Student identifier

**Response (200 OK):**
```json
[
  {
    "attemptId": 8,
    "examName": "Math Chapter 1 Test",
    "subject": "Mathematics",
    "chapter": "Algebra",
    "scorePercent": 85.00,
    "correctCount": 8,
    "wrongCount": 2,
    "status": "Completed",
    "startedAt": "2025-11-21T14:00:00Z",
    "completedAt": "2025-11-21T14:25:00Z"
  },
  {
    "attemptId": 5,
    "examName": "Math Chapter 1 Test",
    "subject": "Mathematics",
    "chapter": "Algebra",
    "scorePercent": 75.00,
    "correctCount": 6,
    "wrongCount": 2,
    "status": "Completed",
    "startedAt": "2025-11-21T10:05:00Z",
    "completedAt": "2025-11-21T10:20:00Z"
  }
]
```

**Behavior:**
- Returns last 20 attempts ordered by most recent
- Includes completed, in-progress, and abandoned exams

**Errors:**
- `400` - studentId is required

---

## Data Flow

### Typical Exam Session

```
1. Create Template
   POST /api/exams/templates
   → ExamTemplate created

2. Start Exam  
   POST /api/exams/start
   → ExamAttempt created (status: InProgress)
   → Returns first question (Medium difficulty)

3. Submit Answers (repeat)
   POST /api/exams/{attemptId}/answer
   → ExamAnswer saved
   → Adaptive algorithm calculates next difficulty
   → Returns next question
   
   Adaptive Logic:
   - After 5 answers: analyze accuracy
   - High accuracy (>80%) → Hard questions
   - Medium accuracy (50-80%) → Medium questions  
   - Low accuracy (<50%) → Easy questions

4. Auto-Complete
   → When answeredCount >= TotalQuestions
   → Status changed to "Completed"
   → Final score calculated

5. View Results
   GET /api/exams/{attemptId}/summary
   → Returns full statistics
   → Per-difficulty breakdown
```

---

## Database Schema

### Questions Table
```sql
Id (PK), Subject, Chapter, Topic, Text, Explanation, 
Difficulty (Easy/Medium/Hard), Type, SourceFileId, CreatedAt
```

### QuestionOptions Table
```sql
Id (PK), QuestionId (FK), OptionText, IsCorrect
```

### ExamTemplates Table
```sql
Id (PK), Name, Subject, Chapter, TotalQuestions, 
DurationMinutes, AdaptiveEnabled, CreatedAt, CreatedBy
```

### ExamAttempts Table
```sql
Id (PK), StudentId, ExamTemplateId (FK), StartedAt, CompletedAt,
ScorePercent, CorrectCount, WrongCount, Status (InProgress/Completed/Abandoned)
```

### ExamAnswers Table
```sql
Id (PK), ExamAttemptId (FK), QuestionId (FK), 
SelectedOptionId, IsCorrect, TimeTakenSeconds
```

---

## Testing

### 1. Setup Database
```powershell
# Run sample questions script
sqlcmd -S your-server.database.windows.net -d school-ai-chatbot -U username -P password -i sample-exam-questions.sql
```

### 2. Test API Endpoints
```powershell
# Run automated test suite
.\test-exam-endpoints.ps1
```

### 3. Manual Testing with curl/Postman
```bash
# Create template
curl -X POST http://localhost:8080/api/exams/templates \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Exam","subject":"Math","chapter":"Algebra","totalQuestions":5,"durationMinutes":15,"adaptiveEnabled":true}'

# Start exam
curl -X POST http://localhost:8080/api/exams/start \
  -H "Content-Type: application/json" \
  -d '{"studentId":"test123","examTemplateId":1}'
```

---

## Features

✅ **Adaptive Difficulty**  
   Real-time adjustment based on student performance

✅ **Per-Difficulty Statistics**  
   Breakdown showing performance at each difficulty level

✅ **Clean DTOs**  
   No EF entities exposed in API responses

✅ **Comprehensive Validation**  
   Request validation with meaningful error messages

✅ **Efficient Queries**  
   Uses `Include()` for eager loading, indexed columns

✅ **Proper Logging**  
   Consistent logging pattern matching other controllers

✅ **Auto-Completion**  
   Automatically completes exam when question limit reached

✅ **Exam History**  
   Track student progress over multiple attempts

---

## Next Steps

### Frontend Integration
1. Create React components for exam UI
2. Add timer functionality
3. Show adaptive difficulty indicator
4. Display real-time statistics

### Question Generation
1. Extract questions from uploaded files using Azure OpenAI
2. Automatically classify difficulty
3. Generate explanations and options

### Analytics
1. Student performance dashboard
2. Question difficulty calibration
3. Time analysis per question/difficulty

### Advanced Features
1. Pause/resume exam functionality
2. Review mode (show correct answers after completion)
3. Question bookmarking
4. Randomize option order
5. Export results to PDF

---

## Error Codes

| Code | Description |
|------|-------------|
| 200  | Success |
| 400  | Bad Request - validation error |
| 404  | Not Found - resource doesn't exist |
| 500  | Internal Server Error |

---

## Configuration

No additional configuration required. The exam system uses:
- Existing `AppDbContext` for database access
- Registered as scoped service in DI container
- Same connection string as other features
