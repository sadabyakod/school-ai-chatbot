# Karnataka 2nd PUC Exam Generator API

## Mobile App Integration Guide

This document provides complete instructions for integrating the AI-powered exam generator endpoint into your Flutter/mobile application.

---

## API Endpoints

### 1. Generate Exam
```
POST /api/exam/generate
```

### 2. Submit & Evaluate Answers
```
POST /api/exam/submit
```

**Base URLs:**
- Local Development: `http://localhost:8080`
- Local Network (Mobile): `http://192.168.1.77:8080`
- Production: `https://app-wlanqwy7vuwmu.azurewebsites.net`

---

## 1. Generate Exam Endpoint

### Request

### Headers
```
Content-Type: application/json
```

### Request Body
```json
{
  "subject": "Mathematics",
  "grade": "2nd PUC"
}
```

### Request Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `subject` | string | ✅ Yes | Subject name (e.g., "Mathematics", "Physics", "Chemistry") |
| `grade` | string | ✅ Yes | Grade/Class (e.g., "2nd PUC", "12", "II PUC") |

### Supported Subjects
- Mathematics
- Physics  
- Chemistry
- Biology
- Computer Science
- Electronics

---

## Response

### Success Response (HTTP 200)

```json
{
  "examId": "KAR-2PUC-20251207123456-A1B2C3D4",
  "subject": "Mathematics",
  "grade": "2nd PUC",
  "chapter": "Full Syllabus",
  "difficulty": "Medium",
  "examType": "Full Paper",
  "totalMarks": 80,
  "duration": 195,
  "instructions": [
    "Answer ALL 15 questions in Part A",
    "Answer any SIX questions from Part B",
    "Answer any SIX questions from Part C",
    "Answer any FOUR questions from Part D",
    "Answer any ONE question from Part E"
  ],
  "parts": [
    {
      "partName": "Part A",
      "partDescription": "Answer ALL the following questions",
      "questionType": "MCQ",
      "marksPerQuestion": 1,
      "totalQuestions": 15,
      "questionsToAnswer": 15,
      "questions": [
        {
          "questionId": "A1",
          "questionNumber": 1,
          "questionText": "What is the order of matrix A = [[1,2],[3,4]]?",
          "options": ["A) 2x2", "B) 2x3", "C) 3x2", "D) 1x4"],
          "correctAnswer": "A) 2x2",
          "topic": "Matrices"
        }
      ]
    },
    {
      "partName": "Part B",
      "partDescription": "Answer any SIX of the following questions",
      "questionType": "Short Answer (2 marks)",
      "marksPerQuestion": 2,
      "totalQuestions": 8,
      "questionsToAnswer": 6,
      "questions": [...]
    },
    {
      "partName": "Part C",
      "partDescription": "Answer any SIX of the following questions",
      "questionType": "Short Answer (3 marks)",
      "marksPerQuestion": 3,
      "totalQuestions": 8,
      "questionsToAnswer": 6,
      "questions": [...]
    },
    {
      "partName": "Part D",
      "partDescription": "Answer any FOUR of the following questions",
      "questionType": "Long Answer (5 marks)",
      "marksPerQuestion": 5,
      "totalQuestions": 6,
      "questionsToAnswer": 4,
      "questions": [...]
    },
    {
      "partName": "Part E",
      "partDescription": "Answer any ONE of the following questions",
      "questionType": "Long Answer (10 marks)",
      "marksPerQuestion": 10,
      "totalQuestions": 2,
      "questionsToAnswer": 1,
      "questions": [
        {
          "questionId": "E1",
          "questionNumber": 38,
          "questionText": "...",
          "subParts": [
            {
              "partLabel": "a",
              "questionText": "Sub-question (5 marks)",
              "correctAnswer": "Model answer for part a"
            },
            {
              "partLabel": "b", 
              "questionText": "Sub-question (5 marks)",
              "correctAnswer": "Model answer for part b"
            }
          ],
          "options": [],
          "correctAnswer": "Combined answer",
          "topic": "Integration"
        }
      ]
    }
  ],
  "questionCount": 39,
  "createdAt": "2025-12-07T12:34:56.789Z"
}
```

---

## 2. Submit & Evaluate Answers Endpoint

### Request

**Headers:**
```
Content-Type: multipart/form-data
```

### Request Body (multipart/form-data)

Students can submit answers as **text** OR **image uploads** of handwritten answers.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `examId` | string | ✅ Yes | The exam ID from generated exam |
| `answers[0].questionId` | string | ✅ Yes | Question ID (e.g., "A1", "B2") |
| `answers[0].questionNumber` | int | ✅ Yes | Question number (1-39) |
| `answers[0].questionText` | string | ✅ Yes | Original question text |
| `answers[0].correctAnswer` | string | ✅ Yes | Model answer for comparison |
| `answers[0].maxMarks` | int | ✅ Yes | Maximum marks for question |
| `answers[0].textAnswer` | string | ❌ Optional | Text answer (use this OR imageFile) |
| `answers[0].imageFile` | file | ❌ Optional | Image of handwritten answer (JPEG, PNG) |

### Success Response (HTTP 200)

```json
{
  "examId": "KAR-2PUC-20251207123456-A1B2C3D4",
  "totalScore": 65,
  "totalMaxScore": 80,
  "percentage": 81.3,
  "grade": "A",
  "questionsAnswered": 35,
  "totalQuestions": 39,
  "results": [
    {
      "questionId": "A1",
      "questionNumber": 1,
      "score": 1,
      "maxMarks": 1,
      "feedback": "Correct! The order of a 2x2 matrix is indeed 2x2.",
      "isCorrect": true,
      "extractedText": null
    },
    {
      "questionId": "B16",
      "questionNumber": 16,
      "score": 2,
      "maxMarks": 2,
      "feedback": "Excellent! The differentiation is correct with proper steps shown.",
      "isCorrect": true,
      "extractedText": null
    },
    {
      "questionId": "C24",
      "questionNumber": 24,
      "score": 2,
      "maxMarks": 3,
      "feedback": "Partial marks awarded. The approach is correct but final simplification is missing.",
      "isCorrect": false,
      "extractedText": "∫ sin(x)dx = -cos(x)"
    }
  ],
  "evaluatedAt": "2025-12-07T12:45:00.000Z"
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `examId` | string | The submitted exam ID |
| `totalScore` | int | Total marks obtained |
| `totalMaxScore` | int | Maximum possible marks |
| `percentage` | double | Score percentage |
| `grade` | string | Grade (A+, A, B+, B, C, D, F) |
| `questionsAnswered` | int | Number of questions answered |
| `totalQuestions` | int | Total questions in submission |
| `results` | array | Per-question evaluation results |
| `evaluatedAt` | string | ISO 8601 timestamp |

### Grade Scale

| Percentage | Grade |
|------------|-------|
| 90-100% | A+ |
| 80-89% | A |
| 70-79% | B+ |
| 60-69% | B |
| 50-59% | C |
| 35-49% | D |
| 0-34% | F |

### Image Answer Support

For handwritten answers, upload an image file:
- **Supported formats**: JPEG, PNG, GIF, WebP
- **Max file size**: 10MB recommended
- **AI OCR**: The system uses GPT-4 Vision to read handwritten text
- **extractedText**: The response includes what text was extracted from the image

---

## Karnataka 2nd PUC Exam Structure

| Part | Questions | Marks/Question | Answer | Total Marks |
|------|-----------|----------------|--------|-------------|
| A | 15 MCQs | 1 | All 15 | 15 |
| B | 8 Short Answer | 2 | Any 6 | 12 |
| C | 8 Short Answer | 3 | Any 6 | 18 |
| D | 6 Long Answer | 5 | Any 4 | 20 |
| E | 2 Long Answer | 10 (5+5 sub-parts) | Any 1 | 10 |

**Total: 80 marks | Duration: 3 hours 15 minutes (195 minutes)**

---

## Flutter Implementation

### 1. Add Dependencies

```yaml
# pubspec.yaml
dependencies:
  http: ^1.1.0
  image_picker: ^1.0.4
  flutter:
    sdk: flutter
```

### 2. Create API Service

```dart
// lib/services/exam_api_service.dart
import 'dart:convert';
import 'dart:io';
import 'package:http/http.dart' as http;

class ExamApiService {
  // Use your production URL or localhost for development
  static const String baseUrl = 'http://localhost:8080';
  // static const String baseUrl = 'https://app-wlanqwy7vuwmu.azurewebsites.net';

  /// Generate a Karnataka 2nd PUC Model Question Paper
  Future<GeneratedExam> generateExam({
    required String subject,
    required String grade,
  }) async {
    final url = Uri.parse('$baseUrl/api/exam/generate');
    
    final response = await http.post(
      url,
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'subject': subject,
        'grade': grade,
      }),
    ).timeout(const Duration(seconds: 120)); // Longer timeout for AI generation

    if (response.statusCode == 200) {
      final json = jsonDecode(response.body);
      return GeneratedExam.fromJson(json);
    } else {
      throw ExamGenerationException(
        'Failed to generate exam: ${response.statusCode}',
        response.body,
      );
    }
  }

  /// Submit exam answers for AI evaluation
  /// Supports both text answers and image uploads
  Future<ExamSubmissionResult> submitExamAnswers({
    required String examId,
    required List<AnswerSubmission> answers,
  }) async {
    final url = Uri.parse('$baseUrl/api/exam/submit');
    
    var request = http.MultipartRequest('POST', url);
    request.fields['examId'] = examId;
    
    for (int i = 0; i < answers.length; i++) {
      final answer = answers[i];
      request.fields['answers[$i].questionId'] = answer.questionId;
      request.fields['answers[$i].questionNumber'] = answer.questionNumber.toString();
      request.fields['answers[$i].questionText'] = answer.questionText;
      request.fields['answers[$i].correctAnswer'] = answer.correctAnswer;
      request.fields['answers[$i].maxMarks'] = answer.maxMarks.toString();
      
      if (answer.textAnswer != null) {
        request.fields['answers[$i].textAnswer'] = answer.textAnswer!;
      }
      
      if (answer.imageFile != null) {
        request.files.add(await http.MultipartFile.fromPath(
          'answers[$i].imageFile',
          answer.imageFile!.path,
        ));
      }
    }
    
    final streamedResponse = await request.send()
        .timeout(const Duration(seconds: 180)); // Longer timeout for evaluation
    final response = await http.Response.fromStream(streamedResponse);
    
    if (response.statusCode == 200) {
      final json = jsonDecode(response.body);
      return ExamSubmissionResult.fromJson(json);
    } else {
      throw ExamSubmissionException(
        'Failed to submit exam: ${response.statusCode}',
        response.body,
      );
    }
  }
}

class ExamGenerationException implements Exception {
  final String message;
  final String? details;
  
  ExamGenerationException(this.message, [this.details]);
  
  @override
  String toString() => 'ExamGenerationException: $message';
}

class ExamSubmissionException implements Exception {
  final String message;
  final String? details;
  
  ExamSubmissionException(this.message, [this.details]);
  
  @override
  String toString() => 'ExamSubmissionException: $message';
}
```

### 3. Create Data Models

```dart
// lib/models/generated_exam.dart
class GeneratedExam {
  final String examId;
  final String subject;
  final String grade;
  final String chapter;
  final String difficulty;
  final String examType;
  final int totalMarks;
  final int duration;
  final List<String> instructions;
  final List<ExamPart> parts;
  final int questionCount;
  final String createdAt;

  GeneratedExam({
    required this.examId,
    required this.subject,
    required this.grade,
    required this.chapter,
    required this.difficulty,
    required this.examType,
    required this.totalMarks,
    required this.duration,
    required this.instructions,
    required this.parts,
    required this.questionCount,
    required this.createdAt,
  });

  factory GeneratedExam.fromJson(Map<String, dynamic> json) {
    return GeneratedExam(
      examId: json['examId'] ?? '',
      subject: json['subject'] ?? '',
      grade: json['grade'] ?? '',
      chapter: json['chapter'] ?? '',
      difficulty: json['difficulty'] ?? '',
      examType: json['examType'] ?? '',
      totalMarks: json['totalMarks'] ?? 80,
      duration: json['duration'] ?? 195,
      instructions: List<String>.from(json['instructions'] ?? []),
      parts: (json['parts'] as List?)
          ?.map((p) => ExamPart.fromJson(p))
          .toList() ?? [],
      questionCount: json['questionCount'] ?? 0,
      createdAt: json['createdAt'] ?? '',
    );
  }
}

class ExamPart {
  final String partName;
  final String partDescription;
  final String questionType;
  final int marksPerQuestion;
  final int totalQuestions;
  final int questionsToAnswer;
  final List<ExamQuestion> questions;

  ExamPart({
    required this.partName,
    required this.partDescription,
    required this.questionType,
    required this.marksPerQuestion,
    required this.totalQuestions,
    required this.questionsToAnswer,
    required this.questions,
  });

  factory ExamPart.fromJson(Map<String, dynamic> json) {
    return ExamPart(
      partName: json['partName'] ?? '',
      partDescription: json['partDescription'] ?? '',
      questionType: json['questionType'] ?? '',
      marksPerQuestion: json['marksPerQuestion'] ?? 0,
      totalQuestions: json['totalQuestions'] ?? 0,
      questionsToAnswer: json['questionsToAnswer'] ?? 0,
      questions: (json['questions'] as List?)
          ?.map((q) => ExamQuestion.fromJson(q))
          .toList() ?? [],
    );
  }
}

class ExamQuestion {
  final String questionId;
  final int questionNumber;
  final String questionText;
  final List<String> options;
  final String correctAnswer;
  final String topic;
  final List<SubPart>? subParts;

  ExamQuestion({
    required this.questionId,
    required this.questionNumber,
    required this.questionText,
    required this.options,
    required this.correctAnswer,
    required this.topic,
    this.subParts,
  });

  bool get isMCQ => options.isNotEmpty;
  bool get hasSubParts => subParts != null && subParts!.isNotEmpty;

  factory ExamQuestion.fromJson(Map<String, dynamic> json) {
    return ExamQuestion(
      questionId: json['questionId'] ?? '',
      questionNumber: json['questionNumber'] ?? 0,
      questionText: json['questionText'] ?? '',
      options: List<String>.from(json['options'] ?? []),
      correctAnswer: json['correctAnswer'] ?? '',
      topic: json['topic'] ?? '',
      subParts: (json['subParts'] as List?)
          ?.map((s) => SubPart.fromJson(s))
          .toList(),
    );
  }
}

class SubPart {
  final String partLabel;
  final String questionText;
  final String correctAnswer;

  SubPart({
    required this.partLabel,
    required this.questionText,
    required this.correctAnswer,
  });

  factory SubPart.fromJson(Map<String, dynamic> json) {
    return SubPart(
      partLabel: json['partLabel'] ?? '',
      questionText: json['questionText'] ?? '',
      correctAnswer: json['correctAnswer'] ?? '',
    );
  }
}

// lib/models/answer_submission.dart
class AnswerSubmission {
  final String questionId;
  final int questionNumber;
  final String questionText;
  final String correctAnswer;
  final int maxMarks;
  final String? textAnswer;
  final File? imageFile;

  AnswerSubmission({
    required this.questionId,
    required this.questionNumber,
    required this.questionText,
    required this.correctAnswer,
    required this.maxMarks,
    this.textAnswer,
    this.imageFile,
  });
}

// lib/models/exam_submission_result.dart
class ExamSubmissionResult {
  final String examId;
  final int totalScore;
  final int totalMaxScore;
  final double percentage;
  final String grade;
  final int questionsAnswered;
  final int totalQuestions;
  final List<AnswerEvaluationResult> results;
  final String evaluatedAt;

  ExamSubmissionResult({
    required this.examId,
    required this.totalScore,
    required this.totalMaxScore,
    required this.percentage,
    required this.grade,
    required this.questionsAnswered,
    required this.totalQuestions,
    required this.results,
    required this.evaluatedAt,
  });

  factory ExamSubmissionResult.fromJson(Map<String, dynamic> json) {
    return ExamSubmissionResult(
      examId: json['examId'] ?? '',
      totalScore: json['totalScore'] ?? 0,
      totalMaxScore: json['totalMaxScore'] ?? 0,
      percentage: (json['percentage'] ?? 0).toDouble(),
      grade: json['grade'] ?? 'F',
      questionsAnswered: json['questionsAnswered'] ?? 0,
      totalQuestions: json['totalQuestions'] ?? 0,
      results: (json['results'] as List?)
          ?.map((r) => AnswerEvaluationResult.fromJson(r))
          .toList() ?? [],
      evaluatedAt: json['evaluatedAt'] ?? '',
    );
  }
}

class AnswerEvaluationResult {
  final String questionId;
  final int questionNumber;
  final int score;
  final int maxMarks;
  final String feedback;
  final bool isCorrect;
  final String? extractedText;

  AnswerEvaluationResult({
    required this.questionId,
    required this.questionNumber,
    required this.score,
    required this.maxMarks,
    required this.feedback,
    required this.isCorrect,
    this.extractedText,
  });

  factory AnswerEvaluationResult.fromJson(Map<String, dynamic> json) {
    return AnswerEvaluationResult(
      questionId: json['questionId'] ?? '',
      questionNumber: json['questionNumber'] ?? 0,
      score: json['score'] ?? 0,
      maxMarks: json['maxMarks'] ?? 0,
      feedback: json['feedback'] ?? '',
      isCorrect: json['isCorrect'] ?? false,
      extractedText: json['extractedText'],
    );
  }
}
```

### 4. Create Exam Generator Screen

```dart
// lib/screens/exam_generator_screen.dart
import 'package:flutter/material.dart';
import '../services/exam_api_service.dart';
import '../models/generated_exam.dart';

class ExamGeneratorScreen extends StatefulWidget {
  const ExamGeneratorScreen({Key? key}) : super(key: key);

  @override
  State<ExamGeneratorScreen> createState() => _ExamGeneratorScreenState();
}

class _ExamGeneratorScreenState extends State<ExamGeneratorScreen> {
  final ExamApiService _apiService = ExamApiService();
  
  String _selectedSubject = 'Mathematics';
  String _selectedGrade = '2nd PUC';
  bool _isLoading = false;
  GeneratedExam? _generatedExam;
  String? _error;

  final List<String> _subjects = [
    'Mathematics',
    'Physics',
    'Chemistry',
    'Biology',
    'Computer Science',
  ];

  final List<String> _grades = [
    '2nd PUC',
    '1st PUC',
    'Class 12',
    'Class 11',
  ];

  Future<void> _generateExam() async {
    setState(() {
      _isLoading = true;
      _error = null;
      _generatedExam = null;
    });

    try {
      final exam = await _apiService.generateExam(
        subject: _selectedSubject,
        grade: _selectedGrade,
      );
      
      setState(() {
        _generatedExam = exam;
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _error = e.toString();
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Karnataka 2nd PUC Exam Generator'),
        backgroundColor: Colors.indigo,
        foregroundColor: Colors.white,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Header Card
            Card(
              color: Colors.indigo.shade50,
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  children: [
                    Icon(Icons.school, size: 48, color: Colors.indigo),
                    const SizedBox(height: 8),
                    Text(
                      'AI-Powered Model Question Paper',
                      style: Theme.of(context).textTheme.titleLarge,
                    ),
                    const Text('Karnataka State Board Format'),
                  ],
                ),
              ),
            ),
            
            const SizedBox(height: 24),
            
            // Subject Dropdown
            DropdownButtonFormField<String>(
              value: _selectedSubject,
              decoration: const InputDecoration(
                labelText: 'Subject',
                border: OutlineInputBorder(),
                prefixIcon: Icon(Icons.book),
              ),
              items: _subjects.map((subject) {
                return DropdownMenuItem(value: subject, child: Text(subject));
              }).toList(),
              onChanged: (value) {
                if (value != null) setState(() => _selectedSubject = value);
              },
            ),
            
            const SizedBox(height: 16),
            
            // Grade Dropdown
            DropdownButtonFormField<String>(
              value: _selectedGrade,
              decoration: const InputDecoration(
                labelText: 'Grade',
                border: OutlineInputBorder(),
                prefixIcon: Icon(Icons.grade),
              ),
              items: _grades.map((grade) {
                return DropdownMenuItem(value: grade, child: Text(grade));
              }).toList(),
              onChanged: (value) {
                if (value != null) setState(() => _selectedGrade = value);
              },
            ),
            
            const SizedBox(height: 24),
            
            // Generate Button
            ElevatedButton.icon(
              onPressed: _isLoading ? null : _generateExam,
              icon: _isLoading 
                  ? const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.auto_awesome),
              label: Text(_isLoading ? 'Generating...' : 'Generate Model Paper'),
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.indigo,
                foregroundColor: Colors.white,
                padding: const EdgeInsets.symmetric(vertical: 16),
                textStyle: const TextStyle(fontSize: 18),
              ),
            ),
            
            if (_isLoading)
              const Padding(
                padding: EdgeInsets.all(16),
                child: Column(
                  children: [
                    LinearProgressIndicator(),
                    SizedBox(height: 8),
                    Text('AI is generating your exam paper...'),
                    Text('This may take 30-60 seconds'),
                  ],
                ),
              ),
            
            // Error Message
            if (_error != null)
              Card(
                color: Colors.red.shade50,
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Text(_error!, style: TextStyle(color: Colors.red)),
                ),
              ),
            
            // Generated Exam Display
            if (_generatedExam != null) ...[
              const SizedBox(height: 24),
              _buildExamCard(_generatedExam!),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildExamCard(GeneratedExam exam) {
    return Card(
      elevation: 4,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Exam Header
            Row(
              children: [
                const Icon(Icons.check_circle, color: Colors.green),
                const SizedBox(width: 8),
                Text(
                  'Exam Generated Successfully!',
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    color: Colors.green,
                  ),
                ),
              ],
            ),
            const Divider(),
            
            // Exam Info
            _infoRow('Exam ID', exam.examId),
            _infoRow('Subject', exam.subject),
            _infoRow('Grade', exam.grade),
            _infoRow('Total Marks', '${exam.totalMarks}'),
            _infoRow('Duration', '${exam.duration} minutes'),
            _infoRow('Total Questions', '${exam.questionCount}'),
            
            const Divider(),
            
            // Parts Summary
            Text('Parts:', style: Theme.of(context).textTheme.titleSmall),
            const SizedBox(height: 8),
            ...exam.parts.map((part) => Padding(
              padding: const EdgeInsets.symmetric(vertical: 4),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text('${part.partName} (${part.questionType})'),
                  Text('${part.questions.length} × ${part.marksPerQuestion} marks'),
                ],
              ),
            )),
            
            const SizedBox(height: 16),
            
            // Start Exam Button
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                onPressed: () {
                  // Navigate to exam taking screen
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (_) => ExamTakingScreen(exam: exam),
                    ),
                  );
                },
                icon: const Icon(Icons.play_arrow),
                label: const Text('Start Exam'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.green,
                  foregroundColor: Colors.white,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _infoRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(fontWeight: FontWeight.w500)),
          Text(value),
        ],
      ),
    );
  }
}
```

### 5. Exam Taking Screen (Optional)

```dart
// lib/screens/exam_taking_screen.dart
import 'package:flutter/material.dart';
import '../models/generated_exam.dart';

class ExamTakingScreen extends StatefulWidget {
  final GeneratedExam exam;

  const ExamTakingScreen({Key? key, required this.exam}) : super(key: key);

  @override
  State<ExamTakingScreen> createState() => _ExamTakingScreenState();
}

class _ExamTakingScreenState extends State<ExamTakingScreen> {
  int _currentPartIndex = 0;
  int _currentQuestionIndex = 0;
  Map<String, String> _userAnswers = {};

  ExamPart get currentPart => widget.exam.parts[_currentPartIndex];
  ExamQuestion get currentQuestion => currentPart.questions[_currentQuestionIndex];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('${widget.exam.subject} - ${currentPart.partName}'),
        actions: [
          Center(
            child: Padding(
              padding: const EdgeInsets.all(8.0),
              child: Text(
                'Q${_currentQuestionIndex + 1}/${currentPart.questions.length}',
                style: const TextStyle(fontSize: 16),
              ),
            ),
          ),
        ],
      ),
      body: Column(
        children: [
          // Part Info
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(12),
            color: Colors.indigo.shade100,
            child: Text(
              '${currentPart.partDescription} (${currentPart.marksPerQuestion} marks each)',
              textAlign: TextAlign.center,
            ),
          ),
          
          // Question
          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Question Number & Text
                  Text(
                    'Q${currentQuestion.questionNumber}. ${currentQuestion.questionText}',
                    style: const TextStyle(fontSize: 18),
                  ),
                  
                  const SizedBox(height: 24),
                  
                  // MCQ Options
                  if (currentQuestion.isMCQ)
                    ...currentQuestion.options.map((option) {
                      final isSelected = _userAnswers[currentQuestion.questionId] == option;
                      return Card(
                        color: isSelected ? Colors.indigo.shade100 : null,
                        child: ListTile(
                          leading: Radio<String>(
                            value: option,
                            groupValue: _userAnswers[currentQuestion.questionId],
                            onChanged: (value) {
                              setState(() {
                                _userAnswers[currentQuestion.questionId] = value!;
                              });
                            },
                          ),
                          title: Text(option),
                          onTap: () {
                            setState(() {
                              _userAnswers[currentQuestion.questionId] = option;
                            });
                          },
                        ),
                      );
                    }),
                  
                  // Sub-parts for Part E
                  if (currentQuestion.hasSubParts) ...[
                    const Divider(),
                    ...currentQuestion.subParts!.map((subPart) => Card(
                      child: Padding(
                        padding: const EdgeInsets.all(12),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              '(${subPart.partLabel}) ${subPart.questionText}',
                              style: const TextStyle(fontWeight: FontWeight.w500),
                            ),
                            const SizedBox(height: 8),
                            TextField(
                              maxLines: 5,
                              decoration: InputDecoration(
                                hintText: 'Write your answer here...',
                                border: OutlineInputBorder(),
                              ),
                            ),
                          ],
                        ),
                      ),
                    )),
                  ],
                  
                  // Text answer for non-MCQ
                  if (!currentQuestion.isMCQ && !currentQuestion.hasSubParts)
                    TextField(
                      maxLines: 8,
                      decoration: const InputDecoration(
                        hintText: 'Write your answer here...',
                        border: OutlineInputBorder(),
                      ),
                    ),
                ],
              ),
            ),
          ),
          
          // Navigation
          Container(
            padding: const EdgeInsets.all(16),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                ElevatedButton.icon(
                  onPressed: _currentQuestionIndex > 0 ? _previousQuestion : null,
                  icon: const Icon(Icons.arrow_back),
                  label: const Text('Previous'),
                ),
                ElevatedButton.icon(
                  onPressed: _nextQuestion,
                  icon: const Icon(Icons.arrow_forward),
                  label: Text(_isLastQuestion ? 'Submit' : 'Next'),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  bool get _isLastQuestion =>
      _currentPartIndex == widget.exam.parts.length - 1 &&
      _currentQuestionIndex == currentPart.questions.length - 1;

  void _previousQuestion() {
    setState(() {
      if (_currentQuestionIndex > 0) {
        _currentQuestionIndex--;
      } else if (_currentPartIndex > 0) {
        _currentPartIndex--;
        _currentQuestionIndex = currentPart.questions.length - 1;
      }
    });
  }

  void _nextQuestion() {
    if (_isLastQuestion) {
      _showSubmitDialog();
    } else {
      setState(() {
        if (_currentQuestionIndex < currentPart.questions.length - 1) {
          _currentQuestionIndex++;
        } else if (_currentPartIndex < widget.exam.parts.length - 1) {
          _currentPartIndex++;
          _currentQuestionIndex = 0;
        }
      });
    }
  }

  void _showSubmitDialog() {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Submit Exam?'),
        content: Text('You have answered ${_userAnswers.length} questions.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Continue'),
          ),
          ElevatedButton(
            onPressed: () {
              Navigator.pop(context);
              _submitExam();
            },
            child: const Text('Submit'),
          ),
        ],
      ),
    );
  }

  Future<void> _submitExam() async {
    // Show loading
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (_) => const Center(child: CircularProgressIndicator()),
    );

    try {
      final answers = <AnswerSubmission>[];
      
      // Collect all answers from all parts
      for (var part in widget.exam.parts) {
        for (var question in part.questions) {
          final answer = _userAnswers[question.questionId];
          final imageFile = _imageAnswers[question.questionId];
          
          answers.add(AnswerSubmission(
            questionId: question.questionId,
            questionNumber: question.questionNumber,
            questionText: question.questionText,
            correctAnswer: question.correctAnswer,
            maxMarks: part.marksPerQuestion,
            textAnswer: answer,
            imageFile: imageFile,
          ));
        }
      }

      final result = await ExamApiService().submitExamAnswers(
        examId: widget.exam.examId,
        answers: answers,
      );

      Navigator.pop(context); // Close loading
      Navigator.pop(context); // Close exam screen
      
      // Navigate to results screen
      Navigator.push(
        context,
        MaterialPageRoute(
          builder: (_) => ExamResultsScreen(result: result),
        ),
      );
    } catch (e) {
      Navigator.pop(context); // Close loading
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Error: $e')),
      );
    }
  }
}
```

### 6. Exam Results Screen

```dart
// lib/screens/exam_results_screen.dart
import 'package:flutter/material.dart';
import '../models/exam_submission_result.dart';

class ExamResultsScreen extends StatelessWidget {
  final ExamSubmissionResult result;

  const ExamResultsScreen({Key? key, required this.result}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Exam Results'),
        backgroundColor: _getGradeColor(result.grade),
        foregroundColor: Colors.white,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            // Score Card
            Card(
              color: _getGradeColor(result.grade).withOpacity(0.1),
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  children: [
                    Text(
                      result.grade,
                      style: TextStyle(
                        fontSize: 72,
                        fontWeight: FontWeight.bold,
                        color: _getGradeColor(result.grade),
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      '${result.totalScore} / ${result.totalMaxScore}',
                      style: const TextStyle(fontSize: 32),
                    ),
                    Text(
                      '${result.percentage.toStringAsFixed(1)}%',
                      style: TextStyle(
                        fontSize: 24,
                        color: Colors.grey[600],
                      ),
                    ),
                  ],
                ),
              ),
            ),
            
            const SizedBox(height: 16),
            
            // Stats Row
            Row(
              children: [
                Expanded(
                  child: _buildStatCard(
                    'Answered',
                    '${result.questionsAnswered}/${result.totalQuestions}',
                    Icons.check_circle,
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: _buildStatCard(
                    'Correct',
                    '${result.results.where((r) => r.isCorrect).length}',
                    Icons.star,
                  ),
                ),
              ],
            ),
            
            const SizedBox(height: 24),
            
            // Question-wise Results
            const Text(
              'Question-wise Results',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 12),
            
            ...result.results.map((r) => Card(
              margin: const EdgeInsets.only(bottom: 8),
              child: ListTile(
                leading: CircleAvatar(
                  backgroundColor: r.isCorrect ? Colors.green : Colors.orange,
                  child: Text(
                    '${r.score}/${r.maxMarks}',
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 12,
                    ),
                  ),
                ),
                title: Text('Question ${r.questionNumber}'),
                subtitle: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      r.feedback,
                      style: TextStyle(color: Colors.grey[600]),
                    ),
                    if (r.extractedText != null) ...[
                      const SizedBox(height: 4),
                      Text(
                        'Your answer: ${r.extractedText}',
                        style: const TextStyle(
                          fontStyle: FontStyle.italic,
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ],
                ),
                trailing: r.isCorrect
                    ? const Icon(Icons.check, color: Colors.green)
                    : const Icon(Icons.close, color: Colors.red),
              ),
            )),
            
            const SizedBox(height: 24),
            
            // Done Button
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: () {
                  Navigator.popUntil(context, (route) => route.isFirst);
                },
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.indigo,
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(vertical: 16),
                ),
                child: const Text('Done'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStatCard(String label, String value, IconData icon) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            Icon(icon, size: 32, color: Colors.indigo),
            const SizedBox(height: 8),
            Text(value, style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold)),
            Text(label, style: TextStyle(color: Colors.grey[600])),
          ],
        ),
      ),
    );
  }

  Color _getGradeColor(String grade) {
    switch (grade) {
      case 'A+':
      case 'A':
        return Colors.green;
      case 'B+':
      case 'B':
        return Colors.blue;
      case 'C':
        return Colors.orange;
      case 'D':
        return Colors.deepOrange;
      default:
        return Colors.red;
    }
  }
}
```

### 7. Image Answer Picker Widget

```dart
// lib/widgets/image_answer_picker.dart
import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';

class ImageAnswerPicker extends StatefulWidget {
  final Function(File?) onImageSelected;
  final File? currentImage;

  const ImageAnswerPicker({
    Key? key,
    required this.onImageSelected,
    this.currentImage,
  }) : super(key: key);

  @override
  State<ImageAnswerPicker> createState() => _ImageAnswerPickerState();
}

class _ImageAnswerPickerState extends State<ImageAnswerPicker> {
  File? _image;
  final ImagePicker _picker = ImagePicker();

  @override
  void initState() {
    super.initState();
    _image = widget.currentImage;
  }

  Future<void> _pickImage(ImageSource source) async {
    final XFile? pickedFile = await _picker.pickImage(
      source: source,
      imageQuality: 85,
      maxWidth: 1920,
    );
    
    if (pickedFile != null) {
      setState(() {
        _image = File(pickedFile.path);
      });
      widget.onImageSelected(_image);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        if (_image != null) ...[
          ClipRRect(
            borderRadius: BorderRadius.circular(8),
            child: Image.file(
              _image!,
              height: 200,
              width: double.infinity,
              fit: BoxFit.cover,
            ),
          ),
          const SizedBox(height: 8),
          TextButton.icon(
            onPressed: () {
              setState(() => _image = null);
              widget.onImageSelected(null);
            },
            icon: const Icon(Icons.delete, color: Colors.red),
            label: const Text('Remove Image', style: TextStyle(color: Colors.red)),
          ),
        ] else ...[
          Container(
            height: 150,
            decoration: BoxDecoration(
              border: Border.all(color: Colors.grey),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.camera_alt, size: 48, color: Colors.grey),
                  const SizedBox(height: 8),
                  const Text('Upload handwritten answer'),
                ],
              ),
            ),
          ),
          const SizedBox(height: 8),
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              ElevatedButton.icon(
                onPressed: () => _pickImage(ImageSource.camera),
                icon: const Icon(Icons.camera),
                label: const Text('Camera'),
              ),
              const SizedBox(width: 16),
              OutlinedButton.icon(
                onPressed: () => _pickImage(ImageSource.gallery),
                icon: const Icon(Icons.photo_library),
                label: const Text('Gallery'),
              ),
            ],
          ),
        ],
      ],
    );
  }
}
```

---

## Error Handling

### HTTP Status Codes

| Code | Description |
|------|-------------|
| 200 | Success - Exam generated/evaluated |
| 400 | Bad Request - Invalid input |
| 500 | Server Error - Generation/evaluation failed |

### Error Response Format

```json
{
  "status": "error",
  "message": "Failed to generate exam.",
  "details": "Error description"
}
```

### Flutter Error Handling

```dart
try {
  final exam = await _apiService.generateExam(
    subject: 'Mathematics',
    grade: '2nd PUC',
  );
  // Use exam
} on ExamGenerationException catch (e) {
  // Handle API error
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(content: Text(e.message)),
  );
} on TimeoutException {
  // Handle timeout (exam generation takes 30-60 seconds)
  ScaffoldMessenger.of(context).showSnackBar(
    const SnackBar(content: Text('Request timed out. Please try again.')),
  );
} catch (e) {
  // Handle other errors
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(content: Text('Error: $e')),
  );
}
```

---

## Best Practices

1. **Timeout**: Set a 120-second timeout as AI generation takes time
2. **Loading State**: Show progress indicator during generation
3. **Offline Storage**: Cache generated exams locally for offline access
4. **Retry Logic**: Implement retry for failed requests
5. **User Feedback**: Show clear progress messages during generation

---

## Testing

### Generate Exam - cURL Test

```bash
curl -X POST "http://localhost:8080/api/exam/generate" \
  -H "Content-Type: application/json" \
  -d '{"subject":"Mathematics","grade":"2nd PUC"}'
```

### Generate Exam - PowerShell Test

```powershell
$body = @{subject="Mathematics"; grade="2nd PUC"} | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8080/api/exam/generate" `
  -Method POST -Body $body -ContentType "application/json" -TimeoutSec 120
```

### Submit Text Answer - cURL Test

```bash
curl -X POST "http://localhost:8080/api/exam/submit" \
  -F "examId=KAR-2PUC-20251207123456-A1B2C3D4" \
  -F "answers[0].questionId=A1" \
  -F "answers[0].questionNumber=1" \
  -F "answers[0].questionText=What is the order of matrix A = [[1,2],[3,4]]?" \
  -F "answers[0].correctAnswer=A) 2x2" \
  -F "answers[0].maxMarks=1" \
  -F "answers[0].textAnswer=A) 2x2"
```

### Submit Image Answer - cURL Test

```bash
curl -X POST "http://localhost:8080/api/exam/submit" \
  -F "examId=KAR-2PUC-20251207123456-A1B2C3D4" \
  -F "answers[0].questionId=B16" \
  -F "answers[0].questionNumber=16" \
  -F "answers[0].questionText=Find the derivative of sin(x)" \
  -F "answers[0].correctAnswer=cos(x)" \
  -F "answers[0].maxMarks=2" \
  -F "answers[0].imageFile=@handwritten-answer.jpg"
```

### Submit Answer - PowerShell Test

```powershell
# Create form data for text answer
$form = @{
    examId = "KAR-2PUC-20251207123456-A1B2C3D4"
    "answers[0].questionId" = "A1"
    "answers[0].questionNumber" = "1"
    "answers[0].questionText" = "What is 2+2?"
    "answers[0].correctAnswer" = "4"
    "answers[0].maxMarks" = "1"
    "answers[0].textAnswer" = "4"
}

Invoke-RestMethod -Uri "http://localhost:8080/api/exam/submit" `
  -Method POST -Form $form -TimeoutSec 120
```

---

## Support

For issues or questions, contact the backend team or check the API logs.
