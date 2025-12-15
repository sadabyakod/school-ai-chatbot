# 📱 Mobile UI - Answer Sheet Status Tracking & Result Retrieval

## Overview

This guide explains how to implement answer sheet upload, status tracking, and result retrieval in your mobile app using the Azure SQL-backed backend API.

---

## 🔄 Complete Flow

```
1. Upload answer sheet → GET writtenSubmissionId
   ↓
2. Poll status every 3 seconds → Check WrittenSubmissions.Status (0-4)
   ↓
3. When Status = 2 (Evaluation Complete)
   → Response includes EvaluationResultBlobPath from database
   ↓
4. Fetch results using /api/exam/result/{examId}/{studentId}
   OR download blob directly using EvaluationResultBlobPath
```

---

## 📊 Status Codes (WrittenSubmissions.Status Column)

| Code | Name | Description | What to Display |
|------|------|-------------|-----------------|
| `0` | Uploaded | Answer sheet uploaded, waiting for OCR | ⏳ Uploaded. Waiting for OCR... |
| `1` | OCR Complete | Text extracted, waiting for AI evaluation | 📄 OCR Complete. Evaluation starting... |
| `2` | Evaluation Complete | ✅ **DONE** - Results ready | ✅ Evaluation completed! |
| `3` | OCR Failed | Error during text extraction | ❌ OCR Failed. Upload clearer images. |
| `4` | Evaluation Failed | Error during AI evaluation | ❌ Evaluation failed. Contact support. |

**Key Point:** When `Status = 2`, the backend populates `EvaluationResultBlobPath` in the `WrittenSubmissions` table and returns it in the status response.

---

## 🚀 API Endpoints

### 1️⃣ Upload Answer Sheet


```
POST /api/exam/upload-written
Content-Type: multipart/form-data
```

**Request:**
- `examId`: string
- `studentId`: string  
- `files`: file[] (max 20 files, 10MB each)

**Response:**
```json
{
  "writtenSubmissionId": "a1b2c3d4-...",
  "status": 0,
  "message": "✅ Uploaded successfully!"
}
```

---

### 2️⃣ Check Status (Poll This)

```
GET /api/exam/submission-status/{writtenSubmissionId}
```

**Response (Status 0 or 1 - Processing):**
```json
{
  "writtenSubmissionId": "a1b2c3d4-...",
  "status": "1",
  "statusMessage": "📄 OCR Complete. AI evaluation starting...",
  "submittedAt": "2025-12-15T10:30:00Z",
  "evaluatedAt": null,
  "isComplete": false,
  "examId": "Karnataka_2nd_PUC_Math_2024_25",
  "studentId": "STUDENT-12345",
  "evaluationResultBlobPath": null
}
```

**Response (Status 2 - Completed ✅):**
```json
{
  "writtenSubmissionId": "a1b2c3d4-...",
  "status": "2",
  "statusMessage": "✅ Evaluation completed! Your results are ready.",
  "submittedAt": "2025-12-15T10:30:00Z",
  "evaluatedAt": "2025-12-15T10:35:00Z",
  "isComplete": true,
  "examId": "Karnataka_2nd_PUC_Math_2024_25",
  "studentId": "STUDENT-12345",
  "evaluationResultBlobPath": "evaluation-results/Karnataka_2nd_PUC_Math_2024_25/a1b2c3d4.../evaluation-result.json",
  "result": {
    "grandScore": 43.5,
    "grandTotalMarks": 60,
    "percentage": 72.5,
    "grade": "B+",
    "passed": true
  }
}
```

**⚠️ Key Field:** `evaluationResultBlobPath` - This comes from `WrittenSubmissions.EvaluationResultBlobPath` column in Azure SQL database. Only populated when `status = "2"`.

---

### 3️⃣ Get Full Results

```
GET /api/exam/result/{examId}/{studentId}
```

**Response:**
```json
{
  "examId": "Karnataka_2nd_PUC_Math_2024_25",
  "studentId": "STUDENT-12345",
  "examTitle": "Mathematics - Determinants and Matrices",
  "mcqScore": 15,
  "mcqTotalMarks": 20,
  "subjectiveScore": 28.5,
  "subjectiveTotalMarks": 40,
  "grandScore": 43.5,
  "grandTotalMarks": 60,
  "percentage": 72.5,
  "grade": "B+",
  "passed": true,
  "subjectiveResults": [
    {
      "questionNumber": 1,
      "earnedMarks": 4.5,
      "maxMarks": 5.0,
      "stepAnalysis": [...],
      "overallFeedback": "Good work! Consider showing all steps..."
    }
  ]
}
```

---

## 🎯 UI Implementation Checklist

### Upload Screen
- [ ] Image picker for answer sheet photos
- [ ] File validation (max 20 files, 10MB each)
- [ ] Upload progress indicator
- [ ] Success message with submission ID

### Status Polling Screen
- [ ] Circular progress indicator
- [ ] Status code display (0-4)
- [ ] Status message display
- [ ] Current step indicator:
  - ⏳ Uploaded (0)
  - 📄 OCR Complete (1)
  - ✅ Evaluation Complete (2)
  - ❌ Failed (3 or 4)

### Results Screen
- [ ] Total score display (grandScore/grandTotalMarks)
- [ ] Percentage and grade display
- [ ] Pass/Fail status
- [ ] MCQ score breakdown
- [ ] Subjective score breakdown
- [ ] Step-wise evaluation details
- [ ] Feedback messages
- [ ] Share/Download results button

---

## ⚠️ Important Notes

### Status Polling
- **Poll interval:** 3 seconds
- **Timeout:** 60 attempts (3 minutes)
- **Stop conditions:**
  - Status = 2 (Success) ✅
  - Status = 3 or 4 (Failed) ❌
  - Timeout reached

### Database Fields
The backend reads from `WrittenSubmissions` table:
- `Status` (int): 0-4 status code
- `EvaluationResultBlobPath` (string): Blob storage path (populated when Status = 2)
- `SubmittedAt` (datetime): Upload timestamp
- `EvaluatedAt` (datetime): Completion timestamp

### Error Handling
- **Status 3 (OCR Failed):** Show message: "OCR failed. Please upload clearer images."
- **Status 4 (Evaluation Failed):** Show message: "Evaluation failed. Contact support."
- **Network errors:** Implement retry logic with exponential backoff
- **Timeout:** Show option to check status later using submission ID

---

## 🔐 Production Checklist

- [ ] Replace localhost with production API URL
- [ ] Add JWT authentication headers
- [ ] Implement proper error handling
- [ ] Add retry logic for network failures
- [ ] Implement offline queue for uploads
- [ ] Add image compression before upload
- [ ] Cache submission ID for later status checks
- [ ] Add analytics tracking
- [ ] Test with slow networks (3G)
- [ ] Add push notifications for completion
- [ ] Implement result caching

---

## 📚 Related Documentation

- [Complete Mobile Integration Guide](./MOBILE-ANSWER-SHEET-UPLOAD-GUIDE.md)
- [Backend API Reference](./API_REFERENCE.md)
- [Database Schema](./DATABASE_SETUP_README.md)
- [Azure Functions Guide](./AZURE-FUNCTION-ANSWER-EVALUATION-README.md)

---

## 📞 Support

For issues or questions:
- **Email:** support@smartstudy.com
- **API Status:** https://status.smartstudy.com
