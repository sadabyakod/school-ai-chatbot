# School AI Chatbot - Mobile App Integration Guide

## API Base URLs

| Environment | URL |
|-------------|-----|
| **Production** | `https://app-wlanqwy7vuwmu.azurewebsites.net` |
| **Local Development** | `http://localhost:8080` |

---

## Authentication

Currently, endpoints are open. For future releases, add JWT token in headers:
```
Authorization: Bearer <token>
```

---

## Exam System Endpoints

### 1. Create Exam Template

Creates a reusable exam configuration.

**Endpoint:** `POST /api/exams/templates`

**Request:**
```json
{
  "name": "Algebra Quiz",
  "subject": "Mathematics",
  "chapter": "Algebra",
  "totalQuestions": 5,
  "durationMinutes": 15,
  "adaptiveEnabled": true
}
```

**Response:**
```json
{
  "id": 1,
  "name": "Algebra Quiz",
  "subject": "Mathematics",
  "chapter": "Algebra",
  "totalQuestions": 5,
  "durationMinutes": 15,
  "adaptiveEnabled": true,
  "createdAt": "2025-12-07T10:00:00Z"
}
```

**Flutter/Dart:**
```dart
Future<ExamTemplate> createTemplate(CreateTemplateRequest request) async {
  final response = await http.post(
    Uri.parse('$baseUrl/api/exams/templates'),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode(request.toJson()),
  );
  
  if (response.statusCode == 200) {
    return ExamTemplate.fromJson(jsonDecode(response.body));
  }
  throw Exception('Failed to create template');
}
```

---

### 2. Start Exam

Begins an exam attempt and returns the first question.

**Endpoint:** `POST /api/exams/start`

**Request:**
```json
{
  "studentId": "student-uuid-123",
  "examTemplateId": 1
}
```

**Response:**
```json
{
  "attemptId": 1,
  "template": {
    "id": 1,
    "name": "Algebra Quiz",
    "subject": "Mathematics",
    "chapter": "Algebra",
    "totalQuestions": 5,
    "durationMinutes": 15,
    "adaptiveEnabled": true,
    "createdAt": "2025-12-07T10:00:00Z"
  },
  "firstQuestion": {
    "id": 1,
    "subject": "Mathematics",
    "chapter": "Algebra",
    "topic": "Linear Equations",
    "text": "Solve for x: 2x + 5 = 15",
    "type": "MultipleChoice",
    "difficulty": "Easy",
    "options": [
      { "id": 1, "optionText": "x = 3" },
      { "id": 2, "optionText": "x = 5" },
      { "id": 3, "optionText": "x = 7" },
      { "id": 4, "optionText": "x = 10" }
    ]
  }
}
```

**Flutter/Dart:**
```dart
Future<StartExamResponse> startExam(String studentId, int templateId) async {
  final response = await http.post(
    Uri.parse('$baseUrl/api/exams/start'),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode({
      'studentId': studentId,
      'examTemplateId': templateId,
    }),
  );
  
  if (response.statusCode == 200) {
    return StartExamResponse.fromJson(jsonDecode(response.body));
  }
  throw Exception('Failed to start exam');
}
```

---

### 3. Submit Answer

Submits an answer and returns the next question (or completion status).

**Endpoint:** `POST /api/exams/{attemptId}/answer`

**Request:**
```json
{
  "questionId": 1,
  "selectedOptionId": 2,
  "timeTakenSeconds": 30
}
```

**Response:**
```json
{
  "isCorrect": true,
  "isCompleted": false,
  "nextQuestion": {
    "id": 2,
    "subject": "Mathematics",
    "chapter": "Algebra",
    "topic": "Quadratic Equations",
    "text": "Solve: x² - 5x + 6 = 0",
    "type": "MultipleChoice",
    "difficulty": "Medium",
    "options": [
      { "id": 5, "optionText": "x = 1, 6" },
      { "id": 6, "optionText": "x = 2, 3" },
      { "id": 7, "optionText": "x = -2, -3" },
      { "id": 8, "optionText": "x = 1, 5" }
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

**When exam is complete:**
```json
{
  "isCorrect": true,
  "isCompleted": true,
  "nextQuestion": null,
  "currentStats": {
    "answeredCount": 5,
    "correctCount": 4,
    "wrongCount": 1,
    "currentAccuracy": 80.0
  }
}
```

**Flutter/Dart:**
```dart
Future<SubmitAnswerResponse> submitAnswer({
  required int attemptId,
  required int questionId,
  required int selectedOptionId,
  required int timeTakenSeconds,
}) async {
  final response = await http.post(
    Uri.parse('$baseUrl/api/exams/$attemptId/answer'),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode({
      'questionId': questionId,
      'selectedOptionId': selectedOptionId,
      'timeTakenSeconds': timeTakenSeconds,
    }),
  );
  
  if (response.statusCode == 200) {
    return SubmitAnswerResponse.fromJson(jsonDecode(response.body));
  }
  throw Exception('Failed to submit answer');
}
```

---

### 4. Get Exam Summary

Retrieves final exam results with detailed statistics.

**Endpoint:** `GET /api/exams/{attemptId}/summary`

**Response:**
```json
{
  "attemptId": 1,
  "studentId": "student-uuid-123",
  "template": {
    "id": 1,
    "name": "Algebra Quiz",
    "subject": "Mathematics",
    "chapter": "Algebra",
    "totalQuestions": 5,
    "durationMinutes": 15,
    "adaptiveEnabled": true,
    "createdAt": "2025-12-07T10:00:00Z"
  },
  "scorePercent": 80.0,
  "correctCount": 4,
  "wrongCount": 1,
  "totalQuestions": 5,
  "startedAt": "2025-12-07T10:00:00Z",
  "completedAt": "2025-12-07T10:12:00Z",
  "status": "Completed",
  "perDifficultyStats": {
    "Easy": {
      "totalQuestions": 2,
      "correctAnswers": 2,
      "accuracy": 100.0
    },
    "Medium": {
      "totalQuestions": 2,
      "correctAnswers": 1,
      "accuracy": 50.0
    },
    "Hard": {
      "totalQuestions": 1,
      "correctAnswers": 1,
      "accuracy": 100.0
    }
  }
}
```

**Flutter/Dart:**
```dart
Future<ExamSummary> getExamSummary(int attemptId) async {
  final response = await http.get(
    Uri.parse('$baseUrl/api/exams/$attemptId/summary'),
  );
  
  if (response.statusCode == 200) {
    return ExamSummary.fromJson(jsonDecode(response.body));
  }
  throw Exception('Failed to get exam summary');
}
```

---

### 5. Get Exam History

Retrieves all past exam attempts for a student.

**Endpoint:** `GET /api/exams/history?studentId={studentId}`

**Response:**
```json
[
  {
    "attemptId": 1,
    "examName": "Algebra Quiz",
    "subject": "Mathematics",
    "chapter": "Algebra",
    "scorePercent": 80.0,
    "correctCount": 4,
    "wrongCount": 1,
    "status": "Completed",
    "startedAt": "2025-12-07T10:00:00Z",
    "completedAt": "2025-12-07T10:12:00Z"
  },
  {
    "attemptId": 2,
    "examName": "Physics Test",
    "subject": "Science",
    "chapter": "Motion",
    "scorePercent": 60.0,
    "correctCount": 3,
    "wrongCount": 2,
    "status": "Completed",
    "startedAt": "2025-12-06T14:00:00Z",
    "completedAt": "2025-12-06T14:20:00Z"
  }
]
```

**Flutter/Dart:**
```dart
Future<List<ExamHistory>> getExamHistory(String studentId) async {
  final response = await http.get(
    Uri.parse('$baseUrl/api/exams/history?studentId=$studentId'),
  );
  
  if (response.statusCode == 200) {
    final List<dynamic> data = jsonDecode(response.body);
    return data.map((e) => ExamHistory.fromJson(e)).toList();
  }
  throw Exception('Failed to get exam history');
}
```

---

## Flutter Data Models

```dart
// lib/models/exam_models.dart

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

  factory ExamTemplate.fromJson(Map<String, dynamic> json) {
    return ExamTemplate(
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
}

class Question {
  final int id;
  final String subject;
  final String? chapter;
  final String? topic;
  final String text;
  final String type;
  final String difficulty;
  final List<QuestionOption> options;

  Question({
    required this.id,
    required this.subject,
    this.chapter,
    this.topic,
    required this.text,
    required this.type,
    required this.difficulty,
    required this.options,
  });

  factory Question.fromJson(Map<String, dynamic> json) {
    return Question(
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
}

class QuestionOption {
  final int id;
  final String optionText;

  QuestionOption({required this.id, required this.optionText});

  factory QuestionOption.fromJson(Map<String, dynamic> json) {
    return QuestionOption(
      id: json['id'],
      optionText: json['optionText'],
    );
  }
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

  factory StartExamResponse.fromJson(Map<String, dynamic> json) {
    return StartExamResponse(
      attemptId: json['attemptId'],
      template: ExamTemplate.fromJson(json['template']),
      firstQuestion: json['firstQuestion'] != null
          ? Question.fromJson(json['firstQuestion'])
          : null,
    );
  }
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

  factory CurrentStats.fromJson(Map<String, dynamic> json) {
    return CurrentStats(
      answeredCount: json['answeredCount'],
      correctCount: json['correctCount'],
      wrongCount: json['wrongCount'],
      currentAccuracy: (json['currentAccuracy'] as num).toDouble(),
    );
  }
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

  factory SubmitAnswerResponse.fromJson(Map<String, dynamic> json) {
    return SubmitAnswerResponse(
      isCorrect: json['isCorrect'],
      isCompleted: json['isCompleted'],
      nextQuestion: json['nextQuestion'] != null
          ? Question.fromJson(json['nextQuestion'])
          : null,
      currentStats: CurrentStats.fromJson(json['currentStats']),
    );
  }
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

  factory DifficultyStats.fromJson(Map<String, dynamic> json) {
    return DifficultyStats(
      totalQuestions: json['totalQuestions'],
      correctAnswers: json['correctAnswers'],
      accuracy: (json['accuracy'] as num).toDouble(),
    );
  }
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

  factory ExamSummary.fromJson(Map<String, dynamic> json) {
    Map<String, DifficultyStats> stats = {};
    if (json['perDifficultyStats'] != null) {
      (json['perDifficultyStats'] as Map<String, dynamic>).forEach((key, value) {
        stats[key] = DifficultyStats.fromJson(value);
      });
    }

    return ExamSummary(
      attemptId: json['attemptId'],
      studentId: json['studentId'],
      template: ExamTemplate.fromJson(json['template']),
      scorePercent: (json['scorePercent'] as num).toDouble(),
      correctCount: json['correctCount'],
      wrongCount: json['wrongCount'],
      totalQuestions: json['totalQuestions'],
      startedAt: DateTime.parse(json['startedAt']),
      completedAt: json['completedAt'] != null
          ? DateTime.parse(json['completedAt'])
          : null,
      status: json['status'],
      perDifficultyStats: stats,
    );
  }
}

class ExamHistory {
  final int attemptId;
  final String examName;
  final String subject;
  final String? chapter;
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
    this.chapter,
    required this.scorePercent,
    required this.correctCount,
    required this.wrongCount,
    required this.status,
    required this.startedAt,
    this.completedAt,
  });

  factory ExamHistory.fromJson(Map<String, dynamic> json) {
    return ExamHistory(
      attemptId: json['attemptId'],
      examName: json['examName'],
      subject: json['subject'],
      chapter: json['chapter'],
      scorePercent: (json['scorePercent'] as num).toDouble(),
      correctCount: json['correctCount'],
      wrongCount: json['wrongCount'],
      status: json['status'],
      startedAt: DateTime.parse(json['startedAt']),
      completedAt: json['completedAt'] != null
          ? DateTime.parse(json['completedAt'])
          : null,
    );
  }
}
```

---

## Flutter Exam Service

```dart
// lib/services/exam_service.dart

import 'dart:convert';
import 'package:http/http.dart' as http;
import '../models/exam_models.dart';

class ExamService {
  static const String baseUrl = 'https://app-wlanqwy7vuwmu.azurewebsites.net';
  
  // For local development:
  // static const String baseUrl = 'http://10.0.2.2:8080'; // Android emulator
  // static const String baseUrl = 'http://localhost:8080'; // iOS simulator

  /// Create a new exam template
  Future<ExamTemplate> createTemplate({
    required String name,
    required String subject,
    String? chapter,
    required int totalQuestions,
    required int durationMinutes,
    bool adaptiveEnabled = false,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/exams/templates'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'name': name,
        'subject': subject,
        'chapter': chapter,
        'totalQuestions': totalQuestions,
        'durationMinutes': durationMinutes,
        'adaptiveEnabled': adaptiveEnabled,
      }),
    );

    if (response.statusCode == 200) {
      return ExamTemplate.fromJson(jsonDecode(response.body));
    }
    throw Exception('Failed to create template: ${response.body}');
  }

  /// Start a new exam attempt
  Future<StartExamResponse> startExam({
    required String studentId,
    required int examTemplateId,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/exams/start'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'studentId': studentId,
        'examTemplateId': examTemplateId,
      }),
    );

    if (response.statusCode == 200) {
      return StartExamResponse.fromJson(jsonDecode(response.body));
    }
    throw Exception('Failed to start exam: ${response.body}');
  }

  /// Submit an answer and get the next question
  Future<SubmitAnswerResponse> submitAnswer({
    required int attemptId,
    required int questionId,
    required int selectedOptionId,
    required int timeTakenSeconds,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/exams/$attemptId/answer'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'questionId': questionId,
        'selectedOptionId': selectedOptionId,
        'timeTakenSeconds': timeTakenSeconds,
      }),
    );

    if (response.statusCode == 200) {
      return SubmitAnswerResponse.fromJson(jsonDecode(response.body));
    }
    throw Exception('Failed to submit answer: ${response.body}');
  }

  /// Get exam summary/results
  Future<ExamSummary> getExamSummary(int attemptId) async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/exams/$attemptId/summary'),
    );

    if (response.statusCode == 200) {
      return ExamSummary.fromJson(jsonDecode(response.body));
    }
    throw Exception('Failed to get exam summary: ${response.body}');
  }

  /// Get student's exam history
  Future<List<ExamHistory>> getExamHistory(String studentId) async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/exams/history?studentId=$studentId'),
    );

    if (response.statusCode == 200) {
      final List<dynamic> data = jsonDecode(response.body);
      return data.map((e) => ExamHistory.fromJson(e)).toList();
    }
    throw Exception('Failed to get exam history: ${response.body}');
  }
}
```

---

## Exam Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     EXAM FLOW                               │
└─────────────────────────────────────────────────────────────┘

     ┌──────────────┐
     │ Select Exam  │  (Template ID from list or create new)
     └──────┬───────┘
            │
            ▼
     ┌──────────────┐
     │ POST /start  │ ──► Get attemptId + firstQuestion
     └──────┬───────┘
            │
            ▼
    ┌───────────────────┐
    │  Display Question │◄─────────────────────┐
    │  with Options     │                      │
    └───────┬───────────┘                      │
            │                                  │
            ▼                                  │
    ┌───────────────────┐                      │
    │  Student Selects  │                      │
    │  an Option        │                      │
    └───────┬───────────┘                      │
            │                                  │
            ▼                                  │
    ┌───────────────────┐     nextQuestion     │
    │ POST /{id}/answer │ ─────────────────────┘
    └───────┬───────────┘
            │
            │ isCompleted = true
            ▼
    ┌───────────────────┐
    │ GET /{id}/summary │ ──► Display Results
    └───────────────────┘

    ┌───────────────────┐
    │ GET /history      │ ──► Show Past Exams
    └───────────────────┘
```

---

## Error Handling

All endpoints return standard error responses:

```json
{
  "status": 400,
  "message": "Validation error message"
}
```

```json
{
  "status": 404,
  "message": "Exam template 99 not found."
}
```

```json
{
  "status": 500,
  "message": "Failed to start exam."
}
```

**Flutter Error Handling:**
```dart
try {
  final exam = await examService.startExam(
    studentId: 'student-123',
    examTemplateId: 1,
  );
  // Handle success
} catch (e) {
  // Show error dialog or snackbar
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(content: Text('Error: $e')),
  );
}
```

---

## Available Subjects & Chapters

Currently seeded in database:

| Subject | Chapters | Questions |
|---------|----------|-----------|
| Mathematics | Algebra | 8 |
| Science | Physics, Chemistry, Biology | 8 |

---

## Testing with cURL

```bash
# 1. Create template
curl -X POST "https://app-wlanqwy7vuwmu.azurewebsites.net/api/exams/templates" \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Quiz","subject":"Mathematics","chapter":"Algebra","totalQuestions":5,"durationMinutes":10,"adaptiveEnabled":true}'

# 2. Start exam
curl -X POST "https://app-wlanqwy7vuwmu.azurewebsites.net/api/exams/start" \
  -H "Content-Type: application/json" \
  -d '{"studentId":"mobile-user-1","examTemplateId":1}'

# 3. Submit answer
curl -X POST "https://app-wlanqwy7vuwmu.azurewebsites.net/api/exams/1/answer" \
  -H "Content-Type: application/json" \
  -d '{"questionId":1,"selectedOptionId":2,"timeTakenSeconds":30}'

# 4. Get summary
curl "https://app-wlanqwy7vuwmu.azurewebsites.net/api/exams/1/summary"

# 5. Get history
curl "https://app-wlanqwy7vuwmu.azurewebsites.net/api/exams/history?studentId=mobile-user-1"
```

---

## Support

For issues or questions, contact the backend team or check the Swagger documentation:
- **Swagger UI:** https://app-wlanqwy7vuwmu.azurewebsites.net/swagger
