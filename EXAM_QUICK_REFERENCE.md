# 🚀 Exam System - Quick Reference

## Base URL
```
http://localhost:8080/api/exams
```

---

## Endpoints

### 1️⃣ Create Template
```http
POST /api/exams/templates
Content-Type: application/json

{
  "name": "Math Chapter 1 Test",
  "subject": "Mathematics",
  "chapter": "Algebra",
  "totalQuestions": 10,
  "durationMinutes": 30,
  "adaptiveEnabled": true
}
```

### 2️⃣ Start Exam
```http
POST /api/exams/start
Content-Type: application/json

{
  "studentId": "student123",
  "examTemplateId": 1
}
```

### 3️⃣ Submit Answer
```http
POST /api/exams/{attemptId}/answer
Content-Type: application/json

{
  "questionId": 42,
  "selectedOptionId": 102,
  "timeTakenSeconds": 45
}
```

### 4️⃣ Get Summary
```http
GET /api/exams/{attemptId}/summary
```

### 5️⃣ Get History
```http
GET /api/exams/history?studentId=student123
```

---

## Adaptive Algorithm

```
Last 5 Answers Analysis:
  >80% correct  → Hard questions
  50-80% correct → Medium questions
  <50% correct  → Easy questions
```

---

## Testing

### Insert Sample Data
```powershell
sqlcmd -S school-chatbot-sql-10271900.database.windows.net `
  -d school-ai-chatbot -U sqladmin -P YourPassword `
  -i sample-exam-questions.sql
```

### Run Tests
```powershell
.\test-exam-endpoints.ps1
```

---

## Response Examples

### Start Exam Response
```json
{
  "attemptId": 5,
  "template": { ... },
  "firstQuestion": {
    "id": 42,
    "text": "Solve for x: 3x + 5 = 14",
    "difficulty": "Medium",
    "options": [...]
  }
}
```

### Submit Answer Response
```json
{
  "isCorrect": true,
  "isCompleted": false,
  "nextQuestion": { ... },
  "currentStats": {
    "answeredCount": 3,
    "correctCount": 2,
    "wrongCount": 1,
    "currentAccuracy": 66.67
  }
}
```

### Summary Response
```json
{
  "attemptId": 5,
  "scorePercent": 75.00,
  "correctCount": 6,
  "wrongCount": 2,
  "perDifficultyStats": {
    "Easy": { "accuracy": 100.00 },
    "Medium": { "accuracy": 66.67 },
    "Hard": { "accuracy": 66.67 }
  }
}
```

---

## Files Created

| File | Purpose |
|------|---------|
| `Controllers/ExamController.cs` | 5 API endpoints |
| `Features/Exams/ExamDtos.cs` | Request/response DTOs |
| `test-exam-endpoints.ps1` | Automated testing |
| `sample-exam-questions.sql` | Test data (10 questions) |
| `EXAM_SYSTEM_README.md` | Full documentation |
| `EXAM_SYSTEM_COMPLETE.md` | Implementation summary |

---

## Status
✅ Backend running: http://localhost:8080  
✅ 5 endpoints operational  
✅ 5 database tables created  
✅ Adaptive algorithm implemented  
✅ Ready for testing & frontend integration  
