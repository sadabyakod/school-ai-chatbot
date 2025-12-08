# Mobile Application Implementation Guide

## Overview

This guide provides a complete roadmap for building a mobile application (iOS & Android) that integrates with the School AI Chatbot backend API.

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

  // Submit written answer
  submitWritten: async (submission) => {
    const response = await apiClient.post('/api/exam/submit-written', submission);
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
