# Mobile App - Syllabus & Question Papers API Documentation

## Overview

This document provides API endpoints for implementing the **Download Syllabus** and **Model Question Papers** features in the SmartStudy mobile app. These endpoints allow fetching files organized by subject and class (grade).

**Base URL:** `https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net`

---

## Part 1: Syllabus API Endpoints

### 1. Get All Available Syllabi (Grouped)

Fetches all uploaded syllabi grouped by subject, grade, and medium.

```
GET /api/file/syllabus
```

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `grade` | string | No | Filter by class (e.g., "2nd PUC", "10th") |
| `medium` | string | No | Filter by language medium (e.g., "English", "Kannada") |

**Example Request:**
```bash
GET /api/file/syllabus?grade=2nd%20PUC&medium=English
```

**Response:**
```json
{
  "status": "success",
  "count": 6,
  "syllabi": [
    {
      "subject": "Mathematics",
      "grade": "2nd PUC",
      "medium": "English",
      "fileCount": 2,
      "latestUpload": "2025-12-10T14:30:00Z",
      "files": [
        {
          "id": 1,
          "fileName": "Mathematics_2ndPUC_Chapter1.pdf",
          "blobUrl": "https://storage.blob.core.windows.net/...",
          "uploadedAt": "2025-12-10T14:30:00Z"
        }
      ]
    },
    {
      "subject": "Physics",
      "grade": "2nd PUC",
      "medium": "English",
      "fileCount": 3,
      "latestUpload": "2025-12-09T10:15:00Z",
      "files": [...]
    }
  ]
}
```

---

### 2. Get Syllabus by Subject

Fetches all syllabus files for a specific subject.

```
GET /api/file/syllabus/{subject}
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `subject` | string | Yes | Subject name (case-insensitive) |

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `grade` | string | No | Filter by class |
| `medium` | string | No | Filter by language medium |

**Example Request:**
```bash
GET /api/file/syllabus/Mathematics?grade=2nd%20PUC
```

**Response:**
```json
{
  "status": "success",
  "subject": "Mathematics",
  "grade": "2nd PUC",
  "medium": null,
  "count": 5,
  "files": [
    {
      "id": 1,
      "fileName": "Mathematics_Relations_Functions.pdf",
      "blobUrl": "https://storage.blob.core.windows.net/...",
      "subject": "Mathematics",
      "grade": "2nd PUC",
      "medium": "English",
      "uploadedAt": "2025-12-10T14:30:00Z",
      "totalChunks": 25
    },
    {
      "id": 2,
      "fileName": "Mathematics_Matrices.pdf",
      "blobUrl": "https://storage.blob.core.windows.net/...",
      "subject": "Mathematics",
      "grade": "2nd PUC",
      "medium": "English",
      "uploadedAt": "2025-12-08T09:00:00Z",
      "totalChunks": 18
    }
  ]
}
```

---

### 3. Get Available Grades/Classes

Fetches list of all grades that have uploaded syllabi.

```
GET /api/file/syllabus/grades
```

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `medium` | string | No | Filter by language medium |

**Example Request:**
```bash
GET /api/file/syllabus/grades
```

**Response:**
```json
{
  "status": "success",
  "count": 3,
  "grades": [
    {
      "grade": "10th",
      "subjectCount": 5,
      "fileCount": 12
    },
    {
      "grade": "1st PUC",
      "subjectCount": 6,
      "fileCount": 18
    },
    {
      "grade": "2nd PUC",
      "subjectCount": 6,
      "fileCount": 24
    }
  ]
}
```

---

### 4. Get Available Subjects

Fetches list of all subjects available for a specific grade.

```
GET /api/file/syllabus/subjects
```

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `grade` | string | No | Filter by class |
| `medium` | string | No | Filter by language medium |

**Example Request:**
```bash
GET /api/file/syllabus/subjects?grade=2nd%20PUC
```

**Response:**
```json
{
  "status": "success",
  "grade": "2nd PUC",
  "medium": null,
  "count": 6,
  "subjects": [
    {
      "subject": "Biology",
      "fileCount": 4,
      "latestUpload": "2025-12-12T11:00:00Z"
    },
    {
      "subject": "Chemistry",
      "fileCount": 3,
      "latestUpload": "2025-12-11T15:30:00Z"
    },
    {
      "subject": "Computer Science",
      "fileCount": 2,
      "latestUpload": "2025-12-10T09:00:00Z"
    },
    {
      "subject": "English",
      "fileCount": 2,
      "latestUpload": "2025-12-09T14:00:00Z"
    },
    {
      "subject": "Mathematics",
      "fileCount": 5,
      "latestUpload": "2025-12-10T14:30:00Z"
    },
    {
      "subject": "Physics",
      "fileCount": 3,
      "latestUpload": "2025-12-09T10:15:00Z"
    }
  ]
}
```

---

### 5. Get Download URL for Specific File

Fetches download URL and metadata for a specific syllabus file.

```
GET /api/file/syllabus/download/{fileId}
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `fileId` | integer | Yes | The unique file ID |

**Example Request:**
```bash
GET /api/file/syllabus/download/5
```

**Response:**
```json
{
  "status": "success",
  "fileId": 5,
  "fileName": "Physics_Electrostatics.pdf",
  "subject": "Physics",
  "grade": "2nd PUC",
  "medium": "English",
  "downloadUrl": "https://storage.blob.core.windows.net/syllabi/Physics_Electrostatics.pdf",
  "uploadedAt": "2025-12-09T10:15:00Z"
}
```

**Error Response (File Not Found):**
```json
{
  "status": "error",
  "message": "Syllabus file not found."
}
```

---

## Part 2: Model Question Papers API Endpoints

### 6. Get All Question Papers (Grouped)

Fetches all model question papers grouped by subject, grade, and medium.

```
GET /api/file/question-papers
```

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `grade` | string | No | Filter by class (e.g., "2nd PUC", "10th") |
| `medium` | string | No | Filter by language medium |
| `academicYear` | string | No | Filter by academic year (e.g., "2024-25") |

**Example Request:**
```bash
GET /api/file/question-papers?grade=2nd%20PUC&academicYear=2024-25
```

**Response:**
```json
{
  "status": "success",
  "count": 6,
  "totalPapers": 18,
  "questionPapers": [
    {
      "subject": "Mathematics",
      "grade": "2nd PUC",
      "medium": "English",
      "paperCount": 3,
      "latestUpload": "2025-12-10T14:30:00Z",
      "papers": [
        {
          "id": 101,
          "fileName": "Mathematics_2ndPUC_ModelPaper1_2024-25.pdf",
          "blobUrl": "https://storage.blob.core.windows.net/...",
          "academicYear": "2024-25",
          "chapter": null,
          "uploadedAt": "2025-12-10T14:30:00Z"
        }
      ]
    },
    {
      "subject": "Physics",
      "grade": "2nd PUC",
      "medium": "English",
      "paperCount": 3,
      "latestUpload": "2025-12-09T10:15:00Z",
      "papers": [...]
    }
  ]
}
```

---

### 7. Get Question Papers by Subject

Fetches all model question papers for a specific subject.

```
GET /api/file/question-papers/{subject}
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `subject` | string | Yes | Subject name (case-insensitive) |

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `grade` | string | No | Filter by class |
| `medium` | string | No | Filter by language medium |
| `academicYear` | string | No | Filter by academic year |

**Example Request:**
```bash
GET /api/file/question-papers/Mathematics?grade=2nd%20PUC
```

**Response:**
```json
{
  "status": "success",
  "subject": "Mathematics",
  "grade": "2nd PUC",
  "medium": null,
  "academicYear": null,
  "count": 5,
  "papers": [
    {
      "id": 101,
      "fileName": "Mathematics_2ndPUC_ModelPaper1_2024-25.pdf",
      "blobUrl": "https://storage.blob.core.windows.net/...",
      "subject": "Mathematics",
      "grade": "2nd PUC",
      "medium": "English",
      "academicYear": "2024-25",
      "chapter": null,
      "uploadedAt": "2025-12-10T14:30:00Z",
      "totalChunks": 0
    }
  ]
}
```

---

### 8. Get Question Papers by Grade/Class

Fetches all model question papers for a specific grade, grouped by subject.

```
GET /api/file/question-papers/grade/{grade}
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `grade` | string | Yes | Class/grade name (e.g., "2nd PUC") |

**Example Request:**
```bash
GET /api/file/question-papers/grade/2nd%20PUC
```

**Response:**
```json
{
  "status": "success",
  "grade": "2nd PUC",
  "medium": null,
  "academicYear": null,
  "subjectCount": 6,
  "totalPapers": 18,
  "subjects": [
    {
      "subject": "Mathematics",
      "paperCount": 3,
      "latestUpload": "2025-12-10T14:30:00Z",
      "papers": [
        {
          "id": 101,
          "fileName": "Mathematics_2ndPUC_ModelPaper1_2024-25.pdf",
          "blobUrl": "https://storage.blob.core.windows.net/...",
          "academicYear": "2024-25",
          "chapter": null,
          "uploadedAt": "2025-12-10T14:30:00Z"
        }
      ]
    }
  ]
}
```

---

### 9. Get Available Grades with Question Papers

Fetches list of all grades that have model question papers.

```
GET /api/file/question-papers/grades
```

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `medium` | string | No | Filter by language medium |
| `academicYear` | string | No | Filter by academic year |

**Response:**
```json
{
  "status": "success",
  "count": 3,
  "grades": [
    {
      "grade": "10th",
      "subjectCount": 5,
      "paperCount": 15,
      "academicYears": ["2023-24", "2024-25"]
    },
    {
      "grade": "1st PUC",
      "subjectCount": 6,
      "paperCount": 12,
      "academicYears": ["2024-25"]
    },
    {
      "grade": "2nd PUC",
      "subjectCount": 6,
      "paperCount": 18,
      "academicYears": ["2023-24", "2024-25"]
    }
  ]
}
```

---

### 10. Get Available Subjects with Question Papers

Fetches list of subjects that have model question papers.

```
GET /api/file/question-papers/subjects
```

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `grade` | string | No | Filter by class |
| `medium` | string | No | Filter by language medium |
| `academicYear` | string | No | Filter by academic year |

**Example Request:**
```bash
GET /api/file/question-papers/subjects?grade=2nd%20PUC
```

**Response:**
```json
{
  "status": "success",
  "grade": "2nd PUC",
  "medium": null,
  "academicYear": null,
  "count": 6,
  "subjects": [
    {
      "subject": "Mathematics",
      "paperCount": 3,
      "latestUpload": "2025-12-10T14:30:00Z",
      "grades": ["2nd PUC"],
      "academicYears": ["2023-24", "2024-25"]
    },
    {
      "subject": "Physics",
      "paperCount": 3,
      "latestUpload": "2025-12-09T10:15:00Z",
      "grades": ["2nd PUC"],
      "academicYears": ["2024-25"]
    }
  ]
}
```

---

### 11. Get Question Paper Download URL

Fetches download URL for a specific question paper.

```
GET /api/file/question-papers/download/{fileId}
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `fileId` | integer | Yes | The unique file ID |

**Response:**
```json
{
  "status": "success",
  "fileId": 101,
  "fileName": "Mathematics_2ndPUC_ModelPaper1_2024-25.pdf",
  "subject": "Mathematics",
  "grade": "2nd PUC",
  "medium": "English",
  "academicYear": "2024-25",
  "chapter": null,
  "downloadUrl": "https://storage.blob.core.windows.net/...",
  "uploadedAt": "2025-12-10T14:30:00Z"
}
```

---

### 12. Get Available Academic Years

Fetches list of academic years that have question papers.

```
GET /api/file/question-papers/years
```

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `grade` | string | No | Filter by class |
| `subject` | string | No | Filter by subject |
| `medium` | string | No | Filter by language medium |

**Response:**
```json
{
  "status": "success",
  "count": 3,
  "years": [
    {
      "academicYear": "2024-25",
      "paperCount": 24,
      "subjectCount": 6,
      "gradeCount": 3
    },
    {
      "academicYear": "2023-24",
      "paperCount": 18,
      "subjectCount": 6,
      "gradeCount": 2
    }
  ]
}
```

---

## Mobile App Implementation Guide

### React Native / Expo Implementation

#### 1. API Service

Create `src/services/syllabusApi.ts`:

```typescript
const API_BASE = 'https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net';

export interface SyllabusFile {
  id: number;
  fileName: string;
  blobUrl: string;
  subject: string;
  grade: string;
  medium: string;
  uploadedAt: string;
  totalChunks?: number;
}

export interface SubjectSyllabus {
  subject: string;
  grade: string;
  medium: string;
  fileCount: number;
  latestUpload: string;
  files: SyllabusFile[];
}

export interface SubjectInfo {
  subject: string;
  fileCount: number;
  latestUpload: string;
}

export interface GradeInfo {
  grade: string;
  subjectCount: number;
  fileCount: number;
}

// Get all syllabi grouped by subject
export async function getAvailableSyllabi(
  grade?: string,
  medium?: string
): Promise<SubjectSyllabus[]> {
  const params = new URLSearchParams();
  if (grade) params.append('grade', grade);
  if (medium) params.append('medium', medium);
  
  const url = `${API_BASE}/api/file/syllabus${params.toString() ? '?' + params : ''}`;
  const response = await fetch(url);
  const data = await response.json();
  
  if (data.status === 'success') {
    return data.syllabi;
  }
  throw new Error(data.message || 'Failed to fetch syllabi');
}

// Get subjects for a grade
export async function getSubjectsForGrade(
  grade?: string,
  medium?: string
): Promise<SubjectInfo[]> {
  const params = new URLSearchParams();
  if (grade) params.append('grade', grade);
  if (medium) params.append('medium', medium);
  
  const url = `${API_BASE}/api/file/syllabus/subjects?${params}`;
  const response = await fetch(url);
  const data = await response.json();
  
  if (data.status === 'success') {
    return data.subjects;
  }
  throw new Error(data.message || 'Failed to fetch subjects');
}

// Get files for a subject
export async function getSyllabusForSubject(
  subject: string,
  grade?: string,
  medium?: string
): Promise<SyllabusFile[]> {
  const params = new URLSearchParams();
  if (grade) params.append('grade', grade);
  if (medium) params.append('medium', medium);
  
  const url = `${API_BASE}/api/file/syllabus/${encodeURIComponent(subject)}?${params}`;
  const response = await fetch(url);
  const data = await response.json();
  
  if (data.status === 'success') {
    return data.files;
  }
  throw new Error(data.message || 'Failed to fetch syllabus files');
}

// Get available grades
export async function getAvailableGrades(medium?: string): Promise<GradeInfo[]> {
  const params = medium ? `?medium=${encodeURIComponent(medium)}` : '';
  const url = `${API_BASE}/api/file/syllabus/grades${params}`;
  const response = await fetch(url);
  const data = await response.json();
  
  if (data.status === 'success') {
    return data.grades;
  }
  throw new Error(data.message || 'Failed to fetch grades');
}

// Get download URL
export async function getDownloadUrl(fileId: number): Promise<{
  fileName: string;
  downloadUrl: string;
  subject: string;
  grade: string;
}> {
  const url = `${API_BASE}/api/file/syllabus/download/${fileId}`;
  const response = await fetch(url);
  const data = await response.json();
  
  if (data.status === 'success') {
    return data;
  }
  throw new Error(data.message || 'Failed to get download URL');
}
```

#### 2. Question Papers API Service

Create `src/services/questionPapersApi.ts`:

```typescript
const API_BASE = 'https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net';

export interface QuestionPaper {
  id: number;
  fileName: string;
  blobUrl: string;
  subject: string;
  grade: string;
  medium: string;
  academicYear: string | null;
  chapter: string | null;
  uploadedAt: string;
  totalChunks?: number;
}

export interface SubjectPapers {
  subject: string;
  grade: string;
  medium: string;
  paperCount: number;
  latestUpload: string;
  papers: QuestionPaper[];
}

export interface QuestionPaperSubjectInfo {
  subject: string;
  paperCount: number;
  latestUpload: string;
  grades: string[];
  academicYears: string[];
}

export interface QuestionPaperGradeInfo {
  grade: string;
  subjectCount: number;
  paperCount: number;
  academicYears: string[];
}

export interface AcademicYearInfo {
  academicYear: string;
  paperCount: number;
  subjectCount: number;
  gradeCount: number;
}

// Get all question papers grouped by subject
export async function getAllQuestionPapers(
  grade?: string,
  medium?: string,
  academicYear?: string
): Promise<SubjectPapers[]> {
  const params = new URLSearchParams();
  if (grade) params.append('grade', grade);
  if (medium) params.append('medium', medium);
  if (academicYear) params.append('academicYear', academicYear);
  
  const url = `${API_BASE}/api/file/question-papers${params.toString() ? '?' + params : ''}`;
  const response = await fetch(url);
  const data = await response.json();
  
  if (data.status === 'success') {
    return data.questionPapers;
  }
  throw new Error(data.message || 'Failed to fetch question papers');
}

// Get question papers for a subject
export async function getQuestionPapersBySubject(
  subject: string,
  grade?: string,
  medium?: string,
  academicYear?: string
): Promise<QuestionPaper[]> {
  const params = new URLSearchParams();
  if (grade) params.append('grade', grade);
  if (medium) params.append('medium', medium);
  if (academicYear) params.append('academicYear', academicYear);
  
  const url = `${API_BASE}/api/file/question-papers/${encodeURIComponent(subject)}?${params}`;
  const response = await fetch(url);
  const data = await response.json();
  
  if (data.status === 'success') {
    return data.papers;
  }
  throw new Error(data.message || 'Failed to fetch question papers');
}

// Get question papers for a grade (grouped by subject)
export async function getQuestionPapersByGrade(
  grade: string,
  medium?: string,
  academicYear?: string
): Promise<{ subjects: SubjectPapers[], totalPapers: number }> {
  const params = new URLSearchParams();
  if (medium) params.append('medium', medium);
  if (academicYear) params.append('academicYear', academicYear);
  
  const url = `${API_BASE}/api/file/question-papers/grade/${encodeURIComponent(grade)}?${params}`;
  const response = await fetch(url);
  const data = await response.json();
  
  if (data.status === 'success') {
    return { subjects: data.subjects, totalPapers: data.totalPapers };
  }
  throw new Error(data.message || 'Failed to fetch question papers');
}

// Get available grades with question papers
export async function getQuestionPaperGrades(
  medium?: string,
  academicYear?: string
): Promise<QuestionPaperGradeInfo[]> {
  const params = new URLSearchParams();
  if (medium) params.append('medium', medium);
  if (academicYear) params.append('academicYear', academicYear);
  
  const url = `${API_BASE}/api/file/question-papers/grades?${params}`;
  const response = await fetch(url);
  const data = await response.json();
  
  if (data.status === 'success') {
    return data.grades;
  }
  throw new Error(data.message || 'Failed to fetch grades');
}

// Get available subjects with question papers
export async function getQuestionPaperSubjects(
  grade?: string,
  medium?: string,
  academicYear?: string
): Promise<QuestionPaperSubjectInfo[]> {
  const params = new URLSearchParams();
  if (grade) params.append('grade', grade);
  if (medium) params.append('medium', medium);
  if (academicYear) params.append('academicYear', academicYear);
  
  const url = `${API_BASE}/api/file/question-papers/subjects?${params}`;
  const response = await fetch(url);
  const data = await response.json();
  
  if (data.status === 'success') {
    return data.subjects;
  }
  throw new Error(data.message || 'Failed to fetch subjects');
}

// Get available academic years
export async function getAcademicYears(
  grade?: string,
  subject?: string,
  medium?: string
): Promise<AcademicYearInfo[]> {
  const params = new URLSearchParams();
  if (grade) params.append('grade', grade);
  if (subject) params.append('subject', subject);
  if (medium) params.append('medium', medium);
  
  const url = `${API_BASE}/api/file/question-papers/years?${params}`;
  const response = await fetch(url);
  const data = await response.json();
  
  if (data.status === 'success') {
    return data.years;
  }
  throw new Error(data.message || 'Failed to fetch academic years');
}

// Get download URL for question paper
export async function getQuestionPaperDownloadUrl(fileId: number): Promise<{
  fileName: string;
  downloadUrl: string;
  subject: string;
  grade: string;
  academicYear: string | null;
}> {
  const url = `${API_BASE}/api/file/question-papers/download/${fileId}`;
  const response = await fetch(url);
  const data = await response.json();
  
  if (data.status === 'success') {
    return data;
  }
  throw new Error(data.message || 'Failed to get download URL');
}
```

#### 3. Download Syllabus Screen

Create `src/screens/DownloadSyllabusScreen.tsx`:

```tsx
import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  FlatList,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
  Alert,
  Linking,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getSubjectsForGrade, getSyllabusForSubject, SubjectInfo, SyllabusFile } from '../services/syllabusApi';

// Subject icons mapping
const SUBJECT_ICONS: Record<string, string> = {
  Mathematics: 'calculator-outline',
  Physics: 'planet-outline',
  Chemistry: 'flask-outline',
  Biology: 'leaf-outline',
  English: 'book-outline',
  'Computer Science': 'desktop-outline',
  default: 'document-outline',
};

const SUBJECT_COLORS: Record<string, string> = {
  Mathematics: '#3B82F6',
  Physics: '#8B5CF6',
  Chemistry: '#10B981',
  Biology: '#F59E0B',
  English: '#EC4899',
  'Computer Science': '#6366F1',
  default: '#6B7280',
};

export default function DownloadSyllabusScreen() {
  const [subjects, setSubjects] = useState<SubjectInfo[]>([]);
  const [selectedSubject, setSelectedSubject] = useState<string | null>(null);
  const [files, setFiles] = useState<SyllabusFile[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingFiles, setLoadingFiles] = useState(false);

  const grade = '2nd PUC'; // Can be made dynamic
  const medium = 'English';

  useEffect(() => {
    loadSubjects();
  }, []);

  const loadSubjects = async () => {
    try {
      setLoading(true);
      const data = await getSubjectsForGrade(grade, medium);
      setSubjects(data);
    } catch (error) {
      Alert.alert('Error', 'Failed to load subjects');
    } finally {
      setLoading(false);
    }
  };

  const handleSubjectPress = async (subject: string) => {
    try {
      setSelectedSubject(subject);
      setLoadingFiles(true);
      const data = await getSyllabusForSubject(subject, grade, medium);
      setFiles(data);
    } catch (error) {
      Alert.alert('Error', 'Failed to load files');
    } finally {
      setLoadingFiles(false);
    }
  };

  const handleDownload = async (file: SyllabusFile) => {
    try {
      // Open PDF in browser/viewer
      await Linking.openURL(file.blobUrl);
    } catch (error) {
      Alert.alert('Error', 'Failed to open file');
    }
  };

  const renderSubjectCard = ({ item }: { item: SubjectInfo }) => {
    const icon = SUBJECT_ICONS[item.subject] || SUBJECT_ICONS.default;
    const color = SUBJECT_COLORS[item.subject] || SUBJECT_COLORS.default;

    return (
      <TouchableOpacity
        style={[styles.subjectCard, { borderLeftColor: color }]}
        onPress={() => handleSubjectPress(item.subject)}
      >
        <View style={[styles.iconContainer, { backgroundColor: color + '20' }]}>
          <Ionicons name={icon as any} size={24} color={color} />
        </View>
        <View style={styles.subjectInfo}>
          <Text style={styles.subjectName}>{item.subject}</Text>
          <Text style={styles.fileCount}>{item.fileCount} file(s)</Text>
        </View>
        <Ionicons name="chevron-forward" size={20} color="#9CA3AF" />
      </TouchableOpacity>
    );
  };

  const renderFileItem = ({ item }: { item: SyllabusFile }) => (
    <TouchableOpacity
      style={styles.fileCard}
      onPress={() => handleDownload(item)}
    >
      <Ionicons name="document-text-outline" size={24} color="#3B82F6" />
      <View style={styles.fileInfo}>
        <Text style={styles.fileName} numberOfLines={2}>
          {item.fileName}
        </Text>
        <Text style={styles.uploadDate}>
          {new Date(item.uploadedAt).toLocaleDateString()}
        </Text>
      </View>
      <Ionicons name="download-outline" size={24} color="#10B981" />
    </TouchableOpacity>
  );

  if (loading) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator size="large" color="#3B82F6" />
        <Text style={styles.loadingText}>Loading subjects...</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <Text style={styles.title}>Download Syllabus</Text>
        <Text style={styles.subtitle}>
          Karnataka State Board | {grade} | 2024-25
        </Text>
      </View>

      {/* Subject List or File List */}
      {selectedSubject ? (
        <View style={styles.content}>
          <TouchableOpacity
            style={styles.backButton}
            onPress={() => {
              setSelectedSubject(null);
              setFiles([]);
            }}
          >
            <Ionicons name="arrow-back" size={20} color="#3B82F6" />
            <Text style={styles.backText}>Back to Subjects</Text>
          </TouchableOpacity>
          
          <Text style={styles.sectionTitle}>{selectedSubject} Files</Text>
          
          {loadingFiles ? (
            <ActivityIndicator size="small" color="#3B82F6" />
          ) : (
            <FlatList
              data={files}
              keyExtractor={(item) => item.id.toString()}
              renderItem={renderFileItem}
              contentContainerStyle={styles.listContainer}
              ListEmptyComponent={
                <Text style={styles.emptyText}>No files available</Text>
              }
            />
          )}
        </View>
      ) : (
        <FlatList
          data={subjects}
          keyExtractor={(item) => item.subject}
          renderItem={renderSubjectCard}
          contentContainerStyle={styles.listContainer}
          ListEmptyComponent={
            <Text style={styles.emptyText}>No subjects available</Text>
          }
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#F9FAFB',
  },
  centered: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingText: {
    marginTop: 12,
    color: '#6B7280',
  },
  header: {
    backgroundColor: '#3B82F6',
    padding: 20,
    paddingTop: 50,
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#FFFFFF',
  },
  subtitle: {
    fontSize: 14,
    color: '#BFDBFE',
    marginTop: 4,
  },
  content: {
    flex: 1,
  },
  backButton: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 16,
  },
  backText: {
    marginLeft: 8,
    color: '#3B82F6',
    fontWeight: '500',
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#111827',
    paddingHorizontal: 16,
    marginBottom: 12,
  },
  listContainer: {
    padding: 16,
  },
  subjectCard: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    borderLeftWidth: 4,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.05,
    shadowRadius: 4,
    elevation: 2,
  },
  iconContainer: {
    width: 48,
    height: 48,
    borderRadius: 12,
    justifyContent: 'center',
    alignItems: 'center',
  },
  subjectInfo: {
    flex: 1,
    marginLeft: 12,
  },
  subjectName: {
    fontSize: 16,
    fontWeight: '600',
    color: '#111827',
  },
  fileCount: {
    fontSize: 12,
    color: '#6B7280',
    marginTop: 2,
  },
  fileCard: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.05,
    shadowRadius: 4,
    elevation: 2,
  },
  fileInfo: {
    flex: 1,
    marginLeft: 12,
  },
  fileName: {
    fontSize: 14,
    fontWeight: '500',
    color: '#111827',
  },
  uploadDate: {
    fontSize: 12,
    color: '#9CA3AF',
    marginTop: 2,
  },
  emptyText: {
    textAlign: 'center',
    color: '#9CA3AF',
    marginTop: 40,
  },
});
```

#### 4. Question Papers Screen

Create `src/screens/QuestionPapersScreen.tsx`:

```tsx
import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  FlatList,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
  Alert,
  Linking,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { Picker } from '@react-native-picker/picker';
import {
  getQuestionPaperSubjects,
  getQuestionPapersBySubject,
  getAcademicYears,
  QuestionPaperSubjectInfo,
  QuestionPaper,
} from '../services/questionPapersApi';

const SUBJECT_ICONS: Record<string, string> = {
  Mathematics: 'calculator-outline',
  Physics: 'planet-outline',
  Chemistry: 'flask-outline',
  Biology: 'leaf-outline',
  English: 'book-outline',
  'Computer Science': 'desktop-outline',
  default: 'document-outline',
};

const SUBJECT_COLORS: Record<string, string> = {
  Mathematics: '#3B82F6',
  Physics: '#8B5CF6',
  Chemistry: '#10B981',
  Biology: '#F59E0B',
  English: '#EC4899',
  'Computer Science': '#6366F1',
  default: '#6B7280',
};

export default function QuestionPapersScreen() {
  const [subjects, setSubjects] = useState<QuestionPaperSubjectInfo[]>([]);
  const [selectedSubject, setSelectedSubject] = useState<string | null>(null);
  const [papers, setPapers] = useState<QuestionPaper[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingPapers, setLoadingPapers] = useState(false);
  const [selectedYear, setSelectedYear] = useState<string>('');
  const [availableYears, setAvailableYears] = useState<string[]>([]);

  const grade = '2nd PUC';
  const medium = 'English';

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      const [subjectsData, yearsData] = await Promise.all([
        getQuestionPaperSubjects(grade, medium),
        getAcademicYears(grade, undefined, medium),
      ]);
      setSubjects(subjectsData);
      setAvailableYears(yearsData.map((y) => y.academicYear));
    } catch (error) {
      Alert.alert('Error', 'Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  const handleSubjectPress = async (subject: string) => {
    try {
      setSelectedSubject(subject);
      setLoadingPapers(true);
      const data = await getQuestionPapersBySubject(
        subject,
        grade,
        medium,
        selectedYear || undefined
      );
      setPapers(data);
    } catch (error) {
      Alert.alert('Error', 'Failed to load papers');
    } finally {
      setLoadingPapers(false);
    }
  };

  const handleDownload = async (paper: QuestionPaper) => {
    try {
      await Linking.openURL(paper.blobUrl);
    } catch (error) {
      Alert.alert('Error', 'Failed to open file');
    }
  };

  const renderSubjectCard = ({ item }: { item: QuestionPaperSubjectInfo }) => {
    const icon = SUBJECT_ICONS[item.subject] || SUBJECT_ICONS.default;
    const color = SUBJECT_COLORS[item.subject] || SUBJECT_COLORS.default;

    return (
      <TouchableOpacity
        style={[styles.subjectCard, { borderLeftColor: color }]}
        onPress={() => handleSubjectPress(item.subject)}
      >
        <View style={[styles.iconContainer, { backgroundColor: color + '20' }]}>
          <Ionicons name={icon as any} size={24} color={color} />
        </View>
        <View style={styles.subjectInfo}>
          <Text style={styles.subjectName}>{item.subject}</Text>
          <Text style={styles.paperCount}>{item.paperCount} paper(s)</Text>
          {item.academicYears.length > 0 && (
            <Text style={styles.yearsText}>
              {item.academicYears.join(', ')}
            </Text>
          )}
        </View>
        <Ionicons name="chevron-forward" size={20} color="#9CA3AF" />
      </TouchableOpacity>
    );
  };

  const renderPaperItem = ({ item }: { item: QuestionPaper }) => (
    <TouchableOpacity
      style={styles.paperCard}
      onPress={() => handleDownload(item)}
    >
      <Ionicons name="document-text-outline" size={24} color="#8B5CF6" />
      <View style={styles.paperInfo}>
        <Text style={styles.paperName} numberOfLines={2}>
          {item.fileName}
        </Text>
        <View style={styles.paperMeta}>
          {item.academicYear && (
            <Text style={styles.yearBadge}>{item.academicYear}</Text>
          )}
          <Text style={styles.uploadDate}>
            {new Date(item.uploadedAt).toLocaleDateString()}
          </Text>
        </View>
      </View>
      <Ionicons name="download-outline" size={24} color="#10B981" />
    </TouchableOpacity>
  );

  if (loading) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator size="large" color="#8B5CF6" />
        <Text style={styles.loadingText}>Loading question papers...</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <Text style={styles.title}>Model Question Papers</Text>
        <Text style={styles.subtitle}>
          Karnataka State Board | {grade} | 2024-25
        </Text>
      </View>

      {/* Year Filter */}
      {!selectedSubject && availableYears.length > 0 && (
        <View style={styles.filterContainer}>
          <Text style={styles.filterLabel}>Academic Year:</Text>
          <View style={styles.pickerContainer}>
            <Picker
              selectedValue={selectedYear}
              onValueChange={(value) => setSelectedYear(value)}
              style={styles.picker}
            >
              <Picker.Item label="All Years" value="" />
              {availableYears.map((year) => (
                <Picker.Item key={year} label={year} value={year} />
              ))}
            </Picker>
          </View>
        </View>
      )}

      {/* Subject List or Papers List */}
      {selectedSubject ? (
        <View style={styles.content}>
          <TouchableOpacity
            style={styles.backButton}
            onPress={() => {
              setSelectedSubject(null);
              setPapers([]);
            }}
          >
            <Ionicons name="arrow-back" size={20} color="#8B5CF6" />
            <Text style={styles.backText}>Back to Subjects</Text>
          </TouchableOpacity>

          <Text style={styles.sectionTitle}>{selectedSubject} Papers</Text>

          {loadingPapers ? (
            <ActivityIndicator size="small" color="#8B5CF6" />
          ) : (
            <FlatList
              data={papers}
              keyExtractor={(item) => item.id.toString()}
              renderItem={renderPaperItem}
              contentContainerStyle={styles.listContainer}
              ListEmptyComponent={
                <Text style={styles.emptyText}>No papers available</Text>
              }
            />
          )}
        </View>
      ) : (
        <FlatList
          data={subjects}
          keyExtractor={(item) => item.subject}
          renderItem={renderSubjectCard}
          contentContainerStyle={styles.listContainer}
          ListEmptyComponent={
            <Text style={styles.emptyText}>No subjects available</Text>
          }
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F9FAFB' },
  centered: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  loadingText: { marginTop: 12, color: '#6B7280' },
  header: { backgroundColor: '#8B5CF6', padding: 20, paddingTop: 50 },
  title: { fontSize: 24, fontWeight: 'bold', color: '#FFFFFF' },
  subtitle: { fontSize: 14, color: '#DDD6FE', marginTop: 4 },
  filterContainer: { flexDirection: 'row', alignItems: 'center', padding: 16, backgroundColor: '#FFF' },
  filterLabel: { fontSize: 14, fontWeight: '500', color: '#374151' },
  pickerContainer: { flex: 1, marginLeft: 12, borderWidth: 1, borderColor: '#E5E7EB', borderRadius: 8 },
  picker: { height: 40 },
  content: { flex: 1 },
  backButton: { flexDirection: 'row', alignItems: 'center', padding: 16 },
  backText: { marginLeft: 8, color: '#8B5CF6', fontWeight: '500' },
  sectionTitle: { fontSize: 18, fontWeight: '600', color: '#111827', paddingHorizontal: 16, marginBottom: 12 },
  listContainer: { padding: 16 },
  subjectCard: { flexDirection: 'row', alignItems: 'center', backgroundColor: '#FFF', borderRadius: 12, padding: 16, marginBottom: 12, borderLeftWidth: 4, elevation: 2 },
  iconContainer: { width: 48, height: 48, borderRadius: 12, justifyContent: 'center', alignItems: 'center' },
  subjectInfo: { flex: 1, marginLeft: 12 },
  subjectName: { fontSize: 16, fontWeight: '600', color: '#111827' },
  paperCount: { fontSize: 12, color: '#6B7280', marginTop: 2 },
  yearsText: { fontSize: 11, color: '#9CA3AF', marginTop: 2 },
  paperCard: { flexDirection: 'row', alignItems: 'center', backgroundColor: '#FFF', borderRadius: 12, padding: 16, marginBottom: 12, elevation: 2 },
  paperInfo: { flex: 1, marginLeft: 12 },
  paperName: { fontSize: 14, fontWeight: '500', color: '#111827' },
  paperMeta: { flexDirection: 'row', alignItems: 'center', marginTop: 4 },
  yearBadge: { fontSize: 11, color: '#8B5CF6', backgroundColor: '#EDE9FE', paddingHorizontal: 8, paddingVertical: 2, borderRadius: 4, marginRight: 8 },
  uploadDate: { fontSize: 12, color: '#9CA3AF' },
  emptyText: { textAlign: 'center', color: '#9CA3AF', marginTop: 40 },
});
```

---

## Error Handling

All endpoints return consistent error responses:

```json
{
  "status": "error",
  "message": "Error description here"
}
```

**HTTP Status Codes:**
| Code | Description |
|------|-------------|
| 200 | Success |
| 404 | Resource not found |
| 500 | Server error |

---

## Testing the API

### Using cURL

```bash
# ===== SYLLABUS ENDPOINTS =====
# Get all syllabi for 2nd PUC
curl "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net/api/file/syllabus?grade=2nd%20PUC"

# Get subjects for 2nd PUC
curl "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net/api/file/syllabus/subjects?grade=2nd%20PUC"

# Get Mathematics syllabus files
curl "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net/api/file/syllabus/Mathematics"

# ===== QUESTION PAPERS ENDPOINTS =====
# Get all question papers
curl "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net/api/file/question-papers"

# Get question papers for 2nd PUC
curl "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net/api/file/question-papers?grade=2nd%20PUC"

# Get question papers by subject
curl "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net/api/file/question-papers/Mathematics?grade=2nd%20PUC"

# Get question papers by grade (grouped by subject)
curl "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net/api/file/question-papers/grade/2nd%20PUC"

# Get available grades
curl "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net/api/file/question-papers/grades"

# Get available subjects
curl "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net/api/file/question-papers/subjects?grade=2nd%20PUC"

# Get available academic years
curl "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net/api/file/question-papers/years"

# Get download URL
curl "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net/api/file/question-papers/download/101"
```

### Using PowerShell

```powershell
# Get all question papers
Invoke-RestMethod -Uri "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net/api/file/question-papers" | ConvertTo-Json -Depth 5

# Get question papers for 2nd PUC Mathematics
Invoke-RestMethod -Uri "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net/api/file/question-papers/Mathematics?grade=2nd%20PUC"

# Get available subjects with papers
Invoke-RestMethod -Uri "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net/api/file/question-papers/subjects?grade=2nd%20PUC"
```

---

## Database Setup

Before using the Question Papers API, run the migration script:

```sql
-- Run this on Azure SQL Database
-- File: add-question-paper-fields.sql
```

This adds the following columns to `UploadedFiles` table:
- `FileType` (NVARCHAR(50)) - "Syllabus" or "ModelQuestionPaper"
- `Chapter` (NVARCHAR(200)) - Optional chapter/unit name
- `AcademicYear` (NVARCHAR(20)) - e.g., "2024-25"

---

## File Types

When uploading files, set the `FileType` field:
| FileType | Description |
|----------|-------------|
| `Syllabus` | Syllabus/textbook content (default) |
| `ModelQuestionPaper` | Model question papers/previous years |
| `Notes` | Study notes and materials |

---

## Notes

1. **File Downloads**: The `blobUrl` returned is a direct link to Azure Blob Storage.

2. **Caching**: Consider implementing client-side caching for subjects list.

3. **Offline Support**: Download files using `expo-file-system` for offline access.

4. **Grade Selection**: Make grade/year dynamic based on user profile.

5. **Academic Years**: Filter by year to show only relevant papers.

---

## Contact

For API issues or questions, contact the backend team.

**Last Updated:** December 14, 2025
