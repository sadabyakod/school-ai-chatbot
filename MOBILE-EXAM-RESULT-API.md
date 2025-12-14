# SmartStudy Exam Result API Documentation

## 📱 Mobile App Integration Guide - Exam Results

**Base URL (Local):** `http://localhost:8080`  
**Base URL (Production):** `https://your-backend-url.azurewebsites.net`

---

## 🎯 Main Endpoint: Get Complete Exam Result

Returns **both MCQ and Subjective answers** with scores, feedback, and detailed analysis.

### `GET /api/exam/result/{examId}/{studentId}`

**Auth Required:** No (add if needed)

---

## 📋 Response Structure

```json
{
  "examId": "EXAM-2024-PHYSICS-001",
  "studentId": "STU-12345",
  "examTitle": "Physics - Electrostatics",
  
  "mcqScore": 15,
  "mcqTotalMarks": 20,
  "mcqResults": [
    {
      "questionId": "Q1-MCQ-001",
      "selectedOption": "B",
      "correctAnswer": "B",
      "isCorrect": true,
      "marksAwarded": 1
    },
    {
      "questionId": "Q1-MCQ-002",
      "selectedOption": "A",
      "correctAnswer": "C",
      "isCorrect": false,
      "marksAwarded": 0
    }
  ],
  
  "subjectiveScore": 28.5,
  "subjectiveTotalMarks": 40,
  "subjectiveResults": [
    {
      "questionId": "Q2-SUB-001",
      "questionNumber": 1,
      "questionText": "Explain Coulomb's Law and its mathematical expression.",
      "earnedMarks": 8.5,
      "maxMarks": 10,
      "isFullyCorrect": false,
      "expectedAnswer": "Coulomb's Law states that the force between two point charges...",
      "studentAnswerEcho": "Coulomb's law says force is proportional to charges...",
      "stepAnalysis": [
        {
          "step": 1,
          "description": "Statement of the law",
          "isCorrect": true,
          "marksAwarded": 2,
          "maxMarksForStep": 2,
          "feedback": "Correctly stated the law"
        },
        {
          "step": 2,
          "description": "Mathematical formula",
          "isCorrect": true,
          "marksAwarded": 3,
          "maxMarksForStep": 3,
          "feedback": "Formula written correctly with proper notation"
        },
        {
          "step": 3,
          "description": "Explanation of terms",
          "isCorrect": false,
          "marksAwarded": 1.5,
          "maxMarksForStep": 3,
          "feedback": "Missed explaining permittivity constant"
        },
        {
          "step": 4,
          "description": "Units and dimensions",
          "isCorrect": true,
          "marksAwarded": 2,
          "maxMarksForStep": 2,
          "feedback": "Units correctly mentioned"
        }
      ],
      "overallFeedback": "Good understanding of Coulomb's Law. Include permittivity constant explanation for full marks."
    }
  ],
  
  "grandScore": 43.5,
  "grandTotalMarks": 60,
  "percentage": 72.5,
  "grade": "B+",
  "passed": true,
  "evaluatedAt": "2025-12-14 15:30:45"
}
```

---

## 📊 Response Fields Reference

### Top Level

| Field | Type | Description |
|-------|------|-------------|
| `examId` | string | Unique exam identifier |
| `studentId` | string | Student identifier |
| `examTitle` | string | Exam title (Subject - Chapter) |
| `grandScore` | number | Total score (MCQ + Subjective) |
| `grandTotalMarks` | number | Maximum possible marks |
| `percentage` | number | Score percentage (0-100) |
| `grade` | string | Letter grade (A+, A, B+, B, C+, C, D, F) |
| `passed` | boolean | Whether student passed (≥35%) |
| `evaluatedAt` | string | Evaluation timestamp |

### MCQ Section

| Field | Type | Description |
|-------|------|-------------|
| `mcqScore` | number | Total MCQ marks earned |
| `mcqTotalMarks` | number | Maximum MCQ marks |
| `mcqResults` | array | List of MCQ answer results |

### MCQ Result Item

| Field | Type | Description |
|-------|------|-------------|
| `questionId` | string | Question identifier |
| `selectedOption` | string | Student's selected option (A/B/C/D) |
| `correctAnswer` | string | Correct option |
| `isCorrect` | boolean | Whether answer was correct |
| `marksAwarded` | number | Marks given (0 or full marks) |

### Subjective Section

| Field | Type | Description |
|-------|------|-------------|
| `subjectiveScore` | number | Total subjective marks earned |
| `subjectiveTotalMarks` | number | Maximum subjective marks |
| `subjectiveResults` | array | List of subjective answer results |

### Subjective Result Item

| Field | Type | Description |
|-------|------|-------------|
| `questionId` | string | Question identifier |
| `questionNumber` | number | Question number in exam |
| `questionText` | string | Full question text |
| `earnedMarks` | number | Marks awarded |
| `maxMarks` | number | Maximum marks for question |
| `isFullyCorrect` | boolean | Whether full marks awarded |
| `expectedAnswer` | string | Model/correct answer |
| `studentAnswerEcho` | string | Student's submitted answer |
| `stepAnalysis` | array | Step-by-step rubric evaluation |
| `overallFeedback` | string | AI-generated feedback summary |

### Step Analysis Item

| Field | Type | Description |
|-------|------|-------------|
| `step` | number | Step number |
| `description` | string | What this step evaluates |
| `isCorrect` | boolean | Whether step was correct |
| `marksAwarded` | number | Marks for this step |
| `maxMarksForStep` | number | Maximum marks for step |
| `feedback` | string | Feedback for this step |

---

## 📱 Mobile Implementation Examples

### Swift (iOS)

```swift
import Foundation

struct ExamResult: Codable {
    let examId: String
    let studentId: String
    let examTitle: String
    let mcqScore: Int
    let mcqTotalMarks: Int
    let mcqResults: [McqResult]
    let subjectiveScore: Double
    let subjectiveTotalMarks: Double
    let subjectiveResults: [SubjectiveResult]
    let grandScore: Double
    let grandTotalMarks: Double
    let percentage: Double
    let grade: String
    let passed: Bool
    let evaluatedAt: String?
}

struct McqResult: Codable {
    let questionId: String
    let selectedOption: String
    let correctAnswer: String
    let isCorrect: Bool
    let marksAwarded: Int
}

struct SubjectiveResult: Codable {
    let questionId: String
    let questionNumber: Int
    let questionText: String
    let earnedMarks: Double
    let maxMarks: Double
    let isFullyCorrect: Bool
    let expectedAnswer: String
    let studentAnswerEcho: String
    let stepAnalysis: [StepAnalysis]
    let overallFeedback: String
}

struct StepAnalysis: Codable {
    let step: Int
    let description: String
    let isCorrect: Bool
    let marksAwarded: Double
    let maxMarksForStep: Double
    let feedback: String
}

class ExamResultService {
    static let baseURL = "http://localhost:8080"
    
    func getExamResult(examId: String, studentId: String) async throws -> ExamResult {
        let url = URL(string: "\(Self.baseURL)/api/exam/result/\(examId)/\(studentId)")!
        let (data, _) = try await URLSession.shared.data(from: url)
        return try JSONDecoder().decode(ExamResult.self, from: data)
    }
}

// Usage in SwiftUI View
struct ExamResultView: View {
    @State private var result: ExamResult?
    
    var body: some View {
        ScrollView {
            if let result = result {
                // Score Summary Card
                VStack(spacing: 16) {
                    Text(result.examTitle)
                        .font(.title2)
                        .bold()
                    
                    HStack(spacing: 40) {
                        ScoreCircle(score: result.percentage, grade: result.grade)
                        
                        VStack(alignment: .leading) {
                            Text("MCQ: \(result.mcqScore)/\(result.mcqTotalMarks)")
                            Text("Subjective: \(String(format: "%.1f", result.subjectiveScore))/\(String(format: "%.0f", result.subjectiveTotalMarks))")
                            Text("Total: \(String(format: "%.1f", result.grandScore))/\(String(format: "%.0f", result.grandTotalMarks))")
                        }
                    }
                }
                .padding()
                .background(Color.green.opacity(0.1))
                .cornerRadius(12)
                
                // MCQ Results
                Section(header: Text("MCQ Answers").font(.headline)) {
                    ForEach(result.mcqResults, id: \.questionId) { mcq in
                        McqResultRow(result: mcq)
                    }
                }
                
                // Subjective Results with Feedback
                Section(header: Text("Subjective Answers").font(.headline)) {
                    ForEach(result.subjectiveResults, id: \.questionId) { sub in
                        SubjectiveResultCard(result: sub)
                    }
                }
            }
        }
    }
}
```

### Kotlin (Android)

```kotlin
import retrofit2.http.GET
import retrofit2.http.Path

// Data Classes
data class ExamResult(
    val examId: String,
    val studentId: String,
    val examTitle: String,
    val mcqScore: Int,
    val mcqTotalMarks: Int,
    val mcqResults: List<McqResult>,
    val subjectiveScore: Double,
    val subjectiveTotalMarks: Double,
    val subjectiveResults: List<SubjectiveResult>,
    val grandScore: Double,
    val grandTotalMarks: Double,
    val percentage: Double,
    val grade: String,
    val passed: Boolean,
    val evaluatedAt: String?
)

data class McqResult(
    val questionId: String,
    val selectedOption: String,
    val correctAnswer: String,
    val isCorrect: Boolean,
    val marksAwarded: Int
)

data class SubjectiveResult(
    val questionId: String,
    val questionNumber: Int,
    val questionText: String,
    val earnedMarks: Double,
    val maxMarks: Double,
    val isFullyCorrect: Boolean,
    val expectedAnswer: String,
    val studentAnswerEcho: String,
    val stepAnalysis: List<StepAnalysis>,
    val overallFeedback: String
)

data class StepAnalysis(
    val step: Int,
    val description: String,
    val isCorrect: Boolean,
    val marksAwarded: Double,
    val maxMarksForStep: Double,
    val feedback: String
)

// Retrofit API Interface
interface ExamApi {
    @GET("api/exam/result/{examId}/{studentId}")
    suspend fun getExamResult(
        @Path("examId") examId: String,
        @Path("studentId") studentId: String
    ): ExamResult
}

// ViewModel
class ExamResultViewModel : ViewModel() {
    private val _result = MutableStateFlow<ExamResult?>(null)
    val result: StateFlow<ExamResult?> = _result
    
    fun loadResult(examId: String, studentId: String) {
        viewModelScope.launch {
            try {
                _result.value = api.getExamResult(examId, studentId)
            } catch (e: Exception) {
                Log.e("ExamResult", "Failed to load result", e)
            }
        }
    }
}

// Compose UI
@Composable
fun ExamResultScreen(result: ExamResult) {
    LazyColumn(modifier = Modifier.padding(16.dp)) {
        // Score Summary
        item {
            Card(modifier = Modifier.fillMaxWidth()) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Text(result.examTitle, style = MaterialTheme.typography.h6)
                    Spacer(modifier = Modifier.height(8.dp))
                    
                    Row(horizontalArrangement = Arrangement.SpaceBetween) {
                        Column {
                            Text("Grade: ${result.grade}", style = MaterialTheme.typography.h4)
                            Text("${result.percentage}%")
                        }
                        Column {
                            Text("MCQ: ${result.mcqScore}/${result.mcqTotalMarks}")
                            Text("Subjective: ${result.subjectiveScore}/${result.subjectiveTotalMarks}")
                        }
                    }
                }
            }
        }
        
        // MCQ Section
        item { Text("MCQ Results", style = MaterialTheme.typography.h6) }
        items(result.mcqResults) { mcq ->
            McqResultCard(mcq)
        }
        
        // Subjective Section with Feedback
        item { Text("Subjective Results", style = MaterialTheme.typography.h6) }
        items(result.subjectiveResults) { sub ->
            SubjectiveResultCard(sub)
        }
    }
}

@Composable
fun SubjectiveResultCard(result: SubjectiveResult) {
    Card(modifier = Modifier.fillMaxWidth().padding(vertical = 8.dp)) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text("Q${result.questionNumber}: ${result.questionText}")
            Text("Score: ${result.earnedMarks}/${result.maxMarks}", 
                 color = if (result.isFullyCorrect) Color.Green else Color.Orange)
            
            Spacer(modifier = Modifier.height(8.dp))
            Text("Your Answer:", style = MaterialTheme.typography.caption)
            Text(result.studentAnswerEcho)
            
            Spacer(modifier = Modifier.height(8.dp))
            Text("Feedback:", style = MaterialTheme.typography.caption)
            Text(result.overallFeedback, color = MaterialTheme.colors.primary)
            
            // Step Analysis
            result.stepAnalysis.forEach { step ->
                Row(
                    modifier = Modifier.fillMaxWidth().padding(vertical = 4.dp),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Icon(
                        if (step.isCorrect) Icons.Default.Check else Icons.Default.Close,
                        contentDescription = null,
                        tint = if (step.isCorrect) Color.Green else Color.Red
                    )
                    Text(step.description, modifier = Modifier.weight(1f))
                    Text("${step.marksAwarded}/${step.maxMarksForStep}")
                }
            }
        }
    }
}
```

### React Native / JavaScript

```javascript
const BASE_URL = 'http://localhost:8080';

// Fetch exam result
async function getExamResult(examId, studentId) {
  const response = await fetch(
    `${BASE_URL}/api/exam/result/${examId}/${studentId}`
  );
  
  if (!response.ok) {
    throw new Error(`Failed to fetch result: ${response.status}`);
  }
  
  return await response.json();
}

// React Native Component
import React, { useEffect, useState } from 'react';
import { View, Text, ScrollView, StyleSheet } from 'react-native';

const ExamResultScreen = ({ route }) => {
  const { examId, studentId } = route.params;
  const [result, setResult] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadResult();
  }, []);

  const loadResult = async () => {
    try {
      const data = await getExamResult(examId, studentId);
      setResult(data);
    } catch (error) {
      console.error('Error loading result:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <Text>Loading...</Text>;
  if (!result) return <Text>No result found</Text>;

  return (
    <ScrollView style={styles.container}>
      {/* Score Summary */}
      <View style={styles.summaryCard}>
        <Text style={styles.title}>{result.examTitle}</Text>
        <View style={styles.scoreRow}>
          <View style={styles.gradeCircle}>
            <Text style={styles.grade}>{result.grade}</Text>
            <Text style={styles.percentage}>{result.percentage}%</Text>
          </View>
          <View>
            <Text>MCQ: {result.mcqScore}/{result.mcqTotalMarks}</Text>
            <Text>Subjective: {result.subjectiveScore.toFixed(1)}/{result.subjectiveTotalMarks}</Text>
            <Text style={styles.total}>Total: {result.grandScore.toFixed(1)}/{result.grandTotalMarks}</Text>
          </View>
        </View>
        <Text style={result.passed ? styles.passed : styles.failed}>
          {result.passed ? '✅ PASSED' : '❌ FAILED'}
        </Text>
      </View>

      {/* MCQ Results */}
      <Text style={styles.sectionTitle}>📝 MCQ Answers</Text>
      {result.mcqResults.map((mcq, index) => (
        <View key={mcq.questionId} style={styles.mcqRow}>
          <Text style={mcq.isCorrect ? styles.correct : styles.wrong}>
            {mcq.isCorrect ? '✓' : '✗'}
          </Text>
          <Text>Q{index + 1}: {mcq.selectedOption}</Text>
          {!mcq.isCorrect && <Text style={styles.correctAns}>(Correct: {mcq.correctAnswer})</Text>}
        </View>
      ))}

      {/* Subjective Results with Feedback */}
      <Text style={styles.sectionTitle}>📖 Subjective Answers</Text>
      {result.subjectiveResults.map((sub) => (
        <View key={sub.questionId} style={styles.subjectiveCard}>
          <Text style={styles.questionText}>Q{sub.questionNumber}: {sub.questionText}</Text>
          <Text style={styles.score}>
            Score: {sub.earnedMarks}/{sub.maxMarks} 
            {sub.isFullyCorrect ? ' ✅' : ''}
          </Text>
          
          <Text style={styles.label}>Your Answer:</Text>
          <Text style={styles.answer}>{sub.studentAnswerEcho}</Text>
          
          <Text style={styles.label}>Step-by-Step Analysis:</Text>
          {sub.stepAnalysis.map((step) => (
            <View key={step.step} style={styles.stepRow}>
              <Text style={step.isCorrect ? styles.correct : styles.wrong}>
                {step.isCorrect ? '✓' : '✗'}
              </Text>
              <View style={styles.stepContent}>
                <Text>{step.description}</Text>
                <Text style={styles.stepFeedback}>{step.feedback}</Text>
              </View>
              <Text>{step.marksAwarded}/{step.maxMarksForStep}</Text>
            </View>
          ))}
          
          <View style={styles.feedbackBox}>
            <Text style={styles.feedbackLabel}>💡 Overall Feedback:</Text>
            <Text style={styles.feedback}>{sub.overallFeedback}</Text>
          </View>
        </View>
      ))}
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1, padding: 16, backgroundColor: '#f5f5f5' },
  summaryCard: { backgroundColor: '#fff', padding: 16, borderRadius: 12, marginBottom: 16 },
  title: { fontSize: 20, fontWeight: 'bold', marginBottom: 12 },
  scoreRow: { flexDirection: 'row', alignItems: 'center', gap: 20 },
  gradeCircle: { width: 80, height: 80, borderRadius: 40, backgroundColor: '#4CAF50', justifyContent: 'center', alignItems: 'center' },
  grade: { fontSize: 24, fontWeight: 'bold', color: '#fff' },
  percentage: { fontSize: 14, color: '#fff' },
  total: { fontWeight: 'bold', marginTop: 4 },
  passed: { color: 'green', fontWeight: 'bold', marginTop: 8 },
  failed: { color: 'red', fontWeight: 'bold', marginTop: 8 },
  sectionTitle: { fontSize: 18, fontWeight: 'bold', marginTop: 16, marginBottom: 8 },
  mcqRow: { flexDirection: 'row', alignItems: 'center', gap: 8, padding: 8, backgroundColor: '#fff', marginBottom: 4, borderRadius: 8 },
  correct: { color: 'green', fontWeight: 'bold' },
  wrong: { color: 'red', fontWeight: 'bold' },
  correctAns: { color: '#666', fontStyle: 'italic' },
  subjectiveCard: { backgroundColor: '#fff', padding: 16, borderRadius: 12, marginBottom: 12 },
  questionText: { fontWeight: 'bold', marginBottom: 8 },
  score: { fontSize: 16, color: '#2196F3', marginBottom: 12 },
  label: { fontWeight: '600', color: '#666', marginTop: 8 },
  answer: { padding: 8, backgroundColor: '#f0f0f0', borderRadius: 8, marginTop: 4 },
  stepRow: { flexDirection: 'row', alignItems: 'flex-start', gap: 8, padding: 8, borderBottomWidth: 1, borderBottomColor: '#eee' },
  stepContent: { flex: 1 },
  stepFeedback: { fontSize: 12, color: '#666', fontStyle: 'italic' },
  feedbackBox: { marginTop: 12, padding: 12, backgroundColor: '#E3F2FD', borderRadius: 8 },
  feedbackLabel: { fontWeight: 'bold', marginBottom: 4 },
  feedback: { color: '#1976D2' }
});

export default ExamResultScreen;
```

### Flutter / Dart

```dart
import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

// Models
class ExamResult {
  final String examId;
  final String studentId;
  final String examTitle;
  final int mcqScore;
  final int mcqTotalMarks;
  final List<McqResult> mcqResults;
  final double subjectiveScore;
  final double subjectiveTotalMarks;
  final List<SubjectiveResult> subjectiveResults;
  final double grandScore;
  final double grandTotalMarks;
  final double percentage;
  final String grade;
  final bool passed;
  final String? evaluatedAt;

  ExamResult.fromJson(Map<String, dynamic> json)
      : examId = json['examId'],
        studentId = json['studentId'],
        examTitle = json['examTitle'],
        mcqScore = json['mcqScore'],
        mcqTotalMarks = json['mcqTotalMarks'],
        mcqResults = (json['mcqResults'] as List)
            .map((e) => McqResult.fromJson(e))
            .toList(),
        subjectiveScore = json['subjectiveScore'].toDouble(),
        subjectiveTotalMarks = json['subjectiveTotalMarks'].toDouble(),
        subjectiveResults = (json['subjectiveResults'] as List)
            .map((e) => SubjectiveResult.fromJson(e))
            .toList(),
        grandScore = json['grandScore'].toDouble(),
        grandTotalMarks = json['grandTotalMarks'].toDouble(),
        percentage = json['percentage'].toDouble(),
        grade = json['grade'],
        passed = json['passed'],
        evaluatedAt = json['evaluatedAt'];
}

class McqResult {
  final String questionId;
  final String selectedOption;
  final String correctAnswer;
  final bool isCorrect;
  final int marksAwarded;

  McqResult.fromJson(Map<String, dynamic> json)
      : questionId = json['questionId'],
        selectedOption = json['selectedOption'],
        correctAnswer = json['correctAnswer'],
        isCorrect = json['isCorrect'],
        marksAwarded = json['marksAwarded'];
}

class SubjectiveResult {
  final String questionId;
  final int questionNumber;
  final String questionText;
  final double earnedMarks;
  final double maxMarks;
  final bool isFullyCorrect;
  final String expectedAnswer;
  final String studentAnswerEcho;
  final List<StepAnalysis> stepAnalysis;
  final String overallFeedback;

  SubjectiveResult.fromJson(Map<String, dynamic> json)
      : questionId = json['questionId'],
        questionNumber = json['questionNumber'],
        questionText = json['questionText'],
        earnedMarks = json['earnedMarks'].toDouble(),
        maxMarks = json['maxMarks'].toDouble(),
        isFullyCorrect = json['isFullyCorrect'],
        expectedAnswer = json['expectedAnswer'],
        studentAnswerEcho = json['studentAnswerEcho'],
        stepAnalysis = (json['stepAnalysis'] as List)
            .map((e) => StepAnalysis.fromJson(e))
            .toList(),
        overallFeedback = json['overallFeedback'];
}

class StepAnalysis {
  final int step;
  final String description;
  final bool isCorrect;
  final double marksAwarded;
  final double maxMarksForStep;
  final String feedback;

  StepAnalysis.fromJson(Map<String, dynamic> json)
      : step = json['step'],
        description = json['description'],
        isCorrect = json['isCorrect'],
        marksAwarded = json['marksAwarded'].toDouble(),
        maxMarksForStep = json['maxMarksForStep'].toDouble(),
        feedback = json['feedback'];
}

// API Service
class ExamResultService {
  static const baseUrl = 'http://localhost:8080';

  Future<ExamResult> getExamResult(String examId, String studentId) async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/exam/result/$examId/$studentId'),
    );

    if (response.statusCode == 200) {
      return ExamResult.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Failed to load exam result');
    }
  }
}

// UI Widget
class ExamResultScreen extends StatelessWidget {
  final ExamResult result;

  const ExamResultScreen({Key? key, required this.result}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Exam Result')),
      body: SingleChildScrollView(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Score Summary Card
            Card(
              color: result.passed ? Colors.green[50] : Colors.red[50],
              child: Padding(
                padding: EdgeInsets.all(16),
                child: Column(
                  children: [
                    Text(result.examTitle,
                        style: Theme.of(context).textTheme.titleLarge),
                    SizedBox(height: 16),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceAround,
                      children: [
                        _buildGradeCircle(),
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text('MCQ: ${result.mcqScore}/${result.mcqTotalMarks}'),
                            Text('Subjective: ${result.subjectiveScore.toStringAsFixed(1)}/${result.subjectiveTotalMarks.toStringAsFixed(0)}'),
                            Text('Total: ${result.grandScore.toStringAsFixed(1)}/${result.grandTotalMarks.toStringAsFixed(0)}',
                                style: TextStyle(fontWeight: FontWeight.bold)),
                          ],
                        ),
                      ],
                    ),
                    SizedBox(height: 8),
                    Text(
                      result.passed ? '✅ PASSED' : '❌ FAILED',
                      style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                        color: result.passed ? Colors.green : Colors.red,
                      ),
                    ),
                  ],
                ),
              ),
            ),

            // MCQ Section
            SizedBox(height: 24),
            Text('📝 MCQ Results', style: Theme.of(context).textTheme.titleMedium),
            ...result.mcqResults.asMap().entries.map((entry) {
              final idx = entry.key;
              final mcq = entry.value;
              return ListTile(
                leading: Icon(
                  mcq.isCorrect ? Icons.check_circle : Icons.cancel,
                  color: mcq.isCorrect ? Colors.green : Colors.red,
                ),
                title: Text('Q${idx + 1}: Selected ${mcq.selectedOption}'),
                subtitle: mcq.isCorrect
                    ? null
                    : Text('Correct: ${mcq.correctAnswer}'),
                trailing: Text('${mcq.marksAwarded} marks'),
              );
            }),

            // Subjective Section
            SizedBox(height: 24),
            Text('📖 Subjective Results', style: Theme.of(context).textTheme.titleMedium),
            ...result.subjectiveResults.map((sub) => _buildSubjectiveCard(sub)),
          ],
        ),
      ),
    );
  }

  Widget _buildGradeCircle() {
    return Container(
      width: 80,
      height: 80,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        color: Colors.blue,
      ),
      child: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(result.grade,
                style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: Colors.white)),
            Text('${result.percentage.toStringAsFixed(1)}%',
                style: TextStyle(fontSize: 12, color: Colors.white)),
          ],
        ),
      ),
    );
  }

  Widget _buildSubjectiveCard(SubjectiveResult sub) {
    return Card(
      margin: EdgeInsets.symmetric(vertical: 8),
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Q${sub.questionNumber}: ${sub.questionText}',
                style: TextStyle(fontWeight: FontWeight.bold)),
            SizedBox(height: 8),
            Text('Score: ${sub.earnedMarks}/${sub.maxMarks} ${sub.isFullyCorrect ? "✅" : ""}',
                style: TextStyle(color: Colors.blue, fontSize: 16)),
            
            SizedBox(height: 12),
            Text('Your Answer:', style: TextStyle(fontWeight: FontWeight.w600, color: Colors.grey[600])),
            Container(
              padding: EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: Colors.grey[100],
                borderRadius: BorderRadius.circular(8),
              ),
              child: Text(sub.studentAnswerEcho),
            ),
            
            SizedBox(height: 12),
            Text('Step Analysis:', style: TextStyle(fontWeight: FontWeight.w600)),
            ...sub.stepAnalysis.map((step) => ListTile(
              dense: true,
              leading: Icon(
                step.isCorrect ? Icons.check : Icons.close,
                color: step.isCorrect ? Colors.green : Colors.red,
                size: 20,
              ),
              title: Text(step.description),
              subtitle: Text(step.feedback, style: TextStyle(fontStyle: FontStyle.italic)),
              trailing: Text('${step.marksAwarded}/${step.maxMarksForStep}'),
            )),
            
            Container(
              margin: EdgeInsets.only(top: 12),
              padding: EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Colors.blue[50],
                borderRadius: BorderRadius.circular(8),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('💡 Overall Feedback', style: TextStyle(fontWeight: FontWeight.bold)),
                  SizedBox(height: 4),
                  Text(sub.overallFeedback, style: TextStyle(color: Colors.blue[700])),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
```

---

## 🔗 Related Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/exam/generate` | POST | Generate a new exam |
| `/api/exam/submit-mcq` | POST | Submit MCQ answers |
| `/api/exam/upload-written` | POST | Upload written answers |
| `/api/exam/submission-status/{id}` | GET | Check evaluation status |
| `/api/exam/result/{examId}/{studentId}` | GET | **Get complete result** ⭐ |

---

## ⚠️ Error Responses

```json
{
  "error": "No submission found for this exam and student"
}
```

| Status | Error | Solution |
|--------|-------|----------|
| 404 | Exam not found | Verify examId exists |
| 404 | No submission found | Student hasn't submitted yet |
| 500 | Internal Server Error | Check server logs |

---

## 🏷️ Grading Scale

| Percentage | Grade |
|------------|-------|
| ≥ 90% | A+ |
| ≥ 80% | A |
| ≥ 70% | B+ |
| ≥ 60% | B |
| ≥ 50% | C+ |
| ≥ 40% | C |
| ≥ 35% | D (Pass) |
| < 35% | F (Fail) |

---

**Last Updated:** December 14, 2025  
**Version:** 1.0.0
