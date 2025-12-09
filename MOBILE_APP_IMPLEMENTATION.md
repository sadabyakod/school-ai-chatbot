# Mobile Application Implementation Guide

## Overview

This guide provides a complete roadmap for building a mobile application (iOS & Android) that integrates with the School AI Chatbot backend API.

**Last Updated**: December 8, 2025  
**Backend API**: http://192.168.1.77:8080  
**Swagger Docs**: http://localhost:8080/swagger

## ✨ Latest Features (December 2025)

- ✅ **AI-Powered Subjective Evaluation** - Upload answer sheets, get instant AI feedback
- ✅ **Step-by-Step Scoring** - Detailed breakdown with marks per step
- ✅ **Analytics Dashboard** - Track student performance and exam statistics
- ✅ **Expected Answers** - Compare student responses with correct answers
- ✅ **Improvement Suggestions** - AI-generated feedback for learning

## 🚀 Quick Start

### Prerequisites
```bash
node -v    # Should be v16 or higher
npm -v     # Should be v8 or higher
```

### Create New Mobile App (React Native)
```bash
# Install React Native CLI
npx react-native init SchoolAiMobile

# Navigate to project
cd SchoolAiMobile

# Install dependencies
npm install axios @react-native-async-storage/async-storage
npm install @react-navigation/native @react-navigation/stack
npm install react-native-image-picker
npm install @reduxjs/toolkit react-redux

# iOS specific
cd ios && pod install && cd ..

# Run the app
npm run android  # For Android
npm run ios      # For iOS
```

### Configure Backend URL
Create `src/constants/config.js`:
```javascript
export const API_BASE_URL = 'http://192.168.1.77:8080'; // Your backend IP
```

### Test API Connection
```javascript
// Test if backend is accessible
fetch(`${API_BASE_URL}/api/exam/list`)
  .then(response => response.json())
  .then(data => console.log('Connected:', data))
  .catch(error => console.error('Connection failed:', error));
```

## Technology Stack Options

### Option 1: React Native (Recommended)
- **Pros**: Single codebase for iOS & Android, large community, fast development
- **Cons**: Larger app size
- **Best for**: Cross-platform development with shared web developer skills

### Option 2: Flutter
- **Pros**: Excellent performance, beautiful UI, single codebase
- **Cons**: Dart language learning curve
- **Best for**: High-performance apps with custom UI

### Option 3: Native (Swift/Kotlin)
- **Pros**: Best performance, platform-specific features
- **Cons**: Separate codebases, longer development time
- **Best for**: Maximum performance and platform integration

## Project Structure (React Native Example)

```
school-ai-mobile/
├── src/
│   ├── api/
│   │   ├── client.js              # API client configuration
│   │   ├── examApi.js             # Exam-related endpoints
│   │   ├── chatApi.js             # Chat endpoints
│   │   └── analyticsApi.js        # Analytics endpoints
│   ├── components/
│   │   ├── common/
│   │   │   ├── Button.js
│   │   │   ├── Input.js
│   │   │   └── Card.js
│   │   ├── exam/
│   │   │   ├── ExamCard.js
│   │   │   ├── QuestionItem.js
│   │   │   └── SubmissionForm.js
│   │   └── chat/
│   │       ├── ChatBubble.js
│   │       └── MessageInput.js
│   ├── screens/
│   │   ├── auth/
│   │   │   ├── LoginScreen.js
│   │   │   └── RegisterScreen.js
│   │   ├── exam/
│   │   │   ├── ExamListScreen.js
│   │   │   ├── ExamDetailScreen.js
│   │   │   ├── TakeExamScreen.js
│   │   │   └── ResultsScreen.js
│   │   ├── chat/
│   │   │   └── ChatScreen.js
│   │   └── profile/
│   │       ├── ProfileScreen.js
│   │       └── HistoryScreen.js
│   ├── navigation/
│   │   ├── AppNavigator.js
│   │   └── AuthNavigator.js
│   ├── store/
│   │   ├── store.js               # Redux/Zustand store
│   │   ├── slices/
│   │   │   ├── authSlice.js
│   │   │   ├── examSlice.js
│   │   │   └── chatSlice.js
│   ├── utils/
│   │   ├── storage.js             # AsyncStorage wrapper
│   │   └── validators.js
│   └── constants/
│       ├── colors.js
│       └── config.js
├── assets/
│   ├── images/
│   └── fonts/
├── App.js
├── package.json
└── app.json
```

## API Integration

### Base Configuration

```javascript
// src/api/client.js
import axios from 'axios';
import AsyncStorage from '@react-native-async-storage/async-storage';

const API_BASE_URL = 'https://your-backend-url.azurewebsites.net';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor to add auth token
apiClient.interceptors.request.use(
  async (config) => {
    const token = await AsyncStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor for error handling
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      // Handle unauthorized - logout user
      await AsyncStorage.removeItem('authToken');
      // Navigate to login
    }
    return Promise.reject(error);
  }
);

export default apiClient;
```

### Exam API Integration

```javascript
// src/api/examApi.js
import apiClient from './client';

export const examApi = {
  // Generate exam
  generateExam: async (params) => {
    const response = await apiClient.post('/api/exam/generate', params);
    return response.data;
  },

  // Get exam details
  getExam: async (examId) => {
    const response = await apiClient.get(`/api/exam/${examId}`);
    return response.data;
  },

  // Submit MCQ answers
  submitMCQ: async (submission) => {
    const response = await apiClient.post('/api/exam/submit-mcq', submission);
    return response.data;
  },

  // Upload written answer sheet (NEW - December 2025)
  uploadWrittenAnswers: async (examId, studentId, files) => {
    const formData = new FormData();
    formData.append('examId', examId);
    formData.append('studentId', studentId);
    
    files.forEach((file, index) => {
      formData.append('files', {
        uri: file.uri,
        type: file.type || 'image/jpeg',
        name: file.name || `answer_${index}.jpg`,
      });
    });

    const response = await apiClient.post('/api/exam/upload-written', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  // Get complete exam result with AI evaluation (NEW - December 2025)
  getExamResult: async (examId, studentId) => {
    const response = await apiClient.get(`/api/exam/result/${examId}/${studentId}`);
    return response.data;
  },

  // Get student's exam history
  getStudentHistory: async (studentId, page = 1, pageSize = 10) => {
    const response = await apiClient.get(
      `/api/exam/submissions/by-student/${studentId}`,
      { params: { page, pageSize } }
    );
    return response.data;
  },

  // Get submission details
  getSubmissionDetails: async (examId, studentId) => {
    const response = await apiClient.get(
      `/api/exam/${examId}/submissions/${studentId}`
    );
    return response.data;
  },

  // Get exam summary statistics (NEW - December 2025)
  getExamSummary: async (examId) => {
    const response = await apiClient.get(`/api/exam/${examId}/summary`);
    return response.data;
  },

  // List all submissions for an exam (NEW - December 2025)
  getExamSubmissions: async (examId, page = 1, pageSize = 10) => {
    const response = await apiClient.get(
      `/api/exam/${examId}/submissions`,
      { params: { page, pageSize } }
    );
    return response.data;
  },
};
```

### Chat API Integration

```javascript
// src/api/chatApi.js
import apiClient from './client';

export const chatApi = {
  // Send message to AI chatbot
  sendMessage: async (message, context = {}) => {
    const response = await apiClient.post('/api/chat', {
      message,
      context,
    });
    return response.data;
  },

  // Get chat history
  getChatHistory: async (studentId) => {
    const response = await apiClient.get(`/api/chat/history/${studentId}`);
    return response.data;
  },
};
```

## Key Screens Implementation

### 1. Exam List Screen

```javascript
// src/screens/exam/ExamListScreen.js
import React, { useEffect, useState } from 'react';
import { View, FlatList, StyleSheet, RefreshControl } from 'react-native';
import { examApi } from '../../api/examApi';
import ExamCard from '../../components/exam/ExamCard';

const ExamListScreen = ({ navigation }) => {
  const [exams, setExams] = useState([]);
  const [loading, setLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);

  const loadExams = async () => {
    try {
      setLoading(true);
      const studentId = await getStudentId(); // From AsyncStorage
      const data = await examApi.getStudentHistory(studentId);
      setExams(data.items);
    } catch (error) {
      console.error('Failed to load exams:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadExams();
  }, []);

  const onRefresh = async () => {
    setRefreshing(true);
    await loadExams();
    setRefreshing(false);
  };

  return (
    <View style={styles.container}>
      <FlatList
        data={exams}
        keyExtractor={(item) => item.examId}
        renderItem={({ item }) => (
          <ExamCard
            exam={item}
            onPress={() => navigation.navigate('ExamDetail', { examId: item.examId })}
          />
        )}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
});

export default ExamListScreen;
```

### 2. Take Exam Screen

```javascript
// src/screens/exam/TakeExamScreen.js
import React, { useState, useEffect } from 'react';
import { View, ScrollView, Text, StyleSheet, Button, Alert } from 'react-native';
import { examApi } from '../../api/examApi';

const TakeExamScreen = ({ route, navigation }) => {
  const { examId } = route.params;
  const [exam, setExam] = useState(null);
  const [answers, setAnswers] = useState({});
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadExam();
  }, []);

  const loadExam = async () => {
    try {
      const data = await examApi.getExam(examId);
      setExam(data);
    } catch (error) {
      Alert.alert('Error', 'Failed to load exam');
    }
  };

  const handleMCQAnswer = (questionId, option) => {
    setAnswers((prev) => ({
      ...prev,
      [questionId]: option,
    }));
  };

  const submitExam = async () => {
    try {
      setLoading(true);
      const studentId = await getStudentId();
      
      // Submit MCQ answers
      const mcqAnswers = Object.entries(answers).map(([questionId, selectedOption]) => ({
        questionId,
        selectedOption,
      }));

      const result = await examApi.submitMCQ({
        examId,
        studentId,
        answers: mcqAnswers,
      });

      Alert.alert('Success', 'Exam submitted successfully!');
      navigation.navigate('Results', { submissionId: result.mcqSubmissionId });
    } catch (error) {
      Alert.alert('Error', 'Failed to submit exam');
    } finally {
      setLoading(false);
    }
  };

  if (!exam) return <Text>Loading...</Text>;

  return (
    <ScrollView style={styles.container}>
      <Text style={styles.title}>{exam.title}</Text>
      
      {exam.parts?.map((part, partIndex) => (
        <View key={partIndex} style={styles.part}>
          <Text style={styles.partTitle}>{part.title}</Text>
          
          {part.questions?.map((question, qIndex) => (
            <View key={qIndex} style={styles.question}>
              <Text style={styles.questionText}>
                {qIndex + 1}. {question.questionText}
              </Text>
              
              {question.options?.map((option, oIndex) => (
                <TouchableOpacity
                  key={oIndex}
                  style={[
                    styles.option,
                    answers[question.id] === option && styles.selectedOption,
                  ]}
                  onPress={() => handleMCQAnswer(question.id, option)}
                >
                  <Text>{option}</Text>
                </TouchableOpacity>
              ))}
            </View>
          ))}
        </View>
      ))}

      <Button
        title="Submit Exam"
        onPress={submitExam}
        disabled={loading}
      />
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 16,
    backgroundColor: '#fff',
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    marginBottom: 16,
  },
  part: {
    marginBottom: 24,
  },
  partTitle: {
    fontSize: 18,
    fontWeight: '600',
    marginBottom: 12,
  },
  question: {
    marginBottom: 20,
  },
  questionText: {
    fontSize: 16,
    marginBottom: 8,
  },
  option: {
    padding: 12,
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 8,
    marginBottom: 8,
  },
  selectedOption: {
    backgroundColor: '#e3f2fd',
    borderColor: '#2196f3',
  },
});

export default TakeExamScreen;
```

### 3. Chat Screen

```javascript
// src/screens/chat/ChatScreen.js
import React, { useState, useEffect, useRef } from 'react';
import { View, FlatList, TextInput, TouchableOpacity, StyleSheet } from 'react-native';
import { chatApi } from '../../api/chatApi';
import ChatBubble from '../../components/chat/ChatBubble';

const ChatScreen = () => {
  const [messages, setMessages] = useState([]);
  const [inputText, setInputText] = useState('');
  const [loading, setLoading] = useState(false);
  const flatListRef = useRef(null);

  const sendMessage = async () => {
    if (!inputText.trim()) return;

    const userMessage = {
      id: Date.now().toString(),
      text: inputText,
      sender: 'user',
      timestamp: new Date(),
    };

    setMessages((prev) => [...prev, userMessage]);
    setInputText('');
    setLoading(true);

    try {
      const response = await chatApi.sendMessage(inputText);
      
      const botMessage = {
        id: (Date.now() + 1).toString(),
        text: response.message,
        sender: 'bot',
        timestamp: new Date(),
      };

      setMessages((prev) => [...prev, botMessage]);
    } catch (error) {
      console.error('Failed to send message:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <FlatList
        ref={flatListRef}
        data={messages}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => <ChatBubble message={item} />}
        onContentSizeChange={() => flatListRef.current?.scrollToEnd()}
      />
      
      <View style={styles.inputContainer}>
        <TextInput
          style={styles.input}
          value={inputText}
          onChangeText={setInputText}
          placeholder="Type a message..."
          multiline
        />
        <TouchableOpacity
          style={styles.sendButton}
          onPress={sendMessage}
          disabled={loading}
        >
          <Text style={styles.sendButtonText}>Send</Text>
        </TouchableOpacity>
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  inputContainer: {
    flexDirection: 'row',
    padding: 12,
    backgroundColor: '#fff',
    borderTopWidth: 1,
    borderTopColor: '#ddd',
  },
  input: {
    flex: 1,
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 20,
    paddingHorizontal: 16,
    paddingVertical: 8,
    marginRight: 8,
    maxHeight: 100,
  },
  sendButton: {
    backgroundColor: '#2196f3',
    borderRadius: 20,
    paddingHorizontal: 20,
    justifyContent: 'center',
  },
  sendButtonText: {
    color: '#fff',
    fontWeight: '600',
  },
});

export default ChatScreen;
```

### 4. Upload Subjective Answers Screen (NEW - December 2025)

```javascript
// src/screens/exam/UploadAnswerScreen.js
import React, { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Image,
  ScrollView,
  Alert,
  ActivityIndicator,
} from 'react-native';
import { launchCamera, launchImageLibrary } from 'react-native-image-picker';
import { examApi } from '../../api/examApi';

const UploadAnswerScreen = ({ route, navigation }) => {
  const { examId, studentId } = route.params;
  const [images, setImages] = useState([]);
  const [uploading, setUploading] = useState(false);

  const selectImage = () => {
    Alert.alert(
      'Select Answer Sheet',
      'Choose how you want to upload your answers',
      [
        {
          text: 'Take Photo',
          onPress: () => openCamera(),
        },
        {
          text: 'Choose from Gallery',
          onPress: () => openGallery(),
        },
        {
          text: 'Cancel',
          style: 'cancel',
        },
      ]
    );
  };

  const openCamera = () => {
    launchCamera(
      {
        mediaType: 'photo',
        quality: 0.8,
        saveToPhotos: true,
      },
      (response) => {
        if (response.didCancel) return;
        if (response.errorCode) {
          Alert.alert('Error', response.errorMessage);
          return;
        }
        
        if (response.assets && response.assets[0]) {
          addImage(response.assets[0]);
        }
      }
    );
  };

  const openGallery = () => {
    launchImageLibrary(
      {
        mediaType: 'photo',
        quality: 0.8,
        selectionLimit: 10, // Allow multiple pages
      },
      (response) => {
        if (response.didCancel) return;
        if (response.errorCode) {
          Alert.alert('Error', response.errorMessage);
          return;
        }
        
        if (response.assets) {
          response.assets.forEach((asset) => addImage(asset));
        }
      }
    );
  };

  const addImage = (asset) => {
    const newImage = {
      uri: asset.uri,
      type: asset.type,
      name: asset.fileName || `answer_${Date.now()}.jpg`,
    };
    setImages((prev) => [...prev, newImage]);
  };

  const removeImage = (index) => {
    setImages((prev) => prev.filter((_, i) => i !== index));
  };

  const uploadAnswers = async () => {
    if (images.length === 0) {
      Alert.alert('Error', 'Please add at least one answer sheet image');
      return;
    }

    try {
      setUploading(true);
      
      const result = await examApi.uploadWrittenAnswers(examId, studentId, images);
      
      Alert.alert(
        'Success!',
        'Your answers have been uploaded successfully. AI is now evaluating them. Results will be ready in a few moments.',
        [
          {
            text: 'View Results',
            onPress: () => {
              navigation.navigate('ExamResult', {
                examId,
                studentId,
                submissionId: result.writtenSubmissionId,
              });
            },
          },
        ]
      );
    } catch (error) {
      Alert.alert('Upload Failed', error.message || 'Please try again');
    } finally {
      setUploading(false);
    }
  };

  return (
    <ScrollView style={styles.container}>
      <Text style={styles.title}>Upload Answer Sheet</Text>
      <Text style={styles.subtitle}>
        Take photos of your written answers or select from gallery
      </Text>

      <TouchableOpacity style={styles.selectButton} onPress={selectImage}>
        <Text style={styles.selectButtonText}>+ Add Answer Sheet</Text>
      </TouchableOpacity>

      <View style={styles.imagesContainer}>
        {images.map((image, index) => (
          <View key={index} style={styles.imageWrapper}>
            <Image source={{ uri: image.uri }} style={styles.image} />
            <TouchableOpacity
              style={styles.removeButton}
              onPress={() => removeImage(index)}
            >
              <Text style={styles.removeButtonText}>×</Text>
            </TouchableOpacity>
            <Text style={styles.pageNumber}>Page {index + 1}</Text>
          </View>
        ))}
      </View>

      {images.length > 0 && (
        <View style={styles.uploadSection}>
          <Text style={styles.infoText}>
            {images.length} page{images.length > 1 ? 's' : ''} ready to upload
          </Text>
          <TouchableOpacity
            style={[styles.uploadButton, uploading && styles.uploadButtonDisabled]}
            onPress={uploadAnswers}
            disabled={uploading}
          >
            {uploading ? (
              <ActivityIndicator color="#fff" />
            ) : (
              <Text style={styles.uploadButtonText}>Upload & Get AI Evaluation</Text>
            )}
          </TouchableOpacity>
        </View>
      )}
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 16,
    backgroundColor: '#f5f5f5',
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    marginBottom: 8,
  },
  subtitle: {
    fontSize: 14,
    color: '#666',
    marginBottom: 24,
  },
  selectButton: {
    backgroundColor: '#2196f3',
    padding: 16,
    borderRadius: 8,
    alignItems: 'center',
    marginBottom: 24,
  },
  selectButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  imagesContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 12,
  },
  imageWrapper: {
    position: 'relative',
    width: '48%',
    aspectRatio: 3 / 4,
    marginBottom: 12,
  },
  image: {
    width: '100%',
    height: '100%',
    borderRadius: 8,
    backgroundColor: '#ddd',
  },
  removeButton: {
    position: 'absolute',
    top: 8,
    right: 8,
    backgroundColor: 'rgba(255, 0, 0, 0.8)',
    width: 28,
    height: 28,
    borderRadius: 14,
    alignItems: 'center',
    justifyContent: 'center',
  },
  removeButtonText: {
    color: '#fff',
    fontSize: 20,
    fontWeight: 'bold',
  },
  pageNumber: {
    position: 'absolute',
    bottom: 8,
    left: 8,
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
    color: '#fff',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 4,
    fontSize: 12,
  },
  uploadSection: {
    marginTop: 24,
    marginBottom: 32,
  },
  infoText: {
    textAlign: 'center',
    marginBottom: 12,
    color: '#666',
  },
  uploadButton: {
    backgroundColor: '#4caf50',
    padding: 16,
    borderRadius: 8,
    alignItems: 'center',
  },
  uploadButtonDisabled: {
    backgroundColor: '#9e9e9e',
  },
  uploadButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold',
  },
});

export default UploadAnswerScreen;
```

### 5. Exam Results Screen with AI Feedback (NEW - December 2025)

```javascript
// src/screens/exam/ExamResultScreen.js
import React, { useEffect, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  ActivityIndicator,
  RefreshControl,
} from 'react-native';
import { examApi } from '../../api/examApi';

const ExamResultScreen = ({ route }) => {
  const { examId, studentId } = route.params;
  const [result, setResult] = useState(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  useEffect(() => {
    loadResult();
  }, []);

  const loadResult = async () => {
    try {
      setLoading(true);
      const data = await examApi.getExamResult(examId, studentId);
      setResult(data);
    } catch (error) {
      console.error('Failed to load results:', error);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  const onRefresh = () => {
    setRefreshing(true);
    loadResult();
  };

  const getGradeColor = (grade) => {
    if (grade === 'A+' || grade === 'A') return '#4caf50';
    if (grade === 'B+' || grade === 'B') return '#8bc34a';
    if (grade === 'C') return '#ffc107';
    return '#f44336';
  };

  if (loading) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator size="large" color="#2196f3" />
        <Text style={styles.loadingText}>Loading results...</Text>
      </View>
    );
  }

  if (!result) {
    return (
      <View style={styles.centered}>
        <Text>No results found</Text>
      </View>
    );
  }

  return (
    <ScrollView
      style={styles.container}
      refreshControl={
        <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
      }
    >
      {/* Header */}
      <View style={styles.header}>
        <Text style={styles.examTitle}>{result.examTitle}</Text>
        <View style={[styles.gradeCard, { backgroundColor: getGradeColor(result.grade) }]}>
          <Text style={styles.gradeText}>{result.grade}</Text>
          <Text style={styles.percentageText}>{result.percentage.toFixed(1)}%</Text>
        </View>
      </View>

      {/* Score Summary */}
      <View style={styles.summaryCard}>
        <Text style={styles.sectionTitle}>Score Summary</Text>
        <View style={styles.scoreRow}>
          <Text style={styles.scoreLabel}>Total Score:</Text>
          <Text style={styles.scoreValue}>
            {result.grandScore} / {result.grandTotalMarks}
          </Text>
        </View>
        <View style={styles.scoreRow}>
          <Text style={styles.scoreLabel}>Status:</Text>
          <Text style={[styles.statusText, result.passed ? styles.passed : styles.failed]}>
            {result.passed ? 'PASSED ✓' : 'FAILED ✗'}
          </Text>
        </View>
      </View>

      {/* MCQ Results */}
      {result.mcqResults && result.mcqResults.length > 0 && (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>MCQ Results</Text>
          <View style={styles.scoreRow}>
            <Text>MCQ Score:</Text>
            <Text style={styles.bold}>
              {result.mcqScore} / {result.mcqTotalMarks}
            </Text>
          </View>
        </View>
      )}

      {/* Subjective Results with AI Feedback */}
      {result.subjectiveResults && result.subjectiveResults.length > 0 && (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>
            Subjective Answers - AI Evaluation
          </Text>

          {result.subjectiveResults.map((question, index) => (
            <View key={index} style={styles.questionCard}>
              {/* Question Header */}
              <View style={styles.questionHeader}>
                <Text style={styles.questionNumber}>Question {question.questionNumber}</Text>
                <Text style={[
                  styles.questionScore,
                  question.isFullyCorrect ? styles.correctScore : styles.partialScore
                ]}>
                  {question.earnedMarks} / {question.maxMarks}
                </Text>
              </View>

              <Text style={styles.questionText}>{question.questionText}</Text>

              {/* Student's Answer */}
              <View style={styles.answerSection}>
                <Text style={styles.answerLabel}>Your Answer:</Text>
                <View style={styles.answerBox}>
                  <Text style={styles.answerText}>{question.studentAnswerEcho}</Text>
                </View>
              </View>

              {/* Expected Answer */}
              <View style={styles.answerSection}>
                <Text style={styles.answerLabel}>Expected Answer:</Text>
                <View style={[styles.answerBox, styles.expectedAnswerBox]}>
                  <Text style={styles.answerText}>{question.expectedAnswer}</Text>
                </View>
              </View>

              {/* Step-by-Step Analysis */}
              {question.stepAnalysis && question.stepAnalysis.length > 0 && (
                <View style={styles.stepsSection}>
                  <Text style={styles.stepsTitle}>Step-by-Step Analysis:</Text>
                  {question.stepAnalysis.map((step, stepIndex) => (
                    <View
                      key={stepIndex}
                      style={[
                        styles.stepCard,
                        step.isCorrect ? styles.correctStep : styles.incorrectStep,
                      ]}
                    >
                      <View style={styles.stepHeader}>
                        <Text style={styles.stepNumber}>Step {step.step}</Text>
                        <Text style={styles.stepScore}>
                          {step.marksAwarded} / {step.maxMarksForStep}
                        </Text>
                      </View>
                      <Text style={styles.stepDescription}>{step.description}</Text>
                      <View style={styles.stepStatus}>
                        <Text style={step.isCorrect ? styles.correctText : styles.incorrectText}>
                          {step.isCorrect ? '✓ Correct' : '✗ Needs Improvement'}
                        </Text>
                      </View>
                      <Text style={styles.stepFeedback}>{step.feedback}</Text>
                    </View>
                  ))}
                </View>
              )}

              {/* Overall Feedback */}
              <View style={styles.feedbackSection}>
                <Text style={styles.feedbackLabel}>Overall Feedback:</Text>
                <Text style={styles.feedbackText}>{question.overallFeedback}</Text>
              </View>
            </View>
          ))}
        </View>
      )}
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  centered: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingText: {
    marginTop: 12,
    color: '#666',
  },
  header: {
    backgroundColor: '#2196f3',
    padding: 20,
    alignItems: 'center',
  },
  examTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#fff',
    marginBottom: 16,
    textAlign: 'center',
  },
  gradeCard: {
    width: 100,
    height: 100,
    borderRadius: 50,
    justifyContent: 'center',
    alignItems: 'center',
  },
  gradeText: {
    fontSize: 32,
    fontWeight: 'bold',
    color: '#fff',
  },
  percentageText: {
    fontSize: 16,
    color: '#fff',
    marginTop: 4,
  },
  summaryCard: {
    backgroundColor: '#fff',
    padding: 16,
    margin: 16,
    borderRadius: 8,
    elevation: 2,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    marginBottom: 12,
  },
  scoreRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 8,
  },
  scoreLabel: {
    fontSize: 16,
  },
  scoreValue: {
    fontSize: 18,
    fontWeight: 'bold',
  },
  statusText: {
    fontSize: 16,
    fontWeight: 'bold',
  },
  passed: {
    color: '#4caf50',
  },
  failed: {
    color: '#f44336',
  },
  section: {
    backgroundColor: '#fff',
    padding: 16,
    margin: 16,
    marginTop: 0,
    borderRadius: 8,
    elevation: 2,
  },
  bold: {
    fontWeight: 'bold',
  },
  questionCard: {
    backgroundColor: '#f9f9f9',
    padding: 16,
    borderRadius: 8,
    marginBottom: 16,
  },
  questionHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  questionNumber: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#2196f3',
  },
  questionScore: {
    fontSize: 18,
    fontWeight: 'bold',
  },
  correctScore: {
    color: '#4caf50',
  },
  partialScore: {
    color: '#ff9800',
  },
  questionText: {
    fontSize: 15,
    marginBottom: 12,
    lineHeight: 22,
  },
  answerSection: {
    marginTop: 12,
  },
  answerLabel: {
    fontSize: 14,
    fontWeight: '600',
    color: '#666',
    marginBottom: 6,
  },
  answerBox: {
    backgroundColor: '#fff',
    padding: 12,
    borderRadius: 6,
    borderLeftWidth: 3,
    borderLeftColor: '#2196f3',
  },
  expectedAnswerBox: {
    borderLeftColor: '#4caf50',
  },
  answerText: {
    fontSize: 14,
    lineHeight: 20,
  },
  stepsSection: {
    marginTop: 16,
  },
  stepsTitle: {
    fontSize: 15,
    fontWeight: '600',
    marginBottom: 8,
  },
  stepCard: {
    backgroundColor: '#fff',
    padding: 12,
    borderRadius: 6,
    marginBottom: 8,
    borderLeftWidth: 3,
  },
  correctStep: {
    borderLeftColor: '#4caf50',
  },
  incorrectStep: {
    borderLeftColor: '#f44336',
  },
  stepHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 6,
  },
  stepNumber: {
    fontSize: 14,
    fontWeight: '600',
  },
  stepScore: {
    fontSize: 14,
    fontWeight: 'bold',
  },
  stepDescription: {
    fontSize: 14,
    marginBottom: 6,
  },
  stepStatus: {
    marginBottom: 6,
  },
  correctText: {
    color: '#4caf50',
    fontWeight: '600',
  },
  incorrectText: {
    color: '#f44336',
    fontWeight: '600',
  },
  stepFeedback: {
    fontSize: 13,
    color: '#666',
    fontStyle: 'italic',
  },
  feedbackSection: {
    marginTop: 12,
    padding: 12,
    backgroundColor: '#e3f2fd',
    borderRadius: 6,
  },
  feedbackLabel: {
    fontSize: 14,
    fontWeight: '600',
    color: '#1976d2',
    marginBottom: 6,
  },
  feedbackText: {
    fontSize: 14,
    lineHeight: 20,
    color: '#333',
  },
});

export default ExamResultScreen;
```

## State Management (Redux Toolkit)

```javascript
// src/store/slices/examSlice.js
import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { examApi } from '../../api/examApi';

export const fetchExamHistory = createAsyncThunk(
  'exam/fetchHistory',
  async (studentId) => {
    const response = await examApi.getStudentHistory(studentId);
    return response;
  }
);

export const submitExam = createAsyncThunk(
  'exam/submit',
  async ({ examId, studentId, answers }) => {
    const response = await examApi.submitMCQ({ examId, studentId, answers });
    return response;
  }
);

const examSlice = createSlice({
  name: 'exam',
  initialState: {
    exams: [],
    currentExam: null,
    loading: false,
    error: null,
  },
  reducers: {
    setCurrentExam: (state, action) => {
      state.currentExam = action.payload;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchExamHistory.pending, (state) => {
        state.loading = true;
      })
      .addCase(fetchExamHistory.fulfilled, (state, action) => {
        state.loading = false;
        state.exams = action.payload.items;
      })
      .addCase(fetchExamHistory.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message;
      });
  },
});

export const { setCurrentExam } = examSlice.actions;
export default examSlice.reducer;
```

## Setup Instructions

### React Native Setup

```bash
# Install React Native CLI
npm install -g react-native-cli

# Create new project
npx react-native init SchoolAiMobile
cd SchoolAiMobile

# Install dependencies
npm install @react-navigation/native @react-navigation/stack
npm install react-native-screens react-native-safe-area-context
npm install @react-native-async-storage/async-storage
npm install axios
npm install @reduxjs/toolkit react-redux

# iOS specific
cd ios && pod install && cd ..

# Run the app
npm run android  # For Android
npm run ios      # For iOS
```

### Flutter Setup

```bash
# Create new Flutter project
flutter create school_ai_mobile
cd school_ai_mobile

# Add dependencies to pubspec.yaml
flutter pub add http
flutter pub add provider
flutter pub add shared_preferences
flutter pub add flutter_secure_storage

# Run the app
flutter run
```

## Required Packages

### React Native
```json
{
  "dependencies": {
    "react": "^18.2.0",
    "react-native": "^0.72.0",
    "@react-navigation/native": "^6.1.0",
    "@react-navigation/stack": "^6.3.0",
    "@react-native-async-storage/async-storage": "^1.19.0",
    "axios": "^1.5.0",
    "@reduxjs/toolkit": "^1.9.0",
    "react-redux": "^8.1.0",
    "react-native-vector-icons": "^10.0.0",
    "react-native-gesture-handler": "^2.12.0",
    "react-native-reanimated": "^3.4.0"
  }
}
```

### Flutter
```yaml
dependencies:
  flutter:
    sdk: flutter
  http: ^1.1.0
  provider: ^6.0.0
  shared_preferences: ^2.2.0
  flutter_secure_storage: ^9.0.0
  intl: ^0.18.0
```

## Key Features to Implement

### Student Features
1. **Authentication**
   - Login/Register
   - Password reset
   - Profile management

2. **Exam Management**
   - Browse available exams
   - Take exams (MCQ + Written)
   - View results and feedback
   - Track exam history

3. **AI Chat Assistant**
   - Ask questions
   - Get homework help
   - Clarify concepts
   - Chat history

4. **Profile & History**
   - View grades
   - Track progress
   - Performance analytics

### Teacher Features
1. **Exam Creation**
   - Generate AI exams
   - Create custom questions
   - Set exam parameters

2. **Analytics Dashboard**
   - View student submissions
   - Grade written answers
   - Class performance stats

3. **Student Management**
   - View student list
   - Track individual progress

## Offline Support

```javascript
// src/utils/storage.js
import AsyncStorage from '@react-native-async-storage/async-storage';

export const cacheManager = {
  // Cache exam data for offline access
  cacheExam: async (examId, examData) => {
    await AsyncStorage.setItem(`exam_${examId}`, JSON.stringify(examData));
  },

  // Get cached exam
  getCachedExam: async (examId) => {
    const data = await AsyncStorage.getItem(`exam_${examId}`);
    return data ? JSON.parse(data) : null;
  },

  // Store pending submissions
  storePendingSubmission: async (submission) => {
    const pending = await AsyncStorage.getItem('pending_submissions');
    const submissions = pending ? JSON.parse(pending) : [];
    submissions.push(submission);
    await AsyncStorage.setItem('pending_submissions', JSON.stringify(submissions));
  },

  // Sync pending submissions when online
  syncPendingSubmissions: async () => {
    const pending = await AsyncStorage.getItem('pending_submissions');
    if (!pending) return;

    const submissions = JSON.parse(pending);
    for (const submission of submissions) {
      try {
        await examApi.submitMCQ(submission);
      } catch (error) {
        console.error('Failed to sync submission:', error);
      }
    }
    await AsyncStorage.removeItem('pending_submissions');
  },
};
```

## Push Notifications

```javascript
// src/services/notificationService.js
import messaging from '@react-native-firebase/messaging';
import AsyncStorage from '@react-native-async-storage/async-storage';

export const notificationService = {
  // Request permission
  requestPermission: async () => {
    const authStatus = await messaging().requestPermission();
    return authStatus === messaging.AuthorizationStatus.AUTHORIZED;
  },

  // Get FCM token
  getToken: async () => {
    const token = await messaging().getToken();
    await AsyncStorage.setItem('fcmToken', token);
    return token;
  },

  // Listen for notifications
  onMessageReceived: (callback) => {
    messaging().onMessage(async (remoteMessage) => {
      callback(remoteMessage);
    });
  },

  // Background message handler
  setBackgroundMessageHandler: () => {
    messaging().setBackgroundMessageHandler(async (remoteMessage) => {
      console.log('Background message:', remoteMessage);
    });
  },
};
```

## Security Best Practices

1. **Secure Storage**
   ```javascript
   // Use react-native-keychain for sensitive data
   import * as Keychain from 'react-native-keychain';

   // Store credentials securely
   await Keychain.setGenericPassword('username', 'password');

   // Retrieve credentials
   const credentials = await Keychain.getGenericPassword();
   ```

2. **API Security**
   - Always use HTTPS
   - Implement certificate pinning
   - Store API keys securely
   - Implement token refresh mechanism

3. **Data Encryption**
   - Encrypt sensitive data at rest
   - Use secure communication channels
   - Implement biometric authentication

## Testing

```bash
# Unit tests
npm test

# E2E tests with Detox
npm run e2e:ios
npm run e2e:android

# Component tests with Jest
npm run test:components
```

## Deployment

### iOS
1. Configure signing in Xcode
2. Archive the app
3. Upload to App Store Connect
4. Submit for review

### Android
1. Generate signed APK/AAB
2. Upload to Google Play Console
3. Submit for review

```bash
# Generate Android release build
cd android
./gradlew assembleRelease

# Build file location
# android/app/build/outputs/apk/release/app-release.apk
```

## Environment Configuration

```javascript
// src/constants/config.js
const ENV = {
  dev: {
    API_URL: 'http://localhost:8080',
  },
  staging: {
    API_URL: 'https://staging-api.azurewebsites.net',
  },
  production: {
    API_URL: 'https://api.yourschool.com',
  },
};

const getEnvVars = () => {
  if (__DEV__) return ENV.dev;
  return ENV.production;
};

export default getEnvVars();
```

## Performance Optimization

1. **Lazy Loading**
   - Load screens on demand
   - Implement pagination
   - Cache images

2. **Memory Management**
   - Clean up listeners
   - Avoid memory leaks
   - Optimize images

3. **Network Optimization**
   - Implement request caching
   - Batch API calls
   - Use compression

## Monitoring & Analytics

```javascript
// src/services/analyticsService.js
import analytics from '@react-native-firebase/analytics';

export const analyticsService = {
  logEvent: async (eventName, params = {}) => {
    await analytics().logEvent(eventName, params);
  },

  logScreenView: async (screenName) => {
    await analytics().logScreenView({
      screen_name: screenName,
      screen_class: screenName,
    });
  },

  setUserId: async (userId) => {
    await analytics().setUserId(userId);
  },
};
```

## Next Steps

1. ✅ Set up development environment
2. ✅ Create project structure
3. ✅ Implement API integration
4. ✅ Build authentication flow
5. ✅ Implement exam features
6. ✅ Add chat functionality
7. ✅ Implement offline support
8. ✅ Add push notifications
9. ✅ Testing & debugging
10. ✅ Deploy to app stores

## Resources

- [React Native Documentation](https://reactnative.dev/)
- [Flutter Documentation](https://flutter.dev/)
- [React Navigation](https://reactnavigation.org/)
- [Redux Toolkit](https://redux-toolkit.js.org/)
- [Axios Documentation](https://axios-http.com/)

## Support

For backend API documentation, see:
- `API_REFERENCE.md` - Complete API endpoints
- `EXAM_ANALYTICS_API.md` - Analytics endpoints

## License

MIT License - See LICENSE file for details
