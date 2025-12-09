# School AI Chatbot - Mobile App Quick Reference

## 📱 Backend API Information

**Base URL**: `http://192.168.1.77:8080`  
**Swagger Documentation**: `http://localhost:8080/swagger`  
**Status**: Production Ready ✅

---

## 🎯 Core Features to Implement

### 1. Exam Generation
Students can request AI-generated exams based on their curriculum.

**Endpoint**: `POST /api/exam/generate`

```javascript
const generateExam = async () => {
  const response = await fetch('http://192.168.1.77:8080/api/exam/generate', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      subject: 'Mathematics',
      chapter: 'Calculus',
      class: '12th Grade',
      questionCount: 10
    })
  });
  return await response.json();
};
```

**Response**:
```json
{
  "examId": "Karnataka_2nd_PUC_Math_2024_25",
  "title": "Karnataka 2nd PUC Mathematics Model Paper 2024-25",
  "parts": [
    {
      "title": "Part A - Multiple Choice Questions",
      "questions": [
        {
          "questionId": "q1",
          "questionText": "What is the derivative of x²?",
          "options": ["2x", "x", "x²", "2"],
          "correctAnswer": "2x",
          "marks": 1
        }
      ]
    }
  ]
}
```

---

### 2. MCQ Submission
Submit multiple choice answers for instant grading.

**Endpoint**: `POST /api/exam/submit-mcq`

```javascript
const submitMCQ = async (examId, studentId, answers) => {
  const response = await fetch('http://192.168.1.77:8080/api/exam/submit-mcq', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      examId,
      studentId,
      answers: [
        { questionId: 'q1', selectedOption: '2x' },
        { questionId: 'q2', selectedOption: 'B' }
      ]
    })
  });
  return await response.json();
};
```

**Response**:
```json
{
  "mcqSubmissionId": "uuid",
  "score": 8,
  "totalMarks": 10,
  "percentage": 80.0,
  "results": [
    {
      "questionId": "q1",
      "selectedOption": "2x",
      "correctAnswer": "2x",
      "isCorrect": true,
      "marksAwarded": 1
    }
  ]
}
```

---

### 3. Subjective Answer Upload (NEW!)
Upload scanned answer sheets for AI evaluation.

**Endpoint**: `POST /api/exam/upload-written`

```javascript
const uploadAnswers = async (examId, studentId, imageFiles) => {
  const formData = new FormData();
  formData.append('examId', examId);
  formData.append('studentId', studentId);
  
  imageFiles.forEach((file) => {
    formData.append('files', {
      uri: file.uri,
      type: 'image/jpeg',
      name: file.name
    });
  });

  const response = await fetch('http://192.168.1.77:8080/api/exam/upload-written', {
    method: 'POST',
    body: formData
  });
  return await response.json();
};
```

**Response**:
```json
{
  "writtenSubmissionId": "uuid",
  "status": "PendingEvaluation",
  "message": "Written answers uploaded successfully. Evaluation in progress."
}
```

---

### 4. Get Complete Results (NEW!)
Retrieve comprehensive results with AI feedback.

**Endpoint**: `GET /api/exam/result/{examId}/{studentId}`

```javascript
const getResults = async (examId, studentId) => {
  const response = await fetch(
    `http://192.168.1.77:8080/api/exam/result/${examId}/${studentId}`
  );
  return await response.json();
};
```

**Response Structure**:
```json
{
  "examId": "Karnataka_2nd_PUC_Math_2024_25",
  "studentId": "STUDENT-001",
  "examTitle": "Mathematics - Calculus",
  
  "mcqScore": 15,
  "mcqTotalMarks": 20,
  
  "subjectiveScore": 68,
  "subjectiveTotalMarks": 80,
  
  "subjectiveResults": [
    {
      "questionNumber": 1,
      "questionText": "Find the derivative of x²",
      "earnedMarks": 4.5,
      "maxMarks": 5,
      "isFullyCorrect": false,
      
      "studentAnswerEcho": "The derivative is 2x using power rule",
      "expectedAnswer": "d/dx(x²) = 2x (using power rule: d/dx(x^n) = nx^(n-1))",
      
      "stepAnalysis": [
        {
          "step": 1,
          "description": "Identify differentiation rule",
          "isCorrect": true,
          "marksAwarded": 1.5,
          "maxMarksForStep": 1.5,
          "feedback": "Correctly identified power rule"
        },
        {
          "step": 2,
          "description": "Apply power rule",
          "isCorrect": true,
          "marksAwarded": 2,
          "maxMarksForStep": 2,
          "feedback": "Power rule applied correctly"
        },
        {
          "step": 3,
          "description": "Show work",
          "isCorrect": false,
          "marksAwarded": 1,
          "maxMarksForStep": 1.5,
          "feedback": "Could show intermediate step x^(2-1)"
        }
      ],
      
      "overallFeedback": "Good understanding! Show more detailed steps for full marks."
    }
  ],
  
  "grandScore": 83,
  "grandTotalMarks": 100,
  "percentage": 83.0,
  "grade": "A",
  "passed": true,
  "evaluatedAt": "2025-12-08 14:30:00"
}
```

---

### 5. Student Exam History
Get all exams taken by a student.

**Endpoint**: `GET /api/exam/submissions/by-student/{studentId}?page=1&pageSize=10`

```javascript
const getHistory = async (studentId, page = 1) => {
  const response = await fetch(
    `http://192.168.1.77:8080/api/exam/submissions/by-student/${studentId}?page=${page}&pageSize=10`
  );
  return await response.json();
};
```

---

### 6. Analytics Dashboard (Teacher/Admin)
View all submissions for an exam.

**Endpoint**: `GET /api/exam/{examId}/submissions?page=1&pageSize=10`

```javascript
const getExamSubmissions = async (examId) => {
  const response = await fetch(
    `http://192.168.1.77:8080/api/exam/${examId}/submissions?page=1&pageSize=10`
  );
  return await response.json();
};
```

---

### 7. Exam Summary Statistics
Get statistical overview for teachers.

**Endpoint**: `GET /api/exam/{examId}/summary`

```javascript
const getExamSummary = async (examId) => {
  const response = await fetch(
    `http://192.168.1.77:8080/api/exam/${examId}/summary`
  );
  return await response.json();
};
```

**Response**:
```json
{
  "examId": "Karnataka_2nd_PUC_Math_2024_25",
  "examTitle": "Mathematics Model Paper",
  "totalSubmissions": 45,
  "completedSubmissions": 42,
  "pendingSubmissions": 3,
  "averageScore": 72.5,
  "highestScore": 95,
  "lowestScore": 45,
  "passRate": 88.9
}
```

---

### 8. AI Chat Assistant
Interactive chat for homework help.

**Endpoint**: `POST /api/chat`

```javascript
const sendMessage = async (message) => {
  const response = await fetch('http://192.168.1.77:8080/api/chat', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      message,
      context: {
        subject: 'Mathematics',
        topic: 'Calculus'
      }
    })
  });
  return await response.json();
};
```

---

## 📦 Required React Native Packages

```bash
npm install axios                                    # API calls
npm install @react-native-async-storage/async-storage  # Local storage
npm install @react-navigation/native                 # Navigation
npm install @react-navigation/stack                  # Stack navigation
npm install react-native-image-picker                # Camera/Gallery
npm install @reduxjs/toolkit react-redux             # State management
npm install react-native-vector-icons                # Icons
```

---

## 🎨 UI Screens to Build

### Student App
1. **Login/Register** - Authentication
2. **Home Dashboard** - Overview of exams
3. **Exam List** - Browse available exams
4. **Take Exam** - MCQ interface
5. **Upload Answers** - Camera/gallery for subjective questions
6. **Results** - Detailed feedback and scores
7. **History** - Past exam attempts
8. **Chat** - AI assistant
9. **Profile** - Student information

### Teacher App
1. **Login** - Authentication
2. **Dashboard** - Analytics overview
3. **Create Exam** - Generate AI exams
4. **View Submissions** - Student submissions list
5. **Grade Answers** - Review and adjust scores
6. **Statistics** - Performance analytics
7. **Students** - Manage student list

---

## 🔐 Security Implementation

### Token Storage
```javascript
import AsyncStorage from '@react-native-async-storage/async-storage';

// Save token
await AsyncStorage.setItem('authToken', token);

// Get token
const token = await AsyncStorage.getItem('authToken');

// Add to headers
headers: {
  'Authorization': `Bearer ${token}`,
  'Content-Type': 'application/json'
}
```

---

## 🚀 Getting Started Checklist

- [ ] Create React Native project
- [ ] Install required packages
- [ ] Configure API base URL
- [ ] Test backend connection
- [ ] Implement authentication
- [ ] Build exam list screen
- [ ] Build exam taking interface
- [ ] Implement camera/upload feature
- [ ] Build results screen with AI feedback
- [ ] Add chat assistant
- [ ] Implement analytics dashboard
- [ ] Add offline support
- [ ] Setup push notifications
- [ ] Test on real devices
- [ ] Deploy to App Store / Play Store

---

## 📞 Support

- **Backend Documentation**: `MOBILE_APP_IMPLEMENTATION.md`
- **API Reference**: `API_REFERENCE.md`
- **Analytics API**: `EXAM_ANALYTICS_API.md`
- **Swagger UI**: http://localhost:8080/swagger

---

## ✨ Key Benefits for Students

✅ **Instant Feedback** - Get results immediately after submission  
✅ **Detailed Analysis** - See exactly where you went wrong  
✅ **Learn from Mistakes** - AI provides improvement suggestions  
✅ **Fair Grading** - Consistent AI evaluation  
✅ **Transparency** - Step-by-step marks breakdown  
✅ **Progress Tracking** - Monitor improvement over time  

---

**Last Updated**: December 8, 2025  
**Status**: Production Ready ✅
