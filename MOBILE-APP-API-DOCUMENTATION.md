# Mobile App API Documentation

## Base URLs
- **Production:** `https://app-wlanqwy7vuwmu.azurewebsites.net`
- **Local Development:** `http://localhost:8080`

## Authentication
Currently using IP-based identification. Future versions will support JWT tokens.

---

## 1. Chat API

### 1.1 Send Chat Message
**Endpoint:** `POST /api/chat`

**Request:**
```json
{
  "question": "What subjects are available?",
  "sessionId": "optional-uuid-for-conversation-continuity"
}
```

**Response (Success):**
```json
{
  "status": "success",
  "sessionId": "912408cc-2dd0-46f7-a111-65c0736b6543",
  "question": "What subjects are available?",
  "reply": "Based on the uploaded materials, the following subjects are available: Mathematics, Science, English...",
  "timestamp": "2025-11-22T10:54:33.0000000Z"
}
```

**Response (Error):**
```json
{
  "status": "error",
  "sessionId": "912408cc-2dd0-46f7-a111-65c0736b6543",
  "reply": "⚠️ Oops! I had a small hiccup. Try again, and I'll help you step by step! 😊",
  "error": "Error message here",
  "stackTrace": ["Line 1", "Line 2", ...]
}
```

---

### 1.2 Get Chat History
**Endpoint:** `GET /api/chat/history?sessionId={sessionId}&limit=10`

**Parameters:**
- `sessionId` (required): Session UUID
- `limit` (optional): Number of messages to retrieve (default: 10)

**Response:**
```json
{
  "status": "success",
  "sessionId": "912408cc-2dd0-46f7-a111-65c0736b6543",
  "count": 5,
  "messages": [
    {
      "id": 123,
      "message": "What is photosynthesis?",
      "reply": "Photosynthesis is the process...",
      "timestamp": "2025-11-22T10:54:33.0000000Z",
      "contextCount": 3
    }
  ]
}
```

---

### 1.3 Get All Chat Sessions
**Endpoint:** `GET /api/chat/sessions?limit=20`

**Parameters:**
- `limit` (optional): Number of sessions (default: 20)

**Response:**
```json
{
  "status": "success",
  "count": 3,
  "sessions": [
    {
      "sessionId": "session-uuid-1",
      "lastMessage": "What is algebra?",
      "timestamp": "2025-11-22T10:54:33.0000000Z"
    }
  ]
}
```

---

### 1.4 Get Most Recent Session
**Endpoint:** `GET /api/chat/most-recent-session`

**Response:**
```json
{
  "status": "success",
  "sessionId": "912408cc-2dd0-46f7-a111-65c0736b6543"
}
```

---

## 2. Exam API

### 2.1 Create Exam Template (Teacher/Admin Only)
**Endpoint:** `POST /api/exam/templates`

**Request:**
```json
{
  "name": "Mathematics Mid-Term",
  "subject": "Mathematics",
  "chapter": "Algebra",
  "totalQuestions": 20,
  "durationMinutes": 60,
  "adaptiveEnabled": true
}
```

**Response:**
```json
{
  "id": 1,
  "name": "Mathematics Mid-Term",
  "subject": "Mathematics",
  "chapter": "Algebra",
  "totalQuestions": 20,
  "durationMinutes": 60,
  "adaptiveEnabled": true,
  "createdAt": "2025-11-22T10:54:33.0000000Z"
}
```

---

### 2.2 Start Exam
**Endpoint:** `POST /api/exam/start`

**Request:**
```json
{
  "studentId": "student-123",
  "examTemplateId": 1
}
```

**Response:**
```json
{
  "attemptId": 456,
  "template": {
    "id": 1,
    "name": "Mathematics Mid-Term",
    "subject": "Mathematics",
    "chapter": "Algebra",
    "totalQuestions": 20,
    "durationMinutes": 60,
    "adaptiveEnabled": true,
    "createdAt": "2025-11-22T10:54:33.0000000Z"
  },
  "firstQuestion": {
    "id": 789,
    "subject": "Mathematics",
    "chapter": "Algebra",
    "topic": "Linear Equations",
    "text": "Solve for x: 2x + 5 = 15",
    "type": "MultipleChoice",
    "difficulty": "Easy",
    "options": [
      {
        "id": 1,
        "optionText": "x = 5"
      },
      {
        "id": 2,
        "optionText": "x = 10"
      },
      {
        "id": 3,
        "optionText": "x = 7.5"
      },
      {
        "id": 4,
        "optionText": "x = 2.5"
      }
    ]
  }
}
```

---

### 2.3 Submit Answer
**Endpoint:** `POST /api/exam/{attemptId}/answer`

**URL Parameters:**
- `attemptId`: Exam attempt ID

**Request:**
```json
{
  "questionId": 789,
  "selectedOptionId": 1,
  "timeTakenSeconds": 45
}
```

**Response:**
```json
{
  "isCorrect": true,
  "isCompleted": false,
  "nextQuestion": {
    "id": 790,
    "subject": "Mathematics",
    "chapter": "Algebra",
    "topic": "Quadratic Equations",
    "text": "Find the roots of x² - 5x + 6 = 0",
    "type": "MultipleChoice",
    "difficulty": "Medium",
    "options": [
      {
        "id": 5,
        "optionText": "x = 2, 3"
      },
      {
        "id": 6,
        "optionText": "x = 1, 6"
      },
      {
        "id": 7,
        "optionText": "x = -2, -3"
      },
      {
        "id": 8,
        "optionText": "x = 0, 5"
      }
    ]
  },
  "currentStats": {
    "answeredCount": 1,
    "correctCount": 1,
    "wrongCount": 0,
    "currentAccuracy": 100.0
  }
}
```

**Response (Exam Completed):**
```json
{
  "isCorrect": false,
  "isCompleted": true,
  "nextQuestion": null,
  "currentStats": {
    "answeredCount": 20,
    "correctCount": 15,
    "wrongCount": 5,
    "currentAccuracy": 75.0
  }
}
```

---

### 2.4 Get Exam Summary
**Endpoint:** `GET /api/exam/{attemptId}/summary`

**URL Parameters:**
- `attemptId`: Exam attempt ID

**Response:**
```json
{
  "attemptId": 456,
  "studentId": "student-123",
  "template": {
    "id": 1,
    "name": "Mathematics Mid-Term",
    "subject": "Mathematics",
    "chapter": "Algebra",
    "totalQuestions": 20,
    "durationMinutes": 60,
    "adaptiveEnabled": true,
    "createdAt": "2025-11-22T10:54:33.0000000Z"
  },
  "scorePercent": 75.0,
  "correctCount": 15,
  "wrongCount": 5,
  "totalQuestions": 20,
  "startedAt": "2025-11-22T10:00:00.0000000Z",
  "completedAt": "2025-11-22T10:45:00.0000000Z",
  "status": "Completed",
  "perDifficultyStats": {
    "Easy": {
      "totalQuestions": 5,
      "correctAnswers": 5,
      "accuracy": 100.0
    },
    "Medium": {
      "totalQuestions": 10,
      "correctAnswers": 8,
      "accuracy": 80.0
    },
    "Hard": {
      "totalQuestions": 5,
      "correctAnswers": 2,
      "accuracy": 40.0
    }
  }
}
```

---

### 2.5 Get Exam History
**Endpoint:** `GET /api/exam/history?studentId={studentId}`

**Parameters:**
- `studentId` (required): Student identifier

**Response:**
```json
[
  {
    "attemptId": 456,
    "examName": "Mathematics Mid-Term",
    "subject": "Mathematics",
    "chapter": "Algebra",
    "scorePercent": 75.0,
    "correctCount": 15,
    "wrongCount": 5,
    "status": "Completed",
    "startedAt": "2025-11-22T10:00:00.0000000Z",
    "completedAt": "2025-11-22T10:45:00.0000000Z"
  },
  {
    "attemptId": 455,
    "examName": "Science Quiz",
    "subject": "Science",
    "chapter": "Physics",
    "scorePercent": 85.0,
    "correctCount": 17,
    "wrongCount": 3,
    "status": "Completed",
    "startedAt": "2025-11-21T14:00:00.0000000Z",
    "completedAt": "2025-11-21T14:30:00.0000000Z"
  }
]
```

---

## 3. File Upload API

### 3.1 Upload Study Material
**Endpoint:** `POST /api/file/upload`

**Content-Type:** `multipart/form-data`

**Form Data:**
```
file: [PDF/DOCX file]
subject: "Mathematics"
grade: "Grade 10"
chapter: "Algebra"
```

**Response:**
```json
{
  "status": "success",
  "message": "File uploaded successfully. Processing will begin automatically.",
  "fileId": 123,
  "fileName": "mathematics-chapter-5.pdf",
  "blobUrl": "https://storage.blob.core.windows.net/documents/Grade%2010/Mathematics/Algebra/mathematics-chapter-5.pdf",
  "uploadedAt": "2025-11-22T10:54:33.0000000Z",
  "note": "Azure Functions will process this file and generate embeddings automatically."
}
```

---

### 3.2 Get Upload Status
**Endpoint:** `GET /api/file/status/{fileId}`

**URL Parameters:**
- `fileId`: File ID from upload response

**Response:**
```json
{
  "status": "success",
  "fileId": 123,
  "fileName": "mathematics-chapter-5.pdf",
  "uploadedAt": "2025-11-22T10:54:33.0000000Z",
  "processingStatus": "Completed",
  "chunksCreated": 45,
  "embeddingsCreated": 45,
  "totalChunks": 45,
  "isComplete": true
}
```

**Processing Status Values:**
- `Pending`: Waiting for processing
- `Processing`: Currently being chunked and embedded
- `Completed`: Ready for use
- `Failed`: Error during processing

---

### 3.3 List Uploaded Files
**Endpoint:** `GET /api/file/list?limit=50`

**Parameters:**
- `limit` (optional): Number of files (default: 50)

**Response:**
```json
{
  "status": "success",
  "count": 3,
  "files": [
    {
      "id": 123,
      "fileName": "mathematics-chapter-5.pdf",
      "blobUrl": "https://storage.../mathematics-chapter-5.pdf",
      "uploadedAt": "2025-11-22T10:54:33.0000000Z",
      "subject": "Mathematics",
      "grade": "Grade 10",
      "chapter": "Algebra",
      "status": "Completed",
      "totalChunks": 45
    }
  ]
}
```

---

## 4. Study Notes API

### 4.1 Generate Study Notes
**Endpoint:** `POST /api/notes/generate`

**Request:**
```json
{
  "topic": "Photosynthesis",
  "subject": "Biology",
  "grade": "Grade 10",
  "chapter": "Plant Biology"
}
```

**Response:**
```json
{
  "status": "success",
  "noteId": 789,
  "topic": "Photosynthesis",
  "notes": "# Photosynthesis\n\nPhotosynthesis is the process by which plants...\n\n## Key Points:\n1. Occurs in chloroplasts\n2. Requires sunlight, water, and CO2\n3. Produces glucose and oxygen\n\n## Chemical Equation:\n6CO2 + 6H2O + light → C6H12O6 + 6O2",
  "subject": "Biology",
  "grade": "Grade 10",
  "chapter": "Plant Biology",
  "createdAt": "2025-11-22T10:54:33.0000000Z"
}
```

---

### 4.2 Get Study Notes List
**Endpoint:** `GET /api/notes?limit=20`

**Parameters:**
- `limit` (optional): Number of notes (default: 20)

**Response:**
```json
{
  "status": "success",
  "count": 3,
  "notes": [
    {
      "id": 789,
      "topic": "Photosynthesis",
      "subject": "Biology",
      "grade": "Grade 10",
      "chapter": "Plant Biology",
      "createdAt": "2025-11-22T10:54:33.0000000Z",
      "rating": 5,
      "preview": "# Photosynthesis\n\nPhotosynthesis is the process by which plants..."
    }
  ]
}
```

---

### 4.3 Get Study Note by ID
**Endpoint:** `GET /api/notes/{id}`

**URL Parameters:**
- `id`: Note ID

**Response:**
```json
{
  "status": "success",
  "note": {
    "id": 789,
    "topic": "Photosynthesis",
    "notes": "# Photosynthesis\n\nComplete markdown content here...",
    "subject": "Biology",
    "grade": "Grade 10",
    "chapter": "Plant Biology",
    "createdAt": "2025-11-22T10:54:33.0000000Z",
    "rating": 5
  }
}
```

---

### 4.4 Rate Study Note
**Endpoint:** `POST /api/notes/{id}/rate`

**URL Parameters:**
- `id`: Note ID

**Request:**
```json
{
  "rating": 5
}
```

**Response:**
```json
{
  "status": "success",
  "message": "Rating saved successfully."
}
```

**Validation:**
- Rating must be between 1 and 5

---

### 4.5 Update Study Note
**Endpoint:** `PUT /api/notes/{id}`

**URL Parameters:**
- `id`: Note ID

**Request:**
```json
{
  "content": "Updated markdown content here..."
}
```

**Response:**
```json
{
  "status": "success",
  "message": "Note updated successfully.",
  "note": {
    "id": 789,
    "topic": "Photosynthesis",
    "notes": "Updated markdown content here...",
    "updatedAt": "2025-11-22T11:00:00.0000000Z"
  }
}
```

---

### 4.6 Share Study Note
**Endpoint:** `POST /api/notes/{id}/share`

**URL Parameters:**
- `id`: Note ID

**Response:**
```json
{
  "status": "success",
  "message": "Note shared successfully.",
  "shareToken": "abc123def456",
  "shareUrl": "https://app-wlanqwy7vuwmu.azurewebsites.net/api/notes/shared/abc123def456"
}
```

---

### 4.7 Unshare Study Note
**Endpoint:** `POST /api/notes/{id}/unshare`

**URL Parameters:**
- `id`: Note ID

**Response:**
```json
{
  "status": "success",
  "message": "Note unshared successfully."
}
```

---

### 4.8 Get Shared Study Note
**Endpoint:** `GET /api/notes/shared/{token}`

**URL Parameters:**
- `token`: Share token

**Response:**
```json
{
  "status": "success",
  "note": {
    "id": 789,
    "topic": "Photosynthesis",
    "notes": "Complete markdown content...",
    "subject": "Biology",
    "grade": "Grade 10",
    "chapter": "Plant Biology",
    "createdAt": "2025-11-22T10:54:33.0000000Z",
    "updatedAt": "2025-11-22T11:00:00.0000000Z",
    "rating": 5
  }
}
```

---

## Error Handling

### Standard Error Response Format
```json
{
  "status": "error",
  "message": "Human-readable error message",
  "details": "Technical details (optional)",
  "debug": "Debug information (development only)"
}
```

### HTTP Status Codes
- `200 OK`: Successful request
- `400 Bad Request`: Invalid request parameters
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error

---

## Mobile App Implementation Tips

### 1. Session Management
```dart
// Store session ID locally
String? currentSessionId;

Future<void> sendMessage(String question) async {
  final response = await http.post(
    Uri.parse('$baseUrl/api/chat'),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode({
      'question': question,
      'sessionId': currentSessionId,
    }),
  );
  
  final data = jsonDecode(response.body);
  currentSessionId = data['sessionId']; // Save for next message
}
```

### 2. File Upload
```dart
Future<void> uploadFile(File file) async {
  var request = http.MultipartRequest(
    'POST',
    Uri.parse('$baseUrl/api/file/upload'),
  );
  
  request.files.add(await http.MultipartFile.fromPath('file', file.path));
  request.fields['subject'] = 'Mathematics';
  request.fields['grade'] = 'Grade 10';
  request.fields['chapter'] = 'Algebra';
  
  var response = await request.send();
  var responseData = await response.stream.bytesToString();
  var json = jsonDecode(responseData);
  
  // Check processing status
  int fileId = json['fileId'];
  await checkUploadStatus(fileId);
}
```

### 3. Exam Flow
```dart
// 1. Start exam
final startResponse = await startExam('student-123', examTemplateId: 1);
int attemptId = startResponse['attemptId'];

// 2. Answer questions
for (var question in questions) {
  final answerResponse = await submitAnswer(attemptId, questionId, optionId);
  
  if (answerResponse['isCompleted']) {
    // Show summary
    final summary = await getExamSummary(attemptId);
    showResults(summary);
    break;
  }
  
  // Show next question
  showQuestion(answerResponse['nextQuestion']);
}
```

### 4. Polling for Upload Status
```dart
Future<void> pollUploadStatus(int fileId) async {
  Timer.periodic(Duration(seconds: 5), (timer) async {
    final status = await getUploadStatus(fileId);
    
    if (status['isComplete']) {
      timer.cancel();
      showSuccess('File processed successfully!');
    } else {
      updateProgress(
        status['embeddingsCreated'], 
        status['totalChunks']
      );
    }
  });
}
```

---

## Testing Endpoints

Use these test endpoints to verify connectivity:

- `GET /api/chat/test` → Returns: `✅ Chat endpoint is working!`
- `GET /api/notes/test` → Returns: `✅ Notes endpoint is working!`
- `GET /api/chat/test-ai` → Tests Azure OpenAI connection

---

## Rate Limiting & Best Practices

1. **Retry Logic**: Implement exponential backoff for failed requests
2. **Timeouts**: Set appropriate timeouts (30s for chat, 60s for file uploads)
3. **Caching**: Cache exam templates and study materials locally
4. **Offline Support**: Queue messages and sync when connection restored
5. **Progress Indicators**: Show loading states for AI generation (can take 5-10s)

---

## Support

For integration issues or questions:
- GitHub: https://github.com/sadabyakod/school-ai-chatbot
- API Base URL: https://app-wlanqwy7vuwmu.azurewebsites.net
