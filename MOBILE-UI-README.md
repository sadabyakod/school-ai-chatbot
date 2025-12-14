# Smart Study - Mobile UI Integration Guide

## 📱 Overview

This document provides comprehensive API documentation for integrating the Smart Study mobile app with the backend services. The app supports syllabus uploads, model question paper management, AI chat, and exam features.

---

## 🌐 API Base URLs

| Environment | URL |
|-------------|-----|
| **Production** | `https://app-wlanqwy7vuwmu.azurewebsites.net` |
| **Local Development** | `http://localhost:8080` |

---

## 📤 File Upload APIs

### 1. Upload Syllabus PDF

Upload textbook/syllabus PDFs for AI processing.

**Endpoint:** `POST /api/file/upload`

**Content-Type:** `multipart/form-data`

**Form Fields:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `file` | File | ✅ | PDF file to upload |
| `medium` | String | ✅ | Language medium (English/Kannada) |
| `className` | String | ✅ | Class/Grade (6-12) |
| `subject` | String | ✅ | Subject name |

**Example Request (Flutter/Dart):**
```dart
import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart';

Future<Map<String, dynamic>> uploadSyllabus({
  required File file,
  required String medium,
  required String className,
  required String subject,
}) async {
  final uri = Uri.parse('$baseUrl/api/file/upload');
  
  final request = http.MultipartRequest('POST', uri);
  
  request.files.add(await http.MultipartFile.fromPath(
    'file',
    file.path,
    contentType: MediaType('application', 'pdf'),
  ));
  
  request.fields['medium'] = medium;
  request.fields['className'] = className;
  request.fields['subject'] = subject;
  
  final response = await request.send();
  final responseBody = await response.stream.bytesToString();
  
  if (response.statusCode == 200) {
    return jsonDecode(responseBody);
  }
  throw Exception('Upload failed: ${response.statusCode}');
}
```

**Success Response:**
```json
{
  "status": "success",
  "message": "File uploaded and processing started",
  "fileId": 123,
  "fileName": "physics_chapter1.pdf",
  "blobUrl": "https://stsmartstudydev.blob.core.windows.net/textbooks/12/Physics/abc123_physics.pdf"
}
```

**Blob Storage Structure:**
```
textbooks/
  └── {Grade}/
      └── {Subject}/
          └── {unique_id}_{filename}.pdf
```

---

### 2. Upload Model Question Paper

Upload model/previous year question papers.

**Endpoint:** `POST /api/questionpapers/upload`

**Content-Type:** `multipart/form-data`

**Form Fields:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `file` | File | ✅ | PDF file to upload |
| `subject` | String | ✅ | Subject name |
| `grade` | String | ✅ | Class/Grade (6-12) |
| `medium` | String | ❌ | Language medium |
| `state` | String | ✅ | State/Board (Karnataka, CBSE, etc.) |
| `academicYear` | String | ❌ | Academic year (e.g., 2024-25) |
| `paperType` | String | ❌ | Paper type (Model, Previous Year) |

**Example Request (Flutter/Dart):**
```dart
Future<Map<String, dynamic>> uploadQuestionPaper({
  required File file,
  required String subject,
  required String grade,
  required String state,
  String? medium,
  String? academicYear,
}) async {
  final uri = Uri.parse('$baseUrl/api/questionpapers/upload');
  
  final request = http.MultipartRequest('POST', uri);
  
  request.files.add(await http.MultipartFile.fromPath(
    'file',
    file.path,
    contentType: MediaType('application', 'pdf'),
  ));
  
  request.fields['subject'] = subject;
  request.fields['grade'] = grade;
  request.fields['state'] = state;
  if (medium != null) request.fields['medium'] = medium;
  if (academicYear != null) request.fields['academicYear'] = academicYear;
  
  final response = await request.send();
  final responseBody = await response.stream.bytesToString();
  
  if (response.statusCode == 200) {
    return jsonDecode(responseBody);
  }
  throw Exception('Upload failed: ${response.statusCode}');
}
```

**Success Response:**
```json
{
  "status": "success",
  "message": "Question paper uploaded successfully",
  "id": 1,
  "fileName": "physics_model_2024.pdf",
  "blobUrl": "https://stsmartstudydev.blob.core.windows.net/model-questions/Karnataka/12/Physics/abc123_physics.pdf",
  "subject": "Physics",
  "grade": "12",
  "state": "Karnataka",
  "academicYear": "2024-25"
}
```

**Blob Storage Structure:**
```
model-questions/
  └── {State}/
      └── {Grade}/
          └── {Subject}/
              └── {unique_id}_{filename}.pdf
```

---

## 📚 Get Syllabus/Textbooks APIs

### 3. List Textbook PDFs in Storage

**Endpoint:** `GET /api/test/blobs/textbooks`

**Example Request (Flutter/Dart):**
```dart
Future<Map<String, dynamic>> getTextbooks() async {
  final response = await http.get(
    Uri.parse('$baseUrl/api/test/blobs/textbooks'),
  );
  
  if (response.statusCode == 200) {
    return jsonDecode(response.body);
  }
  throw Exception('Failed to fetch textbooks');
}
```

**Response:**
```json
{
  "success": true,
  "message": "Found 17 Grade 12 textbook PDFs",
  "totalTextbooks": 17,
  "subjects": [
    {
      "subject": "Physics",
      "fileCount": 2,
      "files": [
        {"fileName": "2nd_pu_physics_part1.pdf", "size": 7166882},
        {"fileName": "2nd_pu_physics_part2.pdf", "size": 5423320}
      ]
    },
    {
      "subject": "Chemistry",
      "fileCount": 2,
      "files": [...]
    }
  ],
  "timestamp": "2025-12-14T09:00:00Z"
}
```

---

## 📝 Model Question Papers APIs

### 4. List All Question Papers

**Endpoint:** `GET /api/questionpapers`

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `subject` | String | Filter by subject |
| `grade` | String | Filter by grade |
| `state` | String | Filter by state |
| `year` | String | Filter by academic year |
| `page` | Int | Page number (default: 1) |
| `pageSize` | Int | Items per page (default: 10) |

**Example Request (Flutter/Dart):**
```dart
Future<Map<String, dynamic>> getQuestionPapers({
  String? subject,
  String? grade,
  String? state,
  int page = 1,
  int pageSize = 10,
}) async {
  final queryParams = {
    if (subject != null) 'subject': subject,
    if (grade != null) 'grade': grade,
    if (state != null) 'state': state,
    'page': page.toString(),
    'pageSize': pageSize.toString(),
  };
  
  final uri = Uri.parse('$baseUrl/api/questionpapers')
      .replace(queryParameters: queryParams);
  
  final response = await http.get(uri);
  
  if (response.statusCode == 200) {
    return jsonDecode(response.body);
  }
  throw Exception('Failed to fetch question papers');
}
```

**Response:**
```json
{
  "status": "success",
  "count": 2,
  "totalPapers": 2,
  "questionPapers": [
    {
      "subject": "Physics",
      "grade": "12",
      "medium": "English",
      "state": "Karnataka",
      "paperCount": 1,
      "latestUpload": "2025-12-14T08:32:20Z",
      "papers": [
        {
          "id": 1,
          "fileName": "Physics_Model_2024.pdf",
          "blobUrl": "https://...",
          "academicYear": "2024-25",
          "paperType": "Model",
          "fileSize": 64020,
          "uploadedAt": "2025-12-14T08:32:20Z"
        }
      ]
    }
  ]
}
```

---

### 5. Get Available Subjects

**Endpoint:** `GET /api/questionpapers/subjects`

**Response:**
```json
{
  "subjects": ["Physics", "Chemistry", "Mathematics", "Biology"]
}
```

---

### 6. Get Available Grades

**Endpoint:** `GET /api/questionpapers/grades`

**Response:**
```json
{
  "grades": ["10", "11", "12"]
}
```

---

### 7. Get Available Academic Years

**Endpoint:** `GET /api/questionpapers/years`

**Response:**
```json
{
  "years": ["2024-25", "2023-24", "2022-23"]
}
```

---

### 8. Get Question Papers by Subject

**Endpoint:** `GET /api/questionpapers/subject/{subject}`

**Example:** `GET /api/questionpapers/subject/Physics`

---

### 9. Get Question Papers by Grade

**Endpoint:** `GET /api/questionpapers/grade/{grade}`

**Example:** `GET /api/questionpapers/grade/12`

---

### 10. Download Question Paper

**Endpoint:** `GET /api/questionpapers/download/{id}`

**Response:** Binary PDF file stream

**Example Request (Flutter/Dart):**
```dart
Future<void> downloadQuestionPaper(int id, String savePath) async {
  final response = await http.get(
    Uri.parse('$baseUrl/api/questionpapers/download/$id'),
  );
  
  if (response.statusCode == 200) {
    final file = File(savePath);
    await file.writeAsBytes(response.bodyBytes);
  } else {
    throw Exception('Download failed');
  }
}
```

---

## � Evaluation Sheets APIs

Evaluation sheets (marking schemes/answer keys) follow the same pattern as Model Question Papers but are stored in the `evaluation-sheets` blob container.

### 11. Upload Evaluation Sheet

**Endpoint:** `POST /api/evaluationsheets/upload`  
**Content-Type:** `multipart/form-data`

**Form Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `file` | File | Yes | PDF file (evaluation sheet) |
| `subject` | String | Yes | Subject name (e.g., "Physics") |
| `grade` | String | Yes | Grade level (e.g., "12") |
| `state` | String | Yes | State/Board (e.g., "Karnataka") |
| `medium` | String | No | Language medium (default: "English") |
| `academicYear` | String | No | Academic year (e.g., "2024-25") |
| `sheetType` | String | No | Type of sheet (e.g., "Marking Scheme") |

**Example Request (Flutter/Dart):**
```dart
Future<Map<String, dynamic>> uploadEvaluationSheet({
  required File file,
  required String subject,
  required String grade,
  required String state,
  String? medium,
  String? academicYear,
  String? sheetType,
}) async {
  final uri = Uri.parse('$baseUrl/api/evaluationsheets/upload');
  
  final request = http.MultipartRequest('POST', uri);
  
  request.files.add(await http.MultipartFile.fromPath(
    'file',
    file.path,
    contentType: MediaType('application', 'pdf'),
  ));
  
  request.fields['subject'] = subject;
  request.fields['grade'] = grade;
  request.fields['state'] = state;
  if (medium != null) request.fields['medium'] = medium;
  if (academicYear != null) request.fields['academicYear'] = academicYear;
  if (sheetType != null) request.fields['sheetType'] = sheetType;
  
  final response = await request.send();
  final responseBody = await response.stream.bytesToString();
  
  if (response.statusCode == 200) {
    return jsonDecode(responseBody);
  }
  throw Exception('Upload failed: ${response.statusCode}');
}
```

**Success Response:**
```json
{
  "status": "success",
  "message": "Evaluation sheet uploaded successfully",
  "id": 1,
  "fileName": "physics_marking_scheme_2024.pdf",
  "blobUrl": "https://stsmartstudydev.blob.core.windows.net/evaluation-sheets/Karnataka/12/Physics/abc123_physics_marking.pdf",
  "subject": "Physics",
  "grade": "12",
  "state": "Karnataka",
  "academicYear": "2024-25",
  "sheetType": "Marking Scheme"
}
```

**Blob Storage Structure:**
```
evaluation-sheets/
  └── {State}/
      └── {Grade}/
          └── {Subject}/
              └── {unique_id}_{filename}.pdf
```

---

### 12. List All Evaluation Sheets

**Endpoint:** `GET /api/evaluationsheets`

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `subject` | String | Filter by subject |
| `grade` | String | Filter by grade |
| `state` | String | Filter by state |
| `year` | String | Filter by academic year |

**Example Request (Flutter/Dart):**
```dart
Future<Map<String, dynamic>> getEvaluationSheets({
  String? subject,
  String? grade,
  String? state,
  String? year,
}) async {
  final queryParams = {
    if (subject != null) 'subject': subject,
    if (grade != null) 'grade': grade,
    if (state != null) 'state': state,
    if (year != null) 'year': year,
  };
  
  final uri = Uri.parse('$baseUrl/api/evaluationsheets')
      .replace(queryParameters: queryParams);
  
  final response = await http.get(uri);
  
  if (response.statusCode == 200) {
    return jsonDecode(response.body);
  }
  throw Exception('Failed to fetch evaluation sheets');
}
```

**Response:**
```json
{
  "status": "success",
  "count": 2,
  "evaluationSheets": [
    {
      "id": 1,
      "fileName": "Physics_Marking_Scheme_2024.pdf",
      "blobUrl": "https://...",
      "subject": "Physics",
      "grade": "12",
      "state": "Karnataka",
      "medium": "English",
      "academicYear": "2024-25",
      "sheetType": "Marking Scheme",
      "fileSize": 48500,
      "uploadedAt": "2025-12-14T10:00:00Z"
    }
  ]
}
```

---

### 13. Get Available Subjects (Evaluation Sheets)

**Endpoint:** `GET /api/evaluationsheets/subjects`

**Response:**
```json
{
  "subjects": ["Physics", "Chemistry", "Mathematics", "Biology"]
}
```

---

### 14. Get Available Grades (Evaluation Sheets)

**Endpoint:** `GET /api/evaluationsheets/grades`

**Response:**
```json
{
  "grades": ["10", "11", "12"]
}
```

---

### 15. Get Available Academic Years (Evaluation Sheets)

**Endpoint:** `GET /api/evaluationsheets/years`

**Response:**
```json
{
  "years": ["2024-25", "2023-24", "2022-23"]
}
```

---

### 16. Get Evaluation Sheets by Subject

**Endpoint:** `GET /api/evaluationsheets/subject/{subject}`

**Example:** `GET /api/evaluationsheets/subject/Physics`

---

### 17. Get Evaluation Sheets by Grade

**Endpoint:** `GET /api/evaluationsheets/grade/{grade}`

**Example:** `GET /api/evaluationsheets/grade/12`

---

### 18. Download Evaluation Sheet

**Endpoint:** `GET /api/evaluationsheets/download/{id}`

**Response:** Binary PDF file stream

**Example Request (Flutter/Dart):**
```dart
Future<void> downloadEvaluationSheet(int id, String savePath) async {
  final response = await http.get(
    Uri.parse('$baseUrl/api/evaluationsheets/download/$id'),
  );
  
  if (response.statusCode == 200) {
    final file = File(savePath);
    await file.writeAsBytes(response.bodyBytes);
  } else {
    throw Exception('Download failed');
  }
}
```

---

### 19. Delete Evaluation Sheet

**Endpoint:** `DELETE /api/evaluationsheets/{id}`

**Response:**
```json
{
  "status": "success",
  "message": "Evaluation sheet deleted successfully"
}
```

---

### 20. List Evaluation Sheets in Blob Storage

**Endpoint:** `GET /api/test/blobs/evaluation-sheets`

This endpoint directly lists all files in the `evaluation-sheets` blob container.

**Example Request (Flutter/Dart):**
```dart
Future<Map<String, dynamic>> getEvaluationSheetBlobs() async {
  final response = await http.get(
    Uri.parse('$baseUrl/api/test/blobs/evaluation-sheets'),
  );
  
  if (response.statusCode == 200) {
    return jsonDecode(response.body);
  }
  throw Exception('Failed to fetch evaluation sheet blobs');
}
```

---

## 💬 AI Chat APIs

### 21. Send Chat Message

**Endpoint:** `POST /api/chat`

**Request:**
```json
{
  "message": "Explain Newton's laws of motion",
  "language": "English",
  "sessionId": "optional-session-id"
}
```

**Response:**
```json
{
  "reply": "Newton's laws of motion are three fundamental principles...",
  "language": "English",
  "sessionId": "abc123-session-id",
  "sources": ["Physics Chapter 3", "Physics Chapter 4"]
}
```

**Example Request (Flutter/Dart):**
```dart
Future<ChatResponse> sendMessage({
  required String message,
  String language = 'English',
  String? sessionId,
}) async {
  final response = await http.post(
    Uri.parse('$baseUrl/api/chat'),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode({
      'message': message,
      'language': language,
      if (sessionId != null) 'sessionId': sessionId,
    }),
  );
  
  if (response.statusCode == 200) {
    return ChatResponse.fromJson(jsonDecode(response.body));
  }
  throw Exception('Chat request failed');
}
```

---

### 12. Get Chat History

**Endpoint:** `GET /api/chat/history?sessionId={sessionId}`

**Response:**
```json
{
  "sessionId": "abc123",
  "messages": [
    {"role": "user", "content": "What is gravity?", "timestamp": "..."},
    {"role": "assistant", "content": "Gravity is...", "timestamp": "..."}
  ]
}
```

---

## 🎯 Exam APIs

### 13. Start Exam

**Endpoint:** `POST /api/exams/start`

**Request:**
```json
{
  "studentId": "student-uuid",
  "examTemplateId": 1
}
```

### 14. Submit Answer

**Endpoint:** `POST /api/exams/answer`

**Request:**
```json
{
  "attemptId": 1,
  "questionId": 1,
  "answer": "B",
  "timeTakenSeconds": 45
}
```

### 15. Get Exam Results

**Endpoint:** `GET /api/exams/results/{attemptId}`

---

## 📱 UI Components Reference

### Upload Page Features

The mobile upload page should include:

1. **Upload Type Toggle**
   - Syllabus (textbooks)
   - Model Question Papers

2. **For Syllabus Upload:**
   - Medium dropdown (English, Kannada)
   - Class dropdown (6-12)
   - Subject dropdown (dynamic based on class)
   - PDF file picker

3. **For Model Question Papers:**
   - State/Board dropdown (Karnataka, CBSE, ICSE, etc.)
   - Class dropdown (6-12)
   - Subject dropdown
   - Academic Year dropdown (last 10 years)
   - PDF file picker

### Subject Options by Class

```dart
Map<String, List<String>> subjectsByClass = {
  '12': ['Physics', 'Chemistry', 'Mathematics', 'Biology', 'Kannada', 
         'English', 'Accountancy', 'Business Studies', 'Economics', 
         'Statistics', 'Computer Science', 'History', 'Political Science'],
  '11': ['Physics', 'Chemistry', 'Mathematics', 'Biology', 'Kannada', 
         'English', 'Accountancy', 'Business Studies', 'Economics', 
         'Statistics', 'Computer Science', 'History', 'Political Science'],
  '10': ['English', 'Kannada', 'Hindi', 'Mathematics', 'Science', 
         'Social Science', 'Sanskrit'],
  'default': ['Mathematics', 'Science', 'Social Science', 'English', 
              'Kannada', 'Hindi', 'Sanskrit'],
};
```

### State/Board Options

```dart
List<String> stateOptions = [
  'Karnataka',
  'Maharashtra', 
  'Tamil Nadu',
  'Kerala',
  'Andhra Pradesh',
  'Telangana',
  'CBSE',
  'ICSE',
];
```

### Academic Year Options

```dart
List<String> getAcademicYears() {
  final currentYear = DateTime.now().year;
  return List.generate(10, (i) {
    final year = currentYear - i;
    return '${year}-${(year + 1).toString().substring(2)}';
  });
}
// Result: ['2025-26', '2024-25', '2023-24', ...]
```

---

## 🔧 Error Handling

All APIs return errors in this format:

```json
{
  "status": "error",
  "message": "Description of what went wrong",
  "code": "ERROR_CODE"
}
```

**Common Error Codes:**
| Code | Description |
|------|-------------|
| `NO_FILE` | No file was uploaded |
| `INVALID_FORMAT` | File format not supported |
| `MISSING_FIELD` | Required field missing |
| `NOT_FOUND` | Resource not found |
| `SERVER_ERROR` | Internal server error |

**Example Error Handler (Flutter/Dart):**
```dart
void handleApiError(http.Response response) {
  final body = jsonDecode(response.body);
  
  switch (response.statusCode) {
    case 400:
      throw ValidationException(body['message']);
    case 404:
      throw NotFoundException(body['message']);
    case 500:
      throw ServerException(body['message']);
    default:
      throw ApiException(body['message'] ?? 'Unknown error');
  }
}
```

---

## 📊 Test Endpoints

For debugging and testing:

| Endpoint | Description |
|----------|-------------|
| `GET /api/test/db-connection` | Test database connection |
| `GET /api/test/blobs/textbooks` | List syllabus PDFs |
| `GET /api/test/blobs/model-questions` | List model question PDFs |
| `GET /api/simple/ping` | Health check |

---

## 🚀 Quick Start Checklist

1. ✅ Set base URL based on environment
2. ✅ Implement file upload with multipart/form-data
3. ✅ Add proper error handling
4. ✅ Implement loading states for uploads
5. ✅ Cache dropdown options locally
6. ✅ Implement PDF viewer for downloaded papers
7. ✅ Add offline support for cached papers

---

## 📞 Support

For API issues or questions, check the backend logs or contact the development team.
