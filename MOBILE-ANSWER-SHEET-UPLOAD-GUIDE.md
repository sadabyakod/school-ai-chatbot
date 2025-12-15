# 📱 Mobile UI - Answer Sheet Evaluation Flow

Complete guide for implementing answer sheet upload and evaluation in your mobile app.

---

## 📋 Complete Flow Overview

```
1. Student uploads answer sheet (images/PDF)
   ↓
2. Backend stores files in Azure Blob Storage
   ↓
3. Poll status (check WrittenSubmissions.Status column):
   Status 0: Uploaded (waiting for OCR)
   Status 1: OCR Complete (waiting for evaluation)
   Status 2: Evaluation Complete ✓
   ↓
4. When Status = 2:
   - EvaluationResultBlobPath is populated in WrittenSubmissions table
   - Status response includes evaluationResultBlobPath field
   - Fetch complete results OR download blob directly
```

---

## 🚀 API Endpoints

### Base URL
```
Production: https://your-api-url.azurewebsites.net
Development: http://localhost:8080
```

**All endpoints start with:** `/api/exam`

---

## 1️⃣ Upload Answer Sheet

### Endpoint
```
POST /api/exam/upload-written
Content-Type: multipart/form-data
```

### Request Parameters
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| examId | string | Yes | Exam identifier (e.g., "Karnataka_2nd_PUC_Math_2024_25") |
| studentId | string | Yes | Student identifier (e.g., "STUDENT-12345") |
| files | file[] | Yes | Answer sheet images/PDF (max 20 files, 10MB each) |

### Supported File Types
- **Images:** `.jpg`, `.jpeg`, `.png`, `.webp`
- **Documents:** `.pdf`

### File Size Limits
- **Per file:** 10MB maximum
- **Total files:** 20 files maximum per upload

### Example Code

**Flutter/Dart:**
```dart
import 'package:http/http.dart' as http;
import 'dart:io';

Future<Map<String, dynamic>> uploadAnswerSheet({
  required String examId,
  required String studentId,
  required List<File> files,
}) async {
  final uri = Uri.parse('http://your-api-url.com/api/exam/upload-written');
  final request = http.MultipartRequest('POST', uri);
  
  // Add form fields
  request.fields['examId'] = examId;
  request.fields['studentId'] = studentId;
  
  // Add files
  for (var file in files) {
    request.files.add(await http.MultipartFile.fromPath('files', file.path));
  }
  
  // Send request
  final response = await request.send();
  final responseBody = await response.stream.bytesToString();
  
  if (response.statusCode == 200) {
    return jsonDecode(responseBody);
  } else {
    throw Exception('Upload failed: ${response.statusCode}');
  }
}

// Usage
try {
  final result = await uploadAnswerSheet(
    examId: 'Karnataka_2nd_PUC_Math_2024_25',
    studentId: 'STUDENT-12345',
    files: [File('/path/to/answer1.jpg'), File('/path/to/answer2.jpg')],
  );
  
  print('Submission ID: ${result['writtenSubmissionId']}');
  print('Status: ${result['status']}');
  print('Message: ${result['message']}');
} catch (e) {
  print('Error: $e');
}
```

**React Native/JavaScript:**
```javascript
const uploadAnswerSheet = async (examId, studentId, files) => {
  const formData = new FormData();
  formData.append('examId', examId);
  formData.append('studentId', studentId);
  
  // Add files
  files.forEach((file, index) => {
    formData.append('files', {
      uri: file.uri,
      type: file.type || 'image/jpeg',
      name: file.name || `answer${index + 1}.jpg`,
    });
  });
  
  try {
    const response = await fetch('http://your-api-url.com/api/exam/upload-written', {
      method: 'POST',
      body: formData,
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    
    const result = await response.json();
    
    if (response.ok) {
      console.log('Submission ID:', result.writtenSubmissionId);
      console.log('Status:', result.status);
      return result;
    } else {
      throw new Error(result.error || 'Upload failed');
    }
  } catch (error) {
    console.error('Upload error:', error);
    throw error;
  }
};

// Usage
const files = [
  { uri: 'file:///path/to/image1.jpg', name: 'page1.jpg', type: 'image/jpeg' },
  { uri: 'file:///path/to/image2.jpg', name: 'page2.jpg', type: 'image/jpeg' },
];

uploadAnswerSheet('Karnataka_2nd_PUC_Math_2024_25', 'STUDENT-12345', files)
  .then(result => console.log('Success:', result))
  .catch(error => console.error('Error:', error));
```

**Kotlin/Android:**
```kotlin
import okhttp3.*
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.RequestBody.Companion.asRequestBody
import java.io.File

fun uploadAnswerSheet(
    examId: String,
    studentId: String,
    files: List<File>,
    callback: (String?, String?) -> Unit
) {
    val client = OkHttpClient()
    
    val requestBody = MultipartBody.Builder()
        .setType(MultipartBody.FORM)
        .addFormDataPart("examId", examId)
        .addFormDataPart("studentId", studentId)
        .apply {
            files.forEach { file ->
                addFormDataPart(
                    "files",
                    file.name,
                    file.asRequestBody("image/jpeg".toMediaType())
                )
            }
        }
        .build()
    
    val request = Request.Builder()
        .url("http://your-api-url.com/api/exam/upload-written")
        .post(requestBody)
        .build()
    
    client.newCall(request).enqueue(object : Callback {
        override fun onFailure(call: Call, e: IOException) {
            callback(null, e.message)
        }
        
        override fun onResponse(call: Call, response: Response) {
            val body = response.body?.string()
            if (response.isSuccessful) {
                callback(body, null)
            } else {
                callback(null, "Upload failed: ${response.code}")
            }
        }
    })
}

// Usage
val files = listOf(File("/path/to/answer1.jpg"), File("/path/to/answer2.jpg"))
uploadAnswerSheet("Karnataka_2nd_PUC_Math_2024_25", "STUDENT-12345", files) { result, error ->
    if (error == null) {
        println("Success: $result")
    } else {
        println("Error: $error")
    }
}
```

**Swift/iOS:**
```swift
import Foundation

func uploadAnswerSheet(
    examId: String,
    studentId: String,
    files: [URL],
    completion: @escaping (Result<[String: Any], Error>) -> Void
) {
    let url = URL(string: "http://your-api-url.com/api/exam/upload-written")!
    var request = URLRequest(url: url)
    request.httpMethod = "POST"
    
    let boundary = UUID().uuidString
    request.setValue("multipart/form-data; boundary=\(boundary)", forHTTPHeaderField: "Content-Type")
    
    var body = Data()
    
    // Add form fields
    body.append("--\(boundary)\r\n".data(using: .utf8)!)
    body.append("Content-Disposition: form-data; name=\"examId\"\r\n\r\n".data(using: .utf8)!)
    body.append("\(examId)\r\n".data(using: .utf8)!)
    
    body.append("--\(boundary)\r\n".data(using: .utf8)!)
    body.append("Content-Disposition: form-data; name=\"studentId\"\r\n\r\n".data(using: .utf8)!)
    body.append("\(studentId)\r\n".data(using: .utf8)!)
    
    // Add files
    for (index, fileURL) in files.enumerated() {
        if let fileData = try? Data(contentsOf: fileURL) {
            body.append("--\(boundary)\r\n".data(using: .utf8)!)
            body.append("Content-Disposition: form-data; name=\"files\"; filename=\"page\(index + 1).jpg\"\r\n".data(using: .utf8)!)
            body.append("Content-Type: image/jpeg\r\n\r\n".data(using: .utf8)!)
            body.append(fileData)
            body.append("\r\n".data(using: .utf8)!)
        }
    }
    
    body.append("--\(boundary)--\r\n".data(using: .utf8)!)
    request.httpBody = body
    
    URLSession.shared.dataTask(with: request) { data, response, error in
        if let error = error {
            completion(.failure(error))
            return
        }
        
        guard let data = data,
              let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any] else {
            completion(.failure(NSError(domain: "", code: -1, userInfo: [NSLocalizedDescriptionKey: "Invalid response"])))
            return
        }
        
        completion(.success(json))
    }.resume()
}

// Usage
let files = [URL(fileURLWithPath: "/path/to/answer1.jpg")]
uploadAnswerSheet(examId: "Karnataka_2nd_PUC_Math_2024_25", studentId: "STUDENT-12345", files: files) { result in
    switch result {
    case .success(let data):
        print("Submission ID:", data["writtenSubmissionId"] ?? "")
   message": "✅ Answer sheet uploaded successfully! Processing will begin shortly.",
  "blobPaths": [
    "Karnataka_2nd_PUC_Math_2024_25/a1b2c3d4.../page-0.jpg",
    "Karnataka_2nd_PUC_Math_2024_25/a1b2c3d4.../page-1.jpg"
  ],
  "examId": "Karnataka_2nd_PUC_Math_2024_25",
  "studentId": "STUDENT-12345",
  "queuedForProcessing": true
}
```

### Error Responses
```json
// 400 - Bad Request (No files)
{
  "error": "No files uploaded"
}

// 400 - Bad Request (Too many files)
{
  "error": "Maximum 20 files allowed"
}

// 400 - Bad Request (File too large)
{
  "error": "File answer.jpg exceeds maximum size of 10MB"
}

// 400 - Bad Request (Invalid file type)
{
  "error": "Invalid file type. Allowed: .jpg, .jpeg, .png, .pdf, .webp"
}

// 404 - Exam Not Found
{
  "error": "Exam Karnataka_2nd_PUC_Math_2024_25 not found. Please generate the exam first using /api/exam/generate"
}

// 409 - Duplicate Submission
{
  "error": "Student STUDENT-12345 has already submitted answers for exam Karnataka_2nd_PUC_Math_2024_25"
}

// 500 - Internal Server Error
{
  "error": "Internal server error",
  "correlationId": "req-12345
  "error": "No files provided"
}

// 404 - Exam Not Found
{
  "error": "Exam Karnataka_2nd_PUC_Math_2024_25 not found"
}

// 409 - Duplicate Submission
{
  "error": "Student has already submitted answers for this exam"
}
```

---

## 2️⃣ Check Evaluation Status

### Endpoint
```
GET /api/exam/submission-status/{writtenSubmissionId}
```

### Example Code

**Flutter/Dart:**
```dart
Future<Map<String, dynamic>> checkSubmissionStatus(String submissionId) async {
  final url = 'http://your-api-url.com/api/exam/submission-status/$submissionId';
  final response = await http.get(Uri.parse(url));
  
  if (response.statusCode == 200) {
    return jsonDecode(response.body);
  } else {
    throw Exception('Failed to fetch status');
  }
}

// Poll status every 3 seconds
Future<void> pollEvaluationStatus(String submissionId) async {
  bool completed = false;
  int maxAttempts = 60;
  int attempt = 0;
  
  while (!completed && attempt < maxAttempts) {
    await Future.delayed(Duration(seconds: 3));
    
    try {
      final status = await checkSubmissionStatus(submissionId);
      print('Status: ${status['status']}');
      print('Message: ${status['statusMessage']}');
      
      if (status['isComplete'] == true) {
        print('✅ Evaluation completed!');
        completed = true;
      }
    } catch (e) {
      print('Error checking status: $e');
    }
    
    attempt++;
  }
}
```

**React Native/JavaScript:**
```javascript
const checkSubmissionStatus = async (submissionId) => {
  const response = await fetch(
    `http://your-api-url.com/api/exam/submission-status/${submissionId}`
  );
  return await response.json();
};

// Poll status
const pollEvaluationStatus = async (submissionId) => {
  let completed = false;
  let attempt = 0;
  
  while (!completed && attempt < 60) {
    await new Promise(resolve => setTimeout(resolve, 3000));
    
    try {
      const status = await checkSubmissionStatus(submissionId);
      console.log('Status:', status.status);
      console.log('Message:', status.statusMessage);
      
      // When status = 2, evaluationResultBlobPath will be populated
      if (status.status === 2 && status.evaluationResultBlobPath) {
        console.log('✅ Blob path available:', status.evaluationResultBlobPath);
      }
      
      if (status.isComplete) {
        console.log('✅ Evaluation completed!');
        completed = true;
      }
    } catch (error) {
      console.error('Error checking status:', error);
    }
    
    attempt++;
  }
};
```

### Response (While Processing - Status 0 or 1)
```json
{
  "writtenSubmissionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": 1,
  "statusMessage": "📄 OCR Complete. AI evaluation starting...",
  "submittedAt": "2025-12-15T10:30:00Z",
  "evaluatedAt": null,
  "isComplete": false,
  "examId": "Karnataka_2nd_PUC_Math_2024_25",
  "studentId": "STUDENT-12345",
  "evaluationResultBlobPath": null
}
```

### Response (Completed)
```json
{
  "writtenSubmissionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": 2,
  "statusMessage": "✅ Evaluation completed! Your results are ready.",
  "submittedAt": "2025-12-15T10:30:00Z",
  "evaluatedAt": "2025-12-15T10:35:00Z",
  "isComplete": true,
  "examId": "Karnataka_2nd_PUC_Math_2024_25",
  "studentId": "STUDENT-12345",
  "evaluationResultBlobPath": "evaluation-results/Karnataka_2nd_PUC_Math_2024_25/a1b2c3d4.../evaluation-result.json",
  "result": {
    "examId": "Karnataka_2nd_PUC_Math_2024_25",
    "studentId": "STUDENT-12345",
    "examTitle": "Mathematics - Determinants and Matrices",
    "grandScore": 43.5,
    "grandTotalMarks": 60,
    "percentage": 72.5,
    "grade": "B+",
    "passed": true
  }
}
```

### Status Values (WrittenSubmissions.Status Column)
| Status Code | Status Name | Description | Database Timestamp |
|-------------|-------------|-------------|-------------------|
| `0` | Uploaded | Answer sheet uploaded, waiting for OCR to start | `SubmittedAt` |
| `1` | OCR Complete | Text extraction completed, waiting for evaluation | `OcrCompletedAt` |
| `2` | Evaluation Complete | Evaluation finished successfully, results ready | `EvaluatedAt`, `EvaluationResultBlobPath` |
| `3` | OCR Failed | Error during text extraction from images | `ErrorMessage` set |
| `4` | Evaluation Failed | Error during AI evaluation process | `ErrorMessage` set |

**Important:** When `Status = 2`, the `EvaluationResultBlobPath` column in `WrittenSubmissions` table is populated with the Azure Blob Storage path to the evaluation result JSON file.

### Status Messages (for UI Display)
| Status Code | Status Name | Message |
|-------------|-------------|---------|
| `0` | Uploaded | ⏳ Uploaded. Waiting for OCR to start... |
| `1` | OCR Complete | 📄 OCR Complete. AI evaluation starting... |
| `2` | Evaluation Complete | ✅ Evaluation completed! Your results are ready. |
| `3` | OCR Failed | ❌ OCR Failed. Please try uploading clearer images. |
| `4` | Evaluation Failed | ❌ Evaluation failed. Please contact support. |

---

## 3️⃣ Get Evaluation Results

### Endpoint
```
GET /api/exam/result/{examId}/{studentId}
```

### Description
This endpoint retrieves the **complete consolidated exam result** including:
- MCQ scores (if MCQ answers were submitted)
- Subjective scores with step-wise evaluation
- Grand total with percentage and grade
- Detailed feedback for each question

**Note:** This endpoint fetches results from the database. Alternatively, when `status = 2`, you can use the `evaluationResultBlobPath` from the status response to download the evaluation result JSON directly from Azure Blob Storage.

### URL Parameters
| Parameter | Description | Example |
|-----------|-------------|---------|
| examId | Exam identifier | `Karnataka_2nd_PUC_Math_2024_25` |
| studentId | Student identifier | `STUDENT-12345` |

### iled | ❌ Evaluation failed. Please contact support.
```

### Response
```json
{
  "writtenSubmissionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": "Evaluating",
  "statusMessage": "🤖 AI is evaluating your answers...",
  "submittedAt": "2025-12-15T10:30:00Z",
  "evaluatedAt": null,
  "isComplete": false,
  "examId": "Karnataka_2nd_PUC_Math_2024_25",
  "studentId": "STUDENT-12345"
}
```

### Status Values
| Status | Description | DB Field Updated |
|--------|-------------|------------------|
| `PendingEvaluation` | Answer sheet uploaded, awaiting processing | `SubmittedAt` |
| `OcrProcessing` | Text extraction in progress | `OcrStartedAt` |
| `Evaluating` | AI scoring in progress | `EvaluationStartedAt` |
| `Completed` | Evaluation finished successfully | `EvaluatedAt`, `TotalScore`, `Grade` |
| `Failed` | Error occurred during processing | `ErrorMessage` |

---

## 3️⃣ Get Evaluation Results

### Endpoint
```
GET /api/exam/result/{examId}/{studentId}
```

### Example Code

**Flutter/Dart:**
```dart
Future<Map<String, dynamic>> getEvaluationResults(
  String examId,
  String studentId,
) async {
  final url = 'http://your-api-url.com/api/exam/result/$examId/$studentId';
  final response = await http.get(Uri.parse(url));
  
  if (response.statusCode == 200) {
    return jsonDecode(response.body);
  } else {
    throw Exception('Failed to fetch results');
  }
}

// Display results
void displayResults(Map<String, dynamic> result) {
  print('=== EVALUATION RESULTS ===');
  print('Exam: ${result['examTitle']}');
  print('Total Score: ${result['grandScore']}/${result['grandTotalMarks']}');
  print('Percentage: ${result['percentage']}%');
  print('Grade: ${result['grade']}');
  print('Status: ${result['passed'] ? 'PASSED ✓' : 'FAILED'}');
   style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Evaluation Results</Text>
        <Text style={styles.examTitle}>{results.examTitle}</Text>
      </View>
      
      <View style={styles.scoreCard}>
        <Text style={styles.scoreLabel}>Total Score</Text>
        <Text style={styles.scoreValue}>
          {results.grandScore}/{results.grandTotalMarks}
        </Text>
        <Text style={styles.percentage}>{results.percentage}%</Text>
        <Text style={[styles.grade, results.passed ? styles.passed : styles.failed]}>
          Grade: {results.grade}
        </Text>
        <Text style={[styles.status, results.passed ? styles.passed : styles.failed]}>
          {results.passed ? '✓ PASSED' : '✗ FAILED'}
        </Text>
      </View>
      
      {/* MCQ Summary */}
      {results.mcqTotalMarks > 0 && (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>MCQ Score</Text>
          <Text style={styles.sectionScore}>
            {results.mcqScore}/{results.mcqTotalMarks}
          </Text>
        </View>
      )}
      
      {/* Subjective Summary */}
      {results.subjectiveTotalMarks > 0 && (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Subjective Score</Text>
          <Text style={styles.sectionScore}>
            {results.subjectiveScore}/{results.subjectiveTotalMarks}
          </Text>
        </View>
      )}
      
      {/* Detailed Subjective Results */}
      {results.subjectiveResults?.map((q, index) => (
        <View key={q.questionId} style={styles.questionCard}>
          <Text style={styles.questionHeader}>Question {q.questionNumber}</Text>
          <Text style={styles.questionText}>{q.questionText}</Text>
          <Text style={styles.questionScore}>
            {q.earnedMarks}/{q.maxMarks} marks {q.isFullyCorrect ? '✓' : ''}
          </Text>
          
          <View style={styles.answerSection}>
            <Text style={styles.answerLabel}>Expected Answer:</Text>
            <Text style={styles.answerText}>{q.expectedAnswer}</Text>
          </View>
          
          <View style={styles.answerSection}>
            <Text style={styles.answerLabel}>Your Answer:</Text>
            <Text style={styles.answerText}>{q.studentAnswerEcho}</Text>
          </View>
          
          {/* Step-wise breakdown */}
          {q.stepAnalysis?.length > 0 && (
            <View style={styles.stepsContainer}>
              <Text style={styles.stepsTitle}>Step-wise Evaluation:</Text>
              {q.stepAnalysis.map((step, idx) => (
                <View key={idx} style={styles.stepRow}>
                  <Text style={[styles.stepIcon, step.isCorrect ? styles.correct : styles.incorrect]}>
                    {step.isCorrect ? '✓' : '✗'}
                  </Text>
                  <View style={styles.stepContent}>
                    <Text style={styles.stepHeader}>
                      Step {step.step}: {step.description}
                    </Text>
                    <Text style={styles.stepMarks}>
                      {step.marksAwarded}/{step.maxMarksForStep} marks
                    </Text>
                    <Text style={styles.stepFeedback}>{step.feedback}</Text>
                  </View>
                </View>
              ))}
            </View>
          )}
          
          <View style={styles.feedbackContainer}>
            <Text style={styles.feedbackLabel}>Overall Feedback:</Text>
            <Text style={styles.feedbackText}>{q.overallFeedback}</Text>
          </View>
        </View>
      ))}
      
      <View style={styles.footer}>
        <Text style={styles.footerText}>
          Evaluated on: {results.evaluatedAt}
        </Text>
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f5f5f5' },
  header: { padding: 20, backgroundColor: '#fff', marginBottom: 10 },
  title: { fontSize: 24, fontWeight: 'bold', marginBottom: 5 },
  examTitle: { fontSize: 16, color: '#666' },
  scoreCard: { 
    backgroundColor: '#fff', 
    padding: 20, 
    marginHorizontal: 10, 
    marginBottom: 10,
    borderRadius: 10,
    alignItems: 'center'
  },
  scoreLabel: { fontSize: 14, color: '#666', marginBottom: 5 },
  scoreValue: { fontSize: 32, fontWeight: 'bold', marginBottom: 5 },
  percentage: { fontSize: 20, color: '#666', marginBottom: 10 },
  grade: { fontSize: 24, fontWeight: 'bold', marginBottom: 5 },
  status: { fontSize: 18, fontWeight: 'bold' },
  passed: { color: '#4caf50' },
  failed: { color: '#f44336' },
  section: { 
    backgroundColor: '#fff', 
    padding: 15, 
    marginHorizontal: 10,
    marginBottom: 10,
    borderRadius: 10,
    flexDirection: 'row',
    justifyContent: 'space-between'
  },
  sectionTitle: { fontSize: 16, fontWeight: 'bold' },
  sectionScore: { fontSize: 16, color: '#2196f3' },
  questionCard: { 
    backgroundColor: '#fff', 
    padding: 15, 
    marginHorizontal: 10,
    marginBottom: 10,
    borderRadius: 10
  },
  questionHeader: { fontSize: 18, fontWeight: 'bold', marginBottom: 5, color: '#2196f3' },
  questionText: { fontSize: 14, marginBottom: 10, color: '#333' },
  questionScore: { fontSize: 14, fontWeight: 'bold', marginBottom: 10, color: '#4caf50' },
  answerSection: { marginBottom: 15 },
  answerLabel: { fontSize: 14, fontWeight: 'bold', marginBottom: 5, color: '#666' },
  answerText: { fontSize: 14, padding: 10, backgroundColor: '#f5f5f5', borderRadius: 5 },
  stepsContainer: { marginBottom: 15 },
  stepsTitle: { fontSize: 14, fontWeight: 'bold', marginBottom: 10 },
  stepRow: { flexDirection: 'row', marginBottom: 10 },
  stepIcon: { fontSize: 18, marginRight: 10, marginTop: 2 },
  correct: { color: '#4caf50' },
  incorrect: { color: '#f44336' },
  stepContent: { flex: 1 },
  stepHeader: { fontSize: 14, fontWeight: 'bold', marginBottom: 3 },
  stepMarks: { fontSize: 12, color: '#666', marginBottom: 3 },
  stepFeedback: { fontSize: 12, color: '#555' },
  feedbackContainer: { 
    backgroundColor: '#e3f2fd', 
    padding: 10, 
    borderRadius: 5,
    borderLeftWidth: 4,
    borderLeftColor: '#2196f3'
  },
  feedbackLabel: { fontSize: 14, fontWeight: 'bold', marginBottom: 5, color: '#1976d2' },
  feedbackText: { fontSize: 14, color: '#333' },
  footer: { padding: 20, alignItems: 'center' },
  footerText: { fontSize: 12, color: '#999' }
}) return await response.json();
};

// Display results in UI
const ResultsScreen = ({ examId, studentId }) => {
  const [results, setResults] = useState(null);
  
  useEffect(() => {
    getEvaluatioMathematics - Determinants and Matrices",
  
  "mcqScore": 15,
  "mcqTotalMarks": 20,
  "mcqResults": [
    {
      "questionId": "q-uuid-1",
      "selectedOption": "B",
      "correctAnswer": "B",
      "isCorrect": true,
      "marksAwarded": 1
    }
  ],
  
  "subjectiveScore": 28.5,
  "subjectiveTotalMarks": 40,
  "subjectiveResults": [
    {
      "questionId": "q-uuid-3",
      "questionNumber": 1,
      "questionText": "Find the determinant of the matrix A = [[2, 3], [4, 5]]",
      "earnedMarks": 4.5,
      "maxMarks": 5.0,
      "isFullyCorrect": false,
      "expectedAnswer": "Step 1: Identify matrix elements a=2, b=3, c=4, d=5\nStep 2: Apply formula det(A) = ad - bc\nStep 3: det(A) = (2)(5) - (3)(4) = 10 - 12 = -2\nAnswer: -2",
      "studentAnswerEcho": "det(A) = 2*5 - 3*4 = 10 - 12 = -2",
      "stepAnalysis": [
        {
          "step": 1,
          "description": "Identify matrix elements",
          "isCorrect": false,
          "marksAwarded": 0.0,
          "maxMarksForStep": 1.0,
          "feedback": "Matrix elements not explicitly identified. Show a=2, b=3, c=4, d=5."
        },
        {
          "step": 2,
          "description": "Apply determinant formula",
          "isCorrect": true,
          "marksAwarded": 2.0,
          "maxMarksForStep": 2.0,
          "feedback": "Correct formula used: det(A) = ad - bc"
        },
        {
          "step": 3,
          "description": "Calculate final answer",
          "isCorrect": true,
          "marksAwarded": 2.5,
          "maxMarksForStep": 2.0,
          "feedback": "Perfect calculation with correct answer: -2"
        }
      ],
      "overallFeedback": "Good work! Your calculation is correct. Consider showing matrix element identification explicitly for full marks."
    },
    {
      "questionId": "q-uuid-4",
      "questionNumber": 2,
      "questionText": "Solve the system of equations using matrices...",
      "earnedMarks": 6.0,
      "maxMarks": 10.0,
      "isFullyCorrect": false,
      "expectedAnswer": "Complete solution with all steps...",
      "studentAnswerEcho": "Partial solution provided...",
      "stepAnalysis": [
        {
          "step": 1,
          "description": "Write equations in matrix form",
          "isCorrect": true,
          "marksAwarded": 2.0,
          "maxMarksForStep": 2.0,
          "feedback": "Correctly formed coefficient matrix"
        },
        {
          "step": 2,
          "description": "Calculate determinant",
          "isCorrect": true,
          "marksAwarded": 2.0,
          "maxMarksForStep": 2.0,
          "feedback": "Determinant calculated correctly"
        },
        {
          "step": 3,
          "description": "Find inverse matrix",
          "isCorrect": false,
          "marksAwarded": 2.0,
          "maxMarksForStep": 3.0,
          "feedback": "Inverse matrix has errors. Check cofactor calculations."
        },
        {
          "step": 4,
          "description": "Solve for variables",
          "isCorrect": false,
          "marksAwarded": 0.0,
          "maxMarksForStep": 3.0,
          "feedback": "Final solution not provided due to incorrect inverse"
        }
      ],
      "overallFeedback": "Good attempt! Matrix formation and determinant are correct. Review inverse matrix calculation method."
    }
  ],
  
  "grandScore": 43.5,
  "grandTotalMarks": 60.0,
  "percentage": 72.5,
  "grade": "B+",
  "passed": true,
  "evaluatedAt": "2025-12-15 10:35:00"
}
```

### Error Responses
```json
// 404 - No submission found
{
  "error": "No submission found for this exam and student"
}

// 404 - Exam not found
{
  "error": "Exam Karnataka_2nd_PUC_Math_2024_25 not found. Please generate the exam first using /api/exam/generate"
}
```

### Grade Scale
| Percentage | Grade |
|------------|-------|
| ≥ 90% | A+ |
| ≥ 80% | A |
| ≥ 70% | B+ |
| ≥ 60% | B |
| ≥ 50% | C |
| ≥ 40% | D |
| < 40% | F |

### Pass Criteria
- **Karnataka 2nd PUC:** 35% minimum
- **Status:** `passed: true` if percentage ≥ 35%

---

## 4️⃣ Download Evaluation Result from Blob Storage (Optional)

### Overview
When evaluation completes (`status = 2`), the `evaluationResultBlobPath` field contains the Azure Blob Storage path to the evaluation result JSON. You can:

**Option A:** Use the existing `/api/exam/result/{examId}/{studentId}` endpoint (recommended)

**Option B:** Download the blob directly using the blob path (requires Azure Storage SDK or backend proxy endpoint)

### Using Blob Path with Backend Proxy

If your backend provides a blob download endpoint:

```
GET /api/exam/evaluation-result/{submissionId}
```

**Example Response:**
```json
{
  "blobUrl": "https://yourstorageaccount.blob.core.windows.net/evaluation-results/Karnataka_2nd_PUC_Math_2024_25/a1b2c3d4.../evaluation-result.json?sas-token",
  "evaluationResult": {
    "examId": "Karnataka_2nd_PUC_Math_2024_25",
    "studentId": "STUDENT-12345",
    "grandScore": 43.5,
    "subjectiveResults": [...]
  }
}
```

### Flutter Example: Using Blob Path

```dart
// When status response includes evaluationResultBlobPath
Future<Map<String, dynamic>> downloadEvaluationFromBlob(String submissionId) async {
  final url = 'http://your-api-url.com/api/exam/evaluation-result/$submissionId';
  final response = await http.get(Uri.parse(url));
  
  if (response.statusCode == 200) {
    final data = jsonDecode(response.body);
    return data['evaluationResult'];
  } else {
    throw Exception('Failed to download evaluation result');
  }
}

// Use in polling when status = 2
if (status['status'] == 2 && status['evaluationResultBlobPath'] != null) {
  print('Blob path available: ${status["evaluationResultBlobPath"]}');
  
  // Option 1: Fetch from database endpoint (recommended)
  await fetchResults();
  
  // Option 2: Download from blob (if backend provides proxy endpoint)
  // final blobResult = await downloadEvaluationFromBlob(submissionId!);
}
```

### Benefits of Blob Storage Approach
- **Reduced database load:** Large evaluation JSONs stored in blob storage
- **Faster retrieval:** Direct blob download can be faster for large results
- **Scalability:** Blob storage handles high concurrent downloads better
- **Caching:** CDN can cache blob URLs for faster subsequent access

### When to Use Each Approach

| Scenario | Recommended Approach |
|----------|---------------------|
| Normal usage | Use `/api/exam/result/{examId}/{studentId}` endpoint |
| Large result files (>500KB) | Download blob directly using `evaluationResultBlobPath` |
| Offline support needed | Use database endpoint (easier to implement retry logic) |
| High traffic/scale | Use blob download with CDN |

---

## 📊 Database Schema Reference

### WrittenSubmissions Table Columns

| Column | Type | Description | Populated When |
|--------|------|-------------|----------------|
| `WrittenSubmissionId` | GUID | Primary key | On upload |
| `ExamId` | String | Exam identifier | On upload |
| `StudentId` | String | Student identifier | On upload |
| `Status` | Int | 0-4 status code | Updated during processing |
| `SubmittedAt` | DateTime | Upload timestamp | On upload |
| `OcrCompletedAt` | DateTime | OCR completion time | When status → 1 |
| `EvaluatedAt` | DateTime | Evaluation completion time | When status → 2 |
| `EvaluationResultBlobPath` | String | Blob storage path | **When status → 2** |
| `TotalScore` | Decimal | Final score | When status → 2 |
| `Grade` | String | Letter grade | When status → 2 |
| `ErrorMessage` | String | Error details | When status → 3 or 4 |

---

subjectiveTotalMarks": 40,
  "subjectiveResults": [
    {
      "questionId": "Q3",
      "questionNumber": 1,
      "questionText": "Find the determinant of the matrix...",
      "earnedMarks": 4.5,
      "maxMarks": 5,
      "isFullyCorrect": false,
      "expectedAnswer": "Step 1: Identify matrix elements a=2, b=3, c=4, d=5\nStep 2: Apply formula det(A) = ad - bc\nStep 3: det(A) = (2)(5) - (3)(4) = 10 - 12 = -2\nAnswer: -2",
      "studentAnswerEcho": "det(A) = 2*5 - 3*4 = 10 - 12 = -2",
      "stepAnalysis": [
        {
          "step": 1,
          "description": "Identify matrix elements",
          "isCorrect": false,
          "marksAwarded": 0,
          "maxMarksForStep": 1,
          "feedback": "Matrix elements not explicitly identified"
        },
        {
          "step": 2,
          "description": "Apply determinant formula",
          "isCorrect": true,
          "marksAwarded": 2,
          "maxMarksForStep": 2,
          "feedback": "Formula correctly applied"
        },
        {
          "step": 3,
          "description": "Calculate final answer",
          "isCorrect": true,
          "marksAwarded": 2.5,
          "maxMarksForStep": 2,
          "feedback": "Correct calculation and answer"
        }
      ],
      "overallFeedback": "Good work! Consider showing all steps explicitly for full marks."
    }
  ],
  
  "grandScore": 43.5,
  "grandTotalMarks": 60,
  "percentage": 72.5,
  "grade": "B+",
  "passed": true,
  "evaluatedAt": "2025-12-15T10:35:00Z"
}
```

---

## 📱 Complete UI Flow Example (Flutter)

```dart
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';
import 'dart:io';
import 'package:image_picker/image_picker.dart';

class AnswerSheetUploadScreen extends StatefulWidget {
  final String examId;
  final String studentId;
  
  const AnswerSheetUploadScreen({
    required this.examId,
    required this.studentId,
  });
  
  @override
  _AnswerSheetUploadScreenState createState() => _AnswerSheetUploadScreenState();
}

class _AnswerSheetUploadScreenState extends State<AnswerSheetUploadScreen> {
  List<File> selectedFiles = [];
  String? submissionId;
  int currentStatus = 0; // 0-4 numeric status from WrittenSubmissions table
  bool isUploading = false;
  bool isEvaluating = false;
  Map<String, dynamic>? results;
  
  final ImagePicker _picker = ImagePicker();
  
  // Pick images
  Future<void> pickImages() async {
    final List<XFile>? images = await _picker.pickMultiImage();
    if (images != null) {
      setState(() {
        selectedFiles = images.map((xFile) => File(xFile.path)).toList();
      });
    }
  }
  
  // Upload answer sheet
  Future<void> uploadAnswerSheet() async {
    if (selectedFiles.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Please select answer sheet images')),
      );
      return;
    }
    
    setState(() {
      isUploading = true;
    });
    
    try {
      final uri = Uri.parse('http://your-api-url.com/api/exam/upload-written');
      final request = http.MultipartRequest('POST', uri);
      
      request.fields['examId'] = widget.examId;
      request.fields['studentId'] = widget.studentId;
      
      for (var file in selectedFiles) {
        request.files.add(await http.MultipartFile.fromPath('files', file.path));
      }
      
      final response = await request.send();
      final responseBody = await response.stream.bytesToString();
      final result = jsonDecode(responseBody);
      
      if (response.statusCode == 200) {
        setState(() {
          submissionId = result['writtenSubmissionId'];
          currentStatus = result['status'] as int; // Numeric status from backend (0-4)
          isUploading = false;
          isEvaluating = true;
        });
        
        // Start polling
        pollEvaluationStatus();
      } else {
        throw Exception(result['error']);
      }
    } catch (e) {
      setState(() {
        isUploading = false;
      });
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Upload failed: $e')),
      );
    }
  }
  
  // Poll evaluation status
  Future<void> pollEvaluationStatus() async {
    bool completed = false;
    int attempts = 0;
    
    while (!completed && attempts < 60) {
      await Future.delayed(Duration(seconds: 3));
      
      try {
        final response = await http.get(
          Uri.parse('http://your-api-url.com/api/exam/submission-status/$submissionId'),
        );
        
        if (response.statusCode == 200) {
          final status = jsonDecode(response.body);
          
          setState(() {
            currentStatus = status['status'] as int; // Get numeric status (0-4)
          });
          
          // Check if completed (status 2) or failed (status 3 or 4)
          if (status['status'] == 2 || status['isComplete'] == true) {
            completed = true;
            
            // When status = 2, evaluationResultBlobPath is available from WrittenSubmissions table
            if (status['evaluationResultBlobPath'] != null) {
              print('✅ Evaluation blob saved at: ${status["evaluationResultBlobPath"]}');
            }
            
            await fetchResults();
            setState(() {
              isEvaluating = false;
            });
          } else if (status['status'] == 3 || status['status'] == 4) {
            // OCR or Evaluation failed
            completed = true;
            setState(() {
              isEvaluating = false;
            });
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text(_getStatusMessage(status['status']))),
            );
          }
   🔄 Complete Integration Flow Summary

```
1. Student selects answer sheet images/PDF
   ↓
2. App uploads to POST /api/exam/upload-written
   ↓ Returns: writtenSubmissionId
3. App polls GET /api/exam/submission-status/{id} every 3 seconds
   ↓ Status updates: PendingEvaluation → OcrProcessing → Evaluating → Completed
4. When isComplete = true, app calls GET /api/exam/result/{examId}/{studentId}
   ↓ Returns: Complete results with step-wise marks
5. Display results with scores, grades, and detailed feedback
```

---

## ⚠️ Important Notes

### File Constraints
- **Maximum files:** 20 per upload
- **File size limit:** 10MB per file
- **Total upload size:** 200MB maximum
- **Supported formats:** `.jpg`, `.jpeg`, `.png`, `.pdf`, `.webp`

### Processing Times
- **Upload:** < 5 seconds
- **OCR (per page):** 2-5 seconds
- **AI Evaluation:** 5-15 seconds per question
- **Total:** Typically 15-45 seconds for 2-3 page submissions

### Status Polling
- **Poll interval:** 3-5 seconds recommended
- **Maximum attempts:** 60 (3 minutes timeout)
- **Network efficiency:** Use exponential backoff for production

### Error Handling
- **Duplicate submission:** Each student can submit only once per exam
- **Exam not found:** Ensure exam is generated before submission
- **Invalid files:** Check file type and size before upload
- **OCR failure:** Status will be 3 with error message - ask student to upload clearer images
- **Evaluation failure:** Status will be 4 with error message - contact support

### Security
- Implement JWT authentication for production
- Validate student ownership before showing results
- Use HTTPS for all API calls
- Store exam/student IDs securely on device

### Offline Support
- Cache exam details locally
- Queue uploads when offline
- Retry failed uploads automatically
- Store submission ID for later status check

---

## 🚀 Production Deployment Checklist

- [ ] Replace `http://localhost:8080` with production API URL
- [ ] Add authentication headers (JWT tokens)
- [ ] Implement proper error handling with retry logic
- [ ] Add loading states and progress indicators
- [ ] Implement offline queue for uploads
- [ ] Add image compression before upload (reduce file size)
- [ ] Implement image quality validation
- [ ] Add analytics tracking for upload/evaluation flows
- [ ] Test with slow networks (3G, 4G)
- [ ] Add push notifications for evaluation completion
- [ ] Implement result caching to avoid repeated API calls
- [ ] Add print/share functionality for results

---

## 📞 Support & Troubleshooting

### Common Issues

**Issue:** "Exam not found" error  
**Solution:** Ensure exam is generated via `/api/exam/generate` before student submits answers

**Issue:** "Duplicate submission" error  
**Solution:** Each student can submit only once per exam. Check if submission already exists.

**Issue:** Upload takes too long  
**Solution:** 
- Compress images before upload
- Check network connection
- Reduce image resolution (1920px width maximum recommended)

**Issue:** Status stuck on "OcrProcessing"  
**Solution:** 
- Check Azure Function logs
- Verify Computer Vision API is running
- Wait up to 2 minutes before reporting issue

**Issue:** Evaluation shows low marks unexpectedly  
**Solution:** 
- Ensure answer sheet images are clear and readable
- Check if all pages were uploaded
- Verify student wrote answers legibly
- Review step-wise feedback for improvement areas

### Contact Support
- **Email:** support@smartstudy.com
- **Phone:** +91-XXXX-XXXXXX
- **Developer Portal:** https://developer.smartstudy.com
- **API Status:** https://status.smartstudy.com

---

## 📚 Additional Resources

- **Backend API Documentation:** [API_REFERENCE.md](./API_REFERENCE.md)
- **Azure Functions Guide:** [AZURE-FUNCTION-ANSWER-EVALUATION-README.md](./AZURE-FUNCTION-ANSWER-EVALUATION-README.md)
- **Database Schema:** [DATABASE_SETUP_README.md](./DATABASE_SETUP_README.md)
- **Evaluation System:** [EXAM_SYSTEM_README.md](./EXAM_SYSTEM_README.md)
- **Testing Guide:** [MANUAL_TESTING_GUIDE.md](./MANUAL_TESTING_GUIDE.md)lt/${widget.examId}/${widget.studentId}'),
      );
      
      if (response.statusCode == 200) {
        setState(() {
          results = jsonDecode(response.body);
        });
      }
    } catch (e) {
      print('Error fetching results: $e');
    }
  }
  
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Upload Answer Sheet')),
      body: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // File selection
            if (submissionId == null) ...[
              ElevatedButton.icon(
                onPressed: pickImages,
                icon: Icon(Icons.photo_library),
                label: Text('Select Answer Sheets'),
              ),
              SizedBox(height: 16),
              Text('${selectedFiles.length} files selected'),
              SizedBox(height: 16),
              ElevatedButton(
                onPressed: isUploading ? null : uploadAnswerSheet,
                child: isUploading
                    ? CircularProgressIndicator(color: Colors.white)
                    : Text('Upload & Evaluate'),
              ),
            ],
            
            // Status monitoring
            if (isEvaluating) ...[
              SizedBox(height: 24),
              CircularProgressIndicator(),
              SizedBox(height: 16),
              Text(
                'Status: $currentStatus',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                textAlign: TextAlign.center,
              ),
              Text(
                _getStatusMessage(currentStatus),
                textAlign: TextAlign.center,
              ),
            ],
            
            // Results
            if (results != null) ...[
              SizedBox(height: 24),
              Card(
                child: Padding(
                  padding: EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Evaluation Results',
                        style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
                      ),
                      Divider(),
                      Text('Score: ${results!['grandScore']}/${results!['grandTotalMarks']}'),
                      Text('Percentage: ${results!['percentage']}%'),
                      Text('Grade: ${results!['grade']}'),
                      Text(
                        results!['passed'] ? 'Status: PASSED ✓' : 'Status: FAILED',
                        style: TextStyle(
                          color: results!['passed'] ? Colors.green : Colors.red,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              SizedBox(height: 16),
              ElevatedButton(
                onPressed: () {
                  // Navigate to detailed results screen
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (context) => DetailedResultsScreen(results: results!),
                    ),
                  );
                },
                child: Text('View Detailed Results'),
              ),
            ],
          ],
        ),
      ),
    );
  }
  
  String _getStatusMessage(int status) {
    switch (status) {
      case 0:
        return '⏳ Uploaded. Waiting for OCR to start...';
      case 1:
        return '📄 OCR Complete. AI evaluation starting...';
      case 2:
        return '✅ Evaluation completed!';
      case 3:
        return '❌ OCR Failed. Please try uploading clearer images.';
      case 4:
        return '❌ Evaluation failed. Please contact support.';
      default:
        return 'Unknown status';
    }
  }
}

class DetailedResultsScreen extends StatelessWidget {
  final Map<String, dynamic> results;
  
  const DetailedResultsScreen({required this.results});
  
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Detailed Results')),
      body: ListView(
        padding: EdgeInsets.all(16),
        children: [
          // Overall summary
          Card(
            child: Padding(
              padding: EdgeInsets.all(16),
              child: Column(
                children: [
                  Text('${results['examTitle']}', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                  SizedBox(height: 8),
                  Text('Total: ${results['grandScore']}/${results['grandTotalMarks']} (${results['percentage']}%)'),
                  Text('Grade: ${results['grade']}', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                ],
              ),
            ),
          ),
          
          // Subjective results
          if (results['subjectiveResults'] != null) ...[
            SizedBox(height: 16),
            Text('Subjective Questions', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            ...List.generate(
              results['subjectiveResults'].length,
              (index) => _buildQuestionCard(results['subjectiveResults'][index], index + 1),
            ),
          ],
        ],
      ),
    );
  }
  
  Widget _buildQuestionCard(Map<String, dynamic> question, int qNum) {
    return Card(
      margin: EdgeInsets.symmetric(vertical: 8),
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Question $qNum', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            Text('Marks: ${question['earnedMarks']}/${question['maxMarks']}'),
            SizedBox(height: 8),
            
            // Expected Answer
            Text('Expected Answer:', style: TextStyle(fontWeight: FontWeight.bold, color: Colors.green)),
            Text(question['expectedAnswer']),
            SizedBox(height: 8),
            
            // Student Answer
            Text('Your Answer:', style: TextStyle(fontWeight: FontWeight.bold, color: Colors.orange)),
            Text(question['studentAnswerEcho']),
            SizedBox(height: 8),
            
            // Step-wise evaluation
            if (question['stepAnalysis'] != null) ...[
              Text('Step-wise Evaluation:', style: TextStyle(fontWeight: FontWeight.bold)),
              ...List.generate(
                question['stepAnalysis'].length,
                (i) => _buildStepItem(question['stepAnalysis'][i]),
              ),
            ],
            
            SizedBox(height: 8),
            Text('Feedback: ${question['overallFeedback']}', style: TextStyle(fontStyle: FontStyle.italic)),
          ],
        ),
      ),
    );
  }
  
  Widget _buildStepItem(Map<String, dynamic> step) {
    return Padding(
      padding: EdgeInsets.only(left: 16, top: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(step['isCorrect'] ? '✓' : '✗', style: TextStyle(color: step['isCorrect'] ? Colors.green : Colors.red)),
          SizedBox(width: 8),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Step ${step['step']}: ${step['marksAwarded']}/${step['maxMarksForStep']} marks'),
                Text(step['feedback'], style: TextStyle(fontSize: 12, color: Colors.grey[600])),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
```

---

## 🔒 Security Best Practices

1. **Authentication**: Add JWT token to requests
2. **File Validation**: Check file types and sizes before upload
3. **Error Handling**: Handle network errors gracefully
4. **Timeout**: Set request timeouts (30-60 seconds for upload)
5. **Retry Logic**: Implement retry for failed uploads

---

## ⚠️ Important Notes

- Maximum 20 files per upload
- Each file maximum 10MB
- Supported formats: JPG, JPEG, PNG, PDF
- Evaluation typically takes 10-30 seconds
- Poll status every 3-5 seconds
- Results include incomplete answer feedback
- Step-wise marks help students understand scoring

---

## 📞 Support

For issues or questions, contact: support@smartstudy.com
