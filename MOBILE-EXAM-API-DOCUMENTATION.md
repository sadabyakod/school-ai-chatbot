# Mobile Exam API Documentation

Complete API documentation for implementing exam functionality in mobile applications.

## Base URL
```
Production: https://app-wlanqwy7vuwmu-hpbwbfgqbybqg7dp.centralindia-01.azurewebsites.net
Local: http://localhost:8080
```

---

## 1. Create Exam Template

**Endpoint:** `POST /api/exams/templates`

**Description:** Create a new exam template with specific subject, chapter, and question count.

**Headers:**
```json
{
  "Content-Type": "application/json",
  "Authorization": "Bearer <JWT_TOKEN>"
}
```

**Request Body:**
```json
{
  "name": "Grade 10 Mathematics - Algebra Test",
  "subject": "Mathematics",
  "chapter": "Algebra",
  "totalQuestions": 10,
  "durationMinutes": 30,
  "adaptiveEnabled": true
}
```

**Request Fields:**
- `name` (string, required): Exam template name
- `subject` (string, required): Subject name (e.g., "Mathematics", "Science")
- `chapter` (string, optional): Chapter or topic name
- `totalQuestions` (integer, required): Number of questions (must be > 0)
- `durationMinutes` (integer, required): Duration in minutes (must be > 0)
- `adaptiveEnabled` (boolean, optional): Enable adaptive difficulty (default: false)

**Response (200 OK):**
```json
{
  "id": 1,
  "name": "Grade 10 Mathematics - Algebra Test",
  "subject": "Mathematics",
  "chapter": "Algebra",
  "totalQuestions": 10,
  "durationMinutes": 30,
  "adaptiveEnabled": true,
  "createdAt": "2025-12-07T10:30:00Z"
}
```

**Response Fields:**
- `id` (integer): Unique template identifier
- `name` (string): Template name
- `subject` (string): Subject name
- `chapter` (string): Chapter name
- `totalQuestions` (integer): Total number of questions
- `durationMinutes` (integer): Duration in minutes
- `adaptiveEnabled` (boolean): Whether adaptive difficulty is enabled
- `createdAt` (string): ISO 8601 timestamp

**Error Response (400 Bad Request):**
```json
{
  "error": "Exam name is required."
}
```

---

## 2. Start Exam

**Endpoint:** `POST /api/exams/start`

**Description:** Start a new exam attempt for a student.

**Headers:**
```json
{
  "Content-Type": "application/json",
  "Authorization": "Bearer <JWT_TOKEN>"
}
```

**Request Body:**
```json
{
  "studentId": "student123",
  "examTemplateId": 1
}
```

**Request Fields:**
- `studentId` (string, required): Unique student identifier
- `examTemplateId` (integer, required): ID of the exam template to use

**Response (200 OK):**
```json
{
  "attemptId": 456,
  "template": {
    "id": 1,
    "name": "Grade 10 Mathematics - Algebra Test",
    "subject": "Mathematics",
    "chapter": "Algebra",
    "totalQuestions": 10,
    "durationMinutes": 30,
    "adaptiveEnabled": true,
    "createdAt": "2025-12-07T10:30:00Z"
  },
  "firstQuestion": {
    "id": 101,
    "subject": "Mathematics",
    "chapter": "Algebra",
    "topic": "Linear Equations",
    "text": "What is the value of x in the equation 2x + 5 = 15?",
    "type": "MCQ",
    "difficulty": "Easy",
    "options": [
      {
        "id": 1001,
        "optionText": "5"
      },
      {
        "id": 1002,
        "optionText": "10"
      },
      {
        "id": 1003,
        "optionText": "15"
      },
      {
        "id": 1004,
        "optionText": "20"
      }
    ]
  }
}
```

**Response Fields:**
- `attemptId` (integer): Unique exam attempt identifier
- `template` (object): Exam template details
- `firstQuestion` (object): First question to display
  - `id` (integer): Question ID
  - `subject` (string): Subject name
  - `chapter` (string): Chapter name
  - `topic` (string): Specific topic
  - `text` (string): Question text
  - `type` (string): Question type (MCQ, TrueFalse, etc.)
  - `difficulty` (string): Easy, Medium, or Hard
  - `options` (array): Answer options
    - `id` (integer): Option ID
    - `optionText` (string): Option text

**Error Response (404 Not Found):**
```json
{
  "error": "Exam template 1 not found."
}
```

---

## 3. Submit Answer

**Endpoint:** `POST /api/exams/{attemptId}/answer`

**Description:** Submit an answer to the current question and receive the next question.

**Headers:**
```json
{
  "Content-Type": "application/json",
  "Authorization": "Bearer <JWT_TOKEN>"
}
```

**Path Parameters:**
- `attemptId` (integer, required): Exam attempt ID

**Request Body:**
```json
{
  "questionId": 101,
  "selectedOptionId": 1001,
  "timeTakenSeconds": 45
}
```

**Request Fields:**
- `questionId` (integer, required): ID of the question being answered
- `selectedOptionId` (integer, required): ID of the selected option
- `timeTakenSeconds` (integer, optional): Time spent on this question

**Response (200 OK):**
```json
{
  "isCorrect": true,
  "isCompleted": false,
  "nextQuestion": {
    "id": 102,
    "subject": "Mathematics",
    "chapter": "Algebra",
    "topic": "Quadratic Equations",
    "text": "Solve for x: x² - 5x + 6 = 0",
    "type": "MCQ",
    "difficulty": "Medium",
    "options": [
      {
        "id": 1005,
        "optionText": "x = 2 or x = 3"
      },
      {
        "id": 1006,
        "optionText": "x = 1 or x = 6"
      },
      {
        "id": 1007,
        "optionText": "x = -2 or x = -3"
      },
      {
        "id": 1008,
        "optionText": "x = 5 or x = 1"
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

**Response Fields:**
- `isCorrect` (boolean): Whether the answer was correct
- `isCompleted` (boolean): Whether this was the last question
- `nextQuestion` (object|null): Next question (null if exam completed)
- `currentStats` (object): Current performance statistics
  - `answeredCount` (integer): Total questions answered so far
  - `correctCount` (integer): Number of correct answers
  - `wrongCount` (integer): Number of wrong answers
  - `currentAccuracy` (decimal): Current accuracy percentage

**Response when exam is completed:**
```json
{
  "isCorrect": false,
  "isCompleted": true,
  "nextQuestion": null,
  "currentStats": {
    "answeredCount": 10,
    "correctCount": 8,
    "wrongCount": 2,
    "currentAccuracy": 80.0
  }
}
```

**Error Response (400 Bad Request):**
```json
{
  "error": "Exam attempt is not in progress. Status: Completed"
}
```

---

## 4. Get Exam Summary

**Endpoint:** `GET /api/exams/{attemptId}/summary`

**Description:** Get detailed summary and statistics for a completed exam.

**Headers:**
```json
{
  "Authorization": "Bearer <JWT_TOKEN>"
}
```

**Path Parameters:**
- `attemptId` (integer, required): Exam attempt ID

**Example Request:**
```
GET /api/exams/456/summary
```

**Response (200 OK):**
```json
{
  "attemptId": 456,
  "studentId": "student123",
  "template": {
    "id": 1,
    "name": "Grade 10 Mathematics - Algebra Test",
    "subject": "Mathematics",
    "chapter": "Algebra",
    "totalQuestions": 10,
    "durationMinutes": 30,
    "adaptiveEnabled": true,
    "createdAt": "2025-12-07T10:30:00Z"
  },
  "scorePercent": 80.0,
  "correctCount": 8,
  "wrongCount": 2,
  "totalQuestions": 10,
  "startedAt": "2025-12-07T11:00:00Z",
  "completedAt": "2025-12-07T11:25:00Z",
  "status": "Completed",
  "perDifficultyStats": {
    "Easy": {
      "totalQuestions": 3,
      "correctAnswers": 3,
      "accuracy": 100.0
    },
    "Medium": {
      "totalQuestions": 5,
      "correctAnswers": 4,
      "accuracy": 80.0
    },
    "Hard": {
      "totalQuestions": 2,
      "correctAnswers": 1,
      "accuracy": 50.0
    }
  }
}
```

**Response Fields:**
- `attemptId` (integer): Exam attempt ID
- `studentId` (string): Student identifier
- `template` (object): Exam template details
- `scorePercent` (decimal): Overall score percentage
- `correctCount` (integer): Number of correct answers
- `wrongCount` (integer): Number of wrong answers
- `totalQuestions` (integer): Total questions answered
- `startedAt` (string): ISO 8601 timestamp when exam started
- `completedAt` (string|null): ISO 8601 timestamp when exam completed
- `status` (string): "InProgress" or "Completed"
- `perDifficultyStats` (object): Performance breakdown by difficulty level
  - `Easy`, `Medium`, `Hard` (object):
    - `totalQuestions` (integer): Questions at this difficulty
    - `correctAnswers` (integer): Correct answers at this difficulty
    - `accuracy` (decimal): Accuracy percentage for this difficulty

---

## 5. Get Exam History

**Endpoint:** `GET /api/exams/history`

**Description:** Get list of all exam attempts for a student.

**Headers:**
```json
{
  "Authorization": "Bearer <JWT_TOKEN>"
}
```

**Query Parameters:**
- `studentId` (string, required): Student identifier

**Example Request:**
```
GET /api/exams/history?studentId=student123
```

**Response (200 OK):**
```json
[
  {
    "attemptId": 456,
    "examName": "Grade 10 Mathematics - Algebra Test",
    "subject": "Mathematics",
    "chapter": "Algebra",
    "scorePercent": 80.0,
    "correctCount": 8,
    "wrongCount": 2,
    "status": "Completed",
    "startedAt": "2025-12-07T11:00:00Z",
    "completedAt": "2025-12-07T11:25:00Z"
  },
  {
    "attemptId": 455,
    "examName": "Grade 10 Science - Physics Test",
    "subject": "Science",
    "chapter": "Physics",
    "scorePercent": 90.0,
    "correctCount": 9,
    "wrongCount": 1,
    "status": "Completed",
    "startedAt": "2025-12-06T14:00:00Z",
    "completedAt": "2025-12-06T14:20:00Z"
  }
]
```

**Response Fields (array of objects):**
- `attemptId` (integer): Exam attempt ID
- `examName` (string): Name of the exam
- `subject` (string): Subject name
- `chapter` (string): Chapter name
- `scorePercent` (decimal): Score percentage
- `correctCount` (integer): Correct answers
- `wrongCount` (integer): Wrong answers
- `status` (string): "InProgress" or "Completed"
- `startedAt` (string): ISO 8601 timestamp
- `completedAt` (string|null): ISO 8601 timestamp (null if in progress)

---

## Error Codes

All endpoints may return the following error responses:

**400 Bad Request:**
```json
{
  "error": "Valid attempt ID is required."
}
```

**401 Unauthorized:**
```json
{
  "error": "Unauthorized",
  "message": "Invalid or expired token"
}
```

**404 Not Found:**
```json
{
  "error": "Exam template 1 not found."
}
```

**500 Internal Server Error:**
```json
{
  "error": "Failed to start exam."
}
```

---

## Mobile Implementation Example (Flutter/Dart)

### Exam Service
```dart
import 'dart:convert';
import 'package:http/http.dart' as http;

class ExamService {
  final String baseUrl = 'https://app-wlanqwy7vuwmu-hpbwbfgqbybqg7dp.centralindia-01.azurewebsites.net';
  final String? token;

  ExamService({required this.token});

  Future<ExamTemplate> createTemplate(CreateTemplateRequest request) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/exams/templates'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: jsonEncode(request.toJson()),
    );

    if (response.statusCode == 200) {
      return ExamTemplate.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Failed to create template: ${response.body}');
    }
  }

  Future<StartExamResponse> startExam(String studentId, int templateId) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/exams/start'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: jsonEncode({
        'studentId': studentId,
        'examTemplateId': templateId,
      }),
    );

    if (response.statusCode == 200) {
      return StartExamResponse.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Failed to start exam: ${response.body}');
    }
  }

  Future<SubmitAnswerResponse> submitAnswer(
    int attemptId,
    int questionId,
    int selectedOptionId,
    int timeTakenSeconds,
  ) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/exams/$attemptId/answer'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: jsonEncode({
        'questionId': questionId,
        'selectedOptionId': selectedOptionId,
        'timeTakenSeconds': timeTakenSeconds,
      }),
    );

    if (response.statusCode == 200) {
      return SubmitAnswerResponse.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Failed to submit answer: ${response.body}');
    }
  }

  Future<ExamSummary> getExamSummary(int attemptId) async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/exams/$attemptId/summary'),
      headers: {
        'Authorization': 'Bearer $token',
      },
    );

    if (response.statusCode == 200) {
      return ExamSummary.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Failed to fetch exam summary: ${response.body}');
    }
  }

  Future<List<ExamHistory>> getExamHistory(String studentId) async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/exams/history?studentId=$studentId'),
      headers: {
        'Authorization': 'Bearer $token',
      },
    );

    if (response.statusCode == 200) {
      final List<dynamic> data = jsonDecode(response.body);
      return data.map((item) => ExamHistory.fromJson(item)).toList();
    } else {
      throw Exception('Failed to fetch exam history: ${response.body}');
    }
  }
}
```

### Data Models
```dart
class CreateTemplateRequest {
  final String name;
  final String subject;
  final String? chapter;
  final int totalQuestions;
  final int durationMinutes;
  final bool adaptiveEnabled;

  CreateTemplateRequest({
    required this.name,
    required this.subject,
    this.chapter,
    required this.totalQuestions,
    required this.durationMinutes,
    this.adaptiveEnabled = false,
  });

  Map<String, dynamic> toJson() => {
    'name': name,
    'subject': subject,
    if (chapter != null) 'chapter': chapter,
    'totalQuestions': totalQuestions,
    'durationMinutes': durationMinutes,
    'adaptiveEnabled': adaptiveEnabled,
  };
}

class ExamTemplate {
  final int id;
  final String name;
  final String subject;
  final String? chapter;
  final int totalQuestions;
  final int durationMinutes;
  final bool adaptiveEnabled;
  final DateTime createdAt;

  ExamTemplate({
    required this.id,
    required this.name,
    required this.subject,
    this.chapter,
    required this.totalQuestions,
    required this.durationMinutes,
    required this.adaptiveEnabled,
    required this.createdAt,
  });

  factory ExamTemplate.fromJson(Map<String, dynamic> json) => ExamTemplate(
    id: json['id'],
    name: json['name'],
    subject: json['subject'],
    chapter: json['chapter'],
    totalQuestions: json['totalQuestions'],
    durationMinutes: json['durationMinutes'],
    adaptiveEnabled: json['adaptiveEnabled'],
    createdAt: DateTime.parse(json['createdAt']),
  );
}

class StartExamResponse {
  final int attemptId;
  final ExamTemplate template;
  final Question? firstQuestion;

  StartExamResponse({
    required this.attemptId,
    required this.template,
    this.firstQuestion,
  });

  factory StartExamResponse.fromJson(Map<String, dynamic> json) => StartExamResponse(
    attemptId: json['attemptId'],
    template: ExamTemplate.fromJson(json['template']),
    firstQuestion: json['firstQuestion'] != null 
      ? Question.fromJson(json['firstQuestion']) 
      : null,
  );
}

class Question {
  final int id;
  final String subject;
  final String chapter;
  final String topic;
  final String text;
  final String type;
  final String difficulty;
  final List<QuestionOption> options;

  Question({
    required this.id,
    required this.subject,
    required this.chapter,
    required this.topic,
    required this.text,
    required this.type,
    required this.difficulty,
    required this.options,
  });

  factory Question.fromJson(Map<String, dynamic> json) => Question(
    id: json['id'],
    subject: json['subject'],
    chapter: json['chapter'],
    topic: json['topic'],
    text: json['text'],
    type: json['type'],
    difficulty: json['difficulty'],
    options: (json['options'] as List)
      .map((o) => QuestionOption.fromJson(o))
      .toList(),
  );
}

class QuestionOption {
  final int id;
  final String optionText;

  QuestionOption({
    required this.id,
    required this.optionText,
  });

  factory QuestionOption.fromJson(Map<String, dynamic> json) => QuestionOption(
    id: json['id'],
    optionText: json['optionText'],
  );
}

class SubmitAnswerResponse {
  final bool isCorrect;
  final bool isCompleted;
  final Question? nextQuestion;
  final CurrentStats currentStats;

  SubmitAnswerResponse({
    required this.isCorrect,
    required this.isCompleted,
    this.nextQuestion,
    required this.currentStats,
  });

  factory SubmitAnswerResponse.fromJson(Map<String, dynamic> json) => SubmitAnswerResponse(
    isCorrect: json['isCorrect'],
    isCompleted: json['isCompleted'],
    nextQuestion: json['nextQuestion'] != null 
      ? Question.fromJson(json['nextQuestion']) 
      : null,
    currentStats: CurrentStats.fromJson(json['currentStats']),
  );
}

class CurrentStats {
  final int answeredCount;
  final int correctCount;
  final int wrongCount;
  final double currentAccuracy;

  CurrentStats({
    required this.answeredCount,
    required this.correctCount,
    required this.wrongCount,
    required this.currentAccuracy,
  });

  factory CurrentStats.fromJson(Map<String, dynamic> json) => CurrentStats(
    answeredCount: json['answeredCount'],
    correctCount: json['correctCount'],
    wrongCount: json['wrongCount'],
    currentAccuracy: json['currentAccuracy'].toDouble(),
  );
}

class ExamSummary {
  final int attemptId;
  final String studentId;
  final ExamTemplate template;
  final double scorePercent;
  final int correctCount;
  final int wrongCount;
  final int totalQuestions;
  final DateTime startedAt;
  final DateTime? completedAt;
  final String status;
  final Map<String, DifficultyStats> perDifficultyStats;

  ExamSummary({
    required this.attemptId,
    required this.studentId,
    required this.template,
    required this.scorePercent,
    required this.correctCount,
    required this.wrongCount,
    required this.totalQuestions,
    required this.startedAt,
    this.completedAt,
    required this.status,
    required this.perDifficultyStats,
  });

  factory ExamSummary.fromJson(Map<String, dynamic> json) => ExamSummary(
    attemptId: json['attemptId'],
    studentId: json['studentId'],
    template: ExamTemplate.fromJson(json['template']),
    scorePercent: json['scorePercent'].toDouble(),
    correctCount: json['correctCount'],
    wrongCount: json['wrongCount'],
    totalQuestions: json['totalQuestions'],
    startedAt: DateTime.parse(json['startedAt']),
    completedAt: json['completedAt'] != null 
      ? DateTime.parse(json['completedAt']) 
      : null,
    status: json['status'],
    perDifficultyStats: (json['perDifficultyStats'] as Map<String, dynamic>)
      .map((key, value) => MapEntry(key, DifficultyStats.fromJson(value))),
  );
}

class DifficultyStats {
  final int totalQuestions;
  final int correctAnswers;
  final double accuracy;

  DifficultyStats({
    required this.totalQuestions,
    required this.correctAnswers,
    required this.accuracy,
  });

  factory DifficultyStats.fromJson(Map<String, dynamic> json) => DifficultyStats(
    totalQuestions: json['totalQuestions'],
    correctAnswers: json['correctAnswers'],
    accuracy: json['accuracy'].toDouble(),
  );
}

class ExamHistory {
  final int attemptId;
  final String examName;
  final String subject;
  final String chapter;
  final double scorePercent;
  final int correctCount;
  final int wrongCount;
  final String status;
  final DateTime startedAt;
  final DateTime? completedAt;

  ExamHistory({
    required this.attemptId,
    required this.examName,
    required this.subject,
    required this.chapter,
    required this.scorePercent,
    required this.correctCount,
    required this.wrongCount,
    required this.status,
    required this.startedAt,
    this.completedAt,
  });

  factory ExamHistory.fromJson(Map<String, dynamic> json) => ExamHistory(
    attemptId: json['attemptId'],
    examName: json['examName'],
    subject: json['subject'],
    chapter: json['chapter'],
    scorePercent: json['scorePercent'].toDouble(),
    correctCount: json['correctCount'],
    wrongCount: json['wrongCount'],
    status: json['status'],
    startedAt: DateTime.parse(json['startedAt']),
    completedAt: json['completedAt'] != null 
      ? DateTime.parse(json['completedAt']) 
      : null,
  );
}
```

---

## Testing

### Sample cURL Commands

**Create Template:**
```bash
curl -X POST http://localhost:8080/api/exams/templates \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "name": "Grade 10 Math Test",
    "subject": "Mathematics",
    "chapter": "Algebra",
    "totalQuestions": 10,
    "durationMinutes": 30,
    "adaptiveEnabled": true
  }'
```

**Start Exam:**
```bash
curl -X POST http://localhost:8080/api/exams/start \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "studentId": "student123",
    "examTemplateId": 1
  }'
```

**Submit Answer:**
```bash
curl -X POST http://localhost:8080/api/exams/456/answer \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "questionId": 101,
    "selectedOptionId": 1001,
    "timeTakenSeconds": 45
  }'
```

**Get Summary:**
```bash
curl -X GET http://localhost:8080/api/exams/456/summary \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Get History:**
```bash
curl -X GET "http://localhost:8080/api/exams/history?studentId=student123" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## Notes for Mobile Developers

1. **Authentication**: All endpoints require JWT token in Authorization header
2. **Error Handling**: Implement retry logic with exponential backoff for network failures
3. **Exam Flow**: 
   - Create template → Start exam → Submit answers (loop) → Get summary
4. **Question Navigation**: Each answer submission returns the next question
5. **Adaptive Difficulty**: If enabled, question difficulty adjusts based on performance
6. **Exam Completion**: When `isCompleted: true`, call summary endpoint for results
7. **Timer**: Track time client-side using `durationMinutes` from template
8. **Offline Support**: Cache exam template and questions locally, sync answers when online
9. **Security**: Store JWT token securely (Keychain/Keystore)
10. **History Limit**: History endpoint returns last 20 attempts

**Headers:**
```json
{
  "Content-Type": "application/json",
  "Authorization": "Bearer <JWT_TOKEN>"
}
```

**Request Body:**
```json
{
  "subject": "Mathematics",
  "grade": "10",
  "chapter": "Algebra",
  "questionCount": 10,
  "difficulty": "Medium",
  "examType": "MCQ"
}
```

**Request Fields:**
- `subject` (string, required): Subject name (e.g., "Mathematics", "Science", "History")
- `grade` (string, required): Grade level (e.g., "10", "11", "12")
- `chapter` (string, required): Chapter or topic name
- `questionCount` (integer, required): Number of questions (1-50)
- `difficulty` (string, optional): "Easy", "Medium", "Hard" (default: "Medium")
- `examType` (string, optional): "MCQ", "TrueFalse", "ShortAnswer" (default: "MCQ")

**Response (200 OK):**
```json
{
  "examId": "550e8400-e29b-41d4-a716-446655440000",
  "subject": "Mathematics",
  "grade": "10",
  "chapter": "Algebra",
  "difficulty": "Medium",
  "examType": "MCQ",
  "questionCount": 10,
  "questions": [
    {
      "questionId": "q1",
      "questionText": "What is the value of x in the equation 2x + 5 = 15?",
      "options": [
        "5",
        "10",
        "15",
        "20"
      ],
      "correctAnswer": "5",
      "difficulty": "Easy",
      "marks": 1
    },
    {
      "questionId": "q2",
      "questionText": "Solve for y: 3y - 7 = 14",
      "options": [
        "5",
        "7",
        "9",
        "11"
      ],
      "correctAnswer": "7",
      "difficulty": "Medium",
      "marks": 2
    }
  ],
  "totalMarks": 15,
  "duration": 30,
  "createdAt": "2025-12-06T10:30:00Z"
}
```

**Response Fields:**
- `examId` (string): Unique exam identifier (UUID)
- `subject` (string): Subject name
- `grade` (string): Grade level
- `chapter` (string): Chapter name
- `difficulty` (string): Overall difficulty level
- `examType` (string): Type of exam
- `questionCount` (integer): Total number of questions
- `questions` (array): List of question objects
  - `questionId` (string): Unique question identifier
  - `questionText` (string): The question text
  - `options` (array of strings): Answer choices (for MCQ)
  - `correctAnswer` (string): Correct answer
  - `difficulty` (string): Question difficulty
  - `marks` (integer): Points for this question
- `totalMarks` (integer): Total marks for the exam
- `duration` (integer): Suggested duration in minutes
- `createdAt` (string): ISO 8601 timestamp

**Error Response (400 Bad Request):**
```json
{
  "error": "Invalid request",
  "message": "questionCount must be between 1 and 50"
}
```

---

## 2. Submit Exam

**Endpoint:** `POST /api/exam/submit`

**Description:** Submit exam answers for grading.

**Headers:**
```json
{
  "Content-Type": "application/json",
  "Authorization": "Bearer <JWT_TOKEN>"
}
```

**Request Body:**
```json
{
  "examId": "550e8400-e29b-41d4-a716-446655440000",
  "studentId": "student123",
  "answers": [
    {
      "questionId": "q1",
      "selectedAnswer": "5"
    },
    {
      "questionId": "q2",
      "selectedAnswer": "7"
    },
    {
      "questionId": "q3",
      "selectedAnswer": "10"
    }
  ],
  "timeSpent": 25
}
```

**Request Fields:**
- `examId` (string, required): Exam UUID
- `studentId` (string, required): Student identifier
- `answers` (array, required): List of student answers
  - `questionId` (string): Question identifier
  - `selectedAnswer` (string): Student's answer
- `timeSpent` (integer, optional): Time taken in minutes

**Response (200 OK):**
```json
{
  "submissionId": "sub-123456",
  "examId": "550e8400-e29b-41d4-a716-446655440000",
  "studentId": "student123",
  "score": 85,
  "totalMarks": 100,
  "percentage": 85.0,
  "grade": "A",
  "passed": true,
  "correctAnswers": 8,
  "incorrectAnswers": 2,
  "timeSpent": 25,
  "results": [
    {
      "questionId": "q1",
      "questionText": "What is the value of x in 2x + 5 = 15?",
      "selectedAnswer": "5",
      "correctAnswer": "5",
      "isCorrect": true,
      "marks": 10,
      "earnedMarks": 10
    },
    {
      "questionId": "q2",
      "questionText": "Solve for y: 3y - 7 = 14",
      "selectedAnswer": "10",
      "correctAnswer": "7",
      "isCorrect": false,
      "marks": 10,
      "earnedMarks": 0,
      "explanation": "The correct solution is y = 7. First add 7 to both sides: 3y = 21, then divide by 3."
    }
  ],
  "feedback": "Good performance! Focus on quadratic equations for improvement.",
  "submittedAt": "2025-12-06T11:00:00Z"
}
```

**Response Fields:**
- `submissionId` (string): Unique submission identifier
- `examId` (string): Exam UUID
- `studentId` (string): Student identifier
- `score` (integer): Total score obtained
- `totalMarks` (integer): Maximum possible marks
- `percentage` (float): Percentage score
- `grade` (string): Letter grade (A, B, C, D, F)
- `passed` (boolean): Whether student passed
- `correctAnswers` (integer): Number of correct answers
- `incorrectAnswers` (integer): Number of incorrect answers
- `timeSpent` (integer): Time taken in minutes
- `results` (array): Detailed question-by-question results
  - `questionId` (string): Question identifier
  - `questionText` (string): The question
  - `selectedAnswer` (string): Student's answer
  - `correctAnswer` (string): Correct answer
  - `isCorrect` (boolean): Whether answer was correct
  - `marks` (integer): Maximum marks for question
  - `earnedMarks` (integer): Marks earned
  - `explanation` (string, optional): Explanation for incorrect answers
- `feedback` (string): AI-generated personalized feedback
- `submittedAt` (string): ISO 8601 timestamp

---

## 3. Get Exam History

**Endpoint:** `GET /api/exam/history`

**Description:** Get list of all exams taken by the student.

**Headers:**
```json
{
  "Authorization": "Bearer <JWT_TOKEN>"
}
```

**Query Parameters:**
- `studentId` (string, required): Student identifier
- `limit` (integer, optional): Number of results (default: 20, max: 100)
- `offset` (integer, optional): Pagination offset (default: 0)
- `subject` (string, optional): Filter by subject
- `fromDate` (string, optional): Filter from date (ISO 8601)
- `toDate` (string, optional): Filter to date (ISO 8601)

**Example Request:**
```
GET /api/exam/history?studentId=student123&limit=10&subject=Mathematics
```

**Response (200 OK):**
```json
{
  "total": 45,
  "limit": 10,
  "offset": 0,
  "exams": [
    {
      "examId": "550e8400-e29b-41d4-a716-446655440000",
      "subject": "Mathematics",
      "grade": "10",
      "chapter": "Algebra",
      "questionCount": 10,
      "score": 85,
      "totalMarks": 100,
      "percentage": 85.0,
      "grade": "A",
      "passed": true,
      "timeSpent": 25,
      "submittedAt": "2025-12-06T11:00:00Z"
    },
    {
      "examId": "660f9500-f39c-52e5-b827-557766551111",
      "subject": "Mathematics",
      "grade": "10",
      "chapter": "Geometry",
      "questionCount": 15,
      "score": 78,
      "totalMarks": 100,
      "percentage": 78.0,
      "grade": "B",
      "passed": true,
      "timeSpent": 30,
      "submittedAt": "2025-12-05T14:30:00Z"
    }
  ]
}
```

---

## 4. Get Exam Details

**Endpoint:** `GET /api/exam/{examId}`

**Description:** Get detailed information about a specific exam.

**Headers:**
```json
{
  "Authorization": "Bearer <JWT_TOKEN>"
}
```

**Path Parameters:**
- `examId` (string, required): Exam UUID

**Example Request:**
```
GET /api/exam/550e8400-e29b-41d4-a716-446655440000
```

**Response (200 OK):**
```json
{
  "examId": "550e8400-e29b-41d4-a716-446655440000",
  "subject": "Mathematics",
  "grade": "10",
  "chapter": "Algebra",
  "difficulty": "Medium",
  "examType": "MCQ",
  "questionCount": 10,
  "questions": [
    {
      "questionId": "q1",
      "questionText": "What is the value of x in the equation 2x + 5 = 15?",
      "options": ["5", "10", "15", "20"],
      "difficulty": "Easy",
      "marks": 1
    }
  ],
  "totalMarks": 15,
  "duration": 30,
  "createdAt": "2025-12-06T10:30:00Z"
}
```

---

## 5. Get Exam Result

**Endpoint:** `GET /api/exam/result/{submissionId}`

**Description:** Get detailed results for a specific exam submission.

**Headers:**
```json
{
  "Authorization": "Bearer <JWT_TOKEN>"
}
```

**Path Parameters:**
- `submissionId` (string, required): Submission identifier

**Example Request:**
```
GET /api/exam/result/sub-123456
```

**Response (200 OK):**
```json
{
  "submissionId": "sub-123456",
  "examId": "550e8400-e29b-41d4-a716-446655440000",
  "studentId": "student123",
  "subject": "Mathematics",
  "chapter": "Algebra",
  "score": 85,
  "totalMarks": 100,
  "percentage": 85.0,
  "grade": "A",
  "passed": true,
  "correctAnswers": 8,
  "incorrectAnswers": 2,
  "timeSpent": 25,
  "results": [
    {
      "questionId": "q1",
      "questionText": "What is the value of x in 2x + 5 = 15?",
      "selectedAnswer": "5",
      "correctAnswer": "5",
      "isCorrect": true,
      "marks": 10,
      "earnedMarks": 10
    }
  ],
  "feedback": "Good performance! Focus on quadratic equations.",
  "submittedAt": "2025-12-06T11:00:00Z"
}
```

---

## 6. Get Exam Summary/Analytics

**Endpoint:** `GET /api/exam/summary`

**Description:** Get summary statistics and analytics for a student's exam performance.

**Headers:**
```json
{
  "Authorization": "Bearer <JWT_TOKEN>"
}
```

**Query Parameters:**
- `studentId` (string, required): Student identifier
- `subject` (string, optional): Filter by subject
- `period` (string, optional): "week", "month", "year", "all" (default: "all")

**Example Request:**
```
GET /api/exam/summary?studentId=student123&period=month
```

**Response (200 OK):**
```json
{
  "studentId": "student123",
  "period": "month",
  "totalExams": 15,
  "averageScore": 82.5,
  "averagePercentage": 82.5,
  "passRate": 93.3,
  "totalTimeSpent": 450,
  "subjectBreakdown": [
    {
      "subject": "Mathematics",
      "examCount": 8,
      "averageScore": 85.0,
      "bestScore": 95,
      "worstScore": 70,
      "improvement": 15.5
    },
    {
      "subject": "Science",
      "examCount": 7,
      "averageScore": 80.0,
      "bestScore": 90,
      "worstScore": 65,
      "improvement": 12.3
    }
  ],
  "difficultyBreakdown": {
    "Easy": {
      "examCount": 5,
      "averageScore": 90.0
    },
    "Medium": {
      "examCount": 7,
      "averageScore": 82.0
    },
    "Hard": {
      "examCount": 3,
      "averageScore": 75.0
    }
  },
  "recentPerformance": [
    {
      "date": "2025-12-06",
      "score": 85
    },
    {
      "date": "2025-12-05",
      "score": 78
    },
    {
      "date": "2025-12-04",
      "score": 92
    }
  ],
  "strengths": ["Algebra", "Geometry"],
  "weaknesses": ["Trigonometry", "Probability"],
  "recommendations": "Focus more practice on Trigonometry. Consider reviewing probability concepts."
}
```

---

## Error Codes

All endpoints may return the following error responses:

**401 Unauthorized:**
```json
{
  "error": "Unauthorized",
  "message": "Invalid or expired token"
}
```

**403 Forbidden:**
```json
{
  "error": "Forbidden",
  "message": "You don't have permission to access this resource"
}
```

**404 Not Found:**
```json
{
  "error": "Not Found",
  "message": "Exam not found"
}
```

**500 Internal Server Error:**
```json
{
  "error": "Internal Server Error",
  "message": "An unexpected error occurred"
}
```

---

## Mobile Implementation Example (Flutter/Dart)

### Exam Service
```dart
import 'dart:convert';
import 'package:http/http.dart' as http;

class ExamService {
  final String baseUrl = 'https://app-wlanqwy7vuwmu-hpbwbfgqbybqg7dp.centralindia-01.azurewebsites.net';
  final String? token;

  ExamService({required this.token});

  Future<ExamResponse> generateExam(GenerateExamRequest request) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/exam/generate'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: jsonEncode(request.toJson()),
    );

    if (response.statusCode == 200) {
      return ExamResponse.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Failed to generate exam: ${response.body}');
    }
  }

  Future<SubmissionResult> submitExam(SubmitExamRequest request) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/exam/submit'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: jsonEncode(request.toJson()),
    );

    if (response.statusCode == 200) {
      return SubmissionResult.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Failed to submit exam: ${response.body}');
    }
  }

  Future<ExamHistoryResponse> getExamHistory(String studentId, {int limit = 20}) async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/exam/history?studentId=$studentId&limit=$limit'),
      headers: {
        'Authorization': 'Bearer $token',
      },
    );

    if (response.statusCode == 200) {
      return ExamHistoryResponse.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Failed to fetch exam history: ${response.body}');
    }
  }
}
```

### Data Models
```dart
class GenerateExamRequest {
  final String subject;
  final String grade;
  final String chapter;
  final int questionCount;
  final String? difficulty;
  final String? examType;

  GenerateExamRequest({
    required this.subject,
    required this.grade,
    required this.chapter,
    required this.questionCount,
    this.difficulty,
    this.examType,
  });

  Map<String, dynamic> toJson() => {
    'subject': subject,
    'grade': grade,
    'chapter': chapter,
    'questionCount': questionCount,
    if (difficulty != null) 'difficulty': difficulty,
    if (examType != null) 'examType': examType,
  };
}

class ExamResponse {
  final String examId;
  final String subject;
  final String grade;
  final String chapter;
  final int questionCount;
  final List<Question> questions;
  final int totalMarks;
  final int duration;

  ExamResponse({
    required this.examId,
    required this.subject,
    required this.grade,
    required this.chapter,
    required this.questionCount,
    required this.questions,
    required this.totalMarks,
    required this.duration,
  });

  factory ExamResponse.fromJson(Map<String, dynamic> json) => ExamResponse(
    examId: json['examId'],
    subject: json['subject'],
    grade: json['grade'],
    chapter: json['chapter'],
    questionCount: json['questionCount'],
    questions: (json['questions'] as List).map((q) => Question.fromJson(q)).toList(),
    totalMarks: json['totalMarks'],
    duration: json['duration'],
  );
}

class Question {
  final String questionId;
  final String questionText;
  final List<String> options;
  final String difficulty;
  final int marks;

  Question({
    required this.questionId,
    required this.questionText,
    required this.options,
    required this.difficulty,
    required this.marks,
  });

  factory Question.fromJson(Map<String, dynamic> json) => Question(
    questionId: json['questionId'],
    questionText: json['questionText'],
    options: List<String>.from(json['options']),
    difficulty: json['difficulty'],
    marks: json['marks'],
  );
}
```

---

## Testing

### Sample cURL Commands

**Generate Exam:**
```bash
curl -X POST https://app-wlanqwy7vuwmu-hpbwbfgqbybqg7dp.centralindia-01.azurewebsites.net/api/exam/generate \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "subject": "Mathematics",
    "grade": "10",
    "chapter": "Algebra",
    "questionCount": 5,
    "difficulty": "Medium"
  }'
```

**Submit Exam:**
```bash
curl -X POST https://app-wlanqwy7vuwmu-hpbwbfgqbybqg7dp.centralindia-01.azurewebsites.net/api/exam/submit \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "examId": "550e8400-e29b-41d4-a716-446655440000",
    "studentId": "student123",
    "answers": [
      {"questionId": "q1", "selectedAnswer": "5"}
    ],
    "timeSpent": 25
  }'
```

---

## Notes for Mobile Developers

1. **Authentication**: All endpoints require JWT token in Authorization header
2. **Error Handling**: Implement retry logic with exponential backoff for network failures
3. **Offline Support**: Cache exams locally and sync when connection is restored
4. **Loading States**: Show loading indicators during API calls
5. **Timeout**: Set reasonable timeout (30 seconds) for exam generation
6. **Pagination**: Use limit and offset for exam history to avoid large data transfers
7. **Data Validation**: Validate all inputs before sending to API
8. **Security**: Store JWT token securely (Keychain/Keystore)

