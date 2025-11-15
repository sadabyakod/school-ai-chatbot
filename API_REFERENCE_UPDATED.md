# School AI Chatbot - API Reference (Updated)

## Base URL
- **Production**: `https://app-wlanqwy7vuwmu.azurewebsites.net`
- **Local**: `http://localhost:8080`

---

## 🤖 Chat Endpoints (SQL-based RAG)

### POST /api/chat
Ask a question using SQL-based RAG retrieval.

**Request:**
```json
{
  "question": "Explain photosynthesis",
  "sessionId": "student-123"
}
```

**Response:**
```json
{
  "status": "success",
  "sessionId": "student-123",
  "question": "Explain photosynthesis",
  "reply": "Photosynthesis is the process...",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### GET /api/chat/history
Get chat history for a session.

**Query Parameters:**
- `sessionId` (required): Session identifier
- `limit` (optional, default=10): Max messages to retrieve

**Response:**
```json
{
  "status": "success",
  "sessionId": "student-123",
  "count": 5,
  "messages": [
    {
      "id": 1,
      "message": "What is photosynthesis?",
      "reply": "Photosynthesis is...",
      "timestamp": "2024-01-15T10:30:00Z",
      "contextCount": 3
    }
  ]
}
```

### GET /api/chat/sessions
Get all chat sessions for a user.

**Query Parameters:**
- `limit` (optional, default=20): Max sessions to retrieve

**Response:**
```json
{
  "status": "success",
  "count": 10,
  "sessions": [
    {
      "sessionId": "student-123",
      "lastMessage": "What is photosynthesis?",
      "lastActivity": "2024-01-15T10:30:00Z",
      "messageCount": 5
    }
  ]
}
```

---

## 📝 Study Notes Endpoints

### POST /api/notes/generate
Generate study notes using SQL-based RAG.

**Request:**
```json
{
  "topic": "Pythagorean Theorem",
  "subject": "Mathematics",
  "grade": "Grade 8",
  "chapter": "Chapter 3: Geometry"
}
```

**Response:**
```json
{
  "status": "success",
  "noteId": 42,
  "topic": "Pythagorean Theorem",
  "notes": "## Pythagorean Theorem\n\n### Definition\n...",
  "subject": "Mathematics",
  "grade": "Grade 8",
  "chapter": "Chapter 3: Geometry",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

### GET /api/notes
Get user's study notes history.

**Query Parameters:**
- `limit` (optional, default=20): Max notes to retrieve

**Response:**
```json
{
  "status": "success",
  "count": 10,
  "notes": [
    {
      "id": 42,
      "topic": "Pythagorean Theorem",
      "subject": "Mathematics",
      "grade": "Grade 8",
      "chapter": "Chapter 3",
      "createdAt": "2024-01-15T10:30:00Z",
      "rating": 5,
      "preview": "## Pythagorean Theorem..."
    }
  ]
}
```

### GET /api/notes/{id}
Get a specific study note by ID.

**Response:**
```json
{
  "status": "success",
  "note": {
    "id": 42,
    "topic": "Pythagorean Theorem",
    "notes": "## Full markdown content...",
    "subject": "Mathematics",
    "grade": "Grade 8",
    "chapter": "Chapter 3",
    "createdAt": "2024-01-15T10:30:00Z",
    "rating": 5
  }
}
```

### POST /api/notes/{id}/rate
Rate a study note (1-5 stars).

**Request:**
```json
{
  "rating": 5
}
```

**Response:**
```json
{
  "status": "success",
  "message": "Rating saved successfully."
}
```

---

## 📤 File Upload Endpoints

### POST /api/file/upload
Upload a file to Azure Blob Storage for processing.

**Request (multipart/form-data):**
- `file`: File to upload (PDF, DOCX, TXT)
- `subject`: Subject name (e.g., "Mathematics")
- `grade`: Grade level (e.g., "Grade 10")
- `chapter`: Chapter name (optional)

**cURL Example:**
```bash
curl -X POST https://app-wlanqwy7vuwmu.azurewebsites.net/api/file/upload \
  -F "file=@textbook.pdf" \
  -F "subject=Mathematics" \
  -F "grade=Grade 10" \
  -F "chapter=Chapter 5: Trigonometry"
```

**Response:**
```json
{
  "status": "success",
  "message": "File uploaded successfully. Processing will begin automatically.",
  "fileId": 15,
  "fileName": "textbook.pdf",
  "blobUrl": "https://storage.blob.core.windows.net/textbooks/abc123_textbook.pdf",
  "uploadedAt": "2024-01-15T10:30:00Z",
  "note": "Azure Functions will process this file and generate embeddings automatically."
}
```

### GET /api/file/status/{fileId}
Check upload and processing status.

**Response:**
```json
{
  "status": "success",
  "fileId": 15,
  "fileName": "textbook.pdf",
  "uploadedAt": "2024-01-15T10:30:00Z",
  "processingStatus": "Completed",
  "chunksCreated": 150,
  "embeddingsCreated": 150,
  "totalChunks": 150,
  "isComplete": true
}
```

**Processing Statuses:**
- `Pending`: Uploaded, waiting for processing
- `Processing`: Azure Functions are extracting and chunking
- `Completed`: All chunks and embeddings created
- `Failed`: Error during processing

### GET /api/file/list
List all uploaded files.

**Query Parameters:**
- `limit` (optional, default=50): Max files to retrieve

**Response:**
```json
{
  "status": "success",
  "count": 25,
  "files": [
    {
      "id": 15,
      "fileName": "math_textbook.pdf",
      "blobUrl": "https://...",
      "uploadedAt": "2024-01-15T10:30:00Z",
      "subject": "Mathematics",
      "grade": "Grade 10",
      "chapter": "Chapter 5",
      "status": "Completed",
      "totalChunks": 150
    }
  ]
}
```

---

## 🔐 Authentication Endpoints (Unchanged)

### POST /api/auth/register
Register a new user.

**Request:**
```json
{
  "email": "student@example.com",
  "password": "SecurePass123!",
  "name": "John Doe"
}
```

### POST /api/auth/login
Login and get JWT token.

**Request:**
```json
{
  "email": "student@example.com",
  "password": "SecurePass123!"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "message": "Login successful"
}
```

---

## ❓ FAQ Endpoints (Unchanged)

### GET /api/faqs
Get all FAQs.

### POST /api/faqs
Create a new FAQ (admin only).

---

## 🏥 Health Check Endpoints

### GET /health
Basic health check.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "database": "configured"
}
```

### GET /api/health
API health check with more details.

### GET /api/ping
Simple ping endpoint.

**Response:**
```json
"pong"
```

---

## 🔧 Configuration Requirements

### Required Environment Variables

```bash
# Database
ConnectionStrings__SqlDb=YOUR_AZURE_SQL_CONNECTION_STRING

# Azure Storage (for file uploads)
AzureWebJobsStorage=YOUR_STORAGE_CONNECTION_STRING

# Azure OpenAI (preferred)
AzureOpenAI__Endpoint=https://YOUR_RESOURCE.openai.azure.com/
AzureOpenAI__ApiKey=YOUR_KEY
AzureOpenAI__ChatDeployment=gpt-4
AzureOpenAI__EmbeddingDeployment=text-embedding-3-small

# OR Standard OpenAI (fallback)
OpenAI__ApiKey=YOUR_OPENAI_KEY

# Features
USE_REAL_EMBEDDINGS=true

# JWT
Jwt__Key=YOUR_SECRET_KEY
Jwt__Issuer=SchoolAiChatbotBackend
Jwt__Audience=SchoolAiChatbotUsers
```

---

## 📊 Data Flow

### Chat Request Flow
```
1. User sends question → POST /api/chat
2. RAGService generates embedding
3. SQL cosine similarity search (ChunkEmbeddings table)
4. Retrieve top-K FileChunks
5. Build context from chunks
6. OpenAIService generates answer
7. Save to ChatHistory table
8. Return answer to user
```

### File Upload Flow
```
1. User uploads file → POST /api/file/upload
2. Backend uploads to Azure Blob Storage
3. Metadata saved to UploadedFiles (Status=Pending)
4. Azure Functions blob trigger detects file
5. Functions extract text → chunk → embed
6. Save to FileChunks and ChunkEmbeddings tables
7. Update Status=Completed
8. Backend can now use chunks for RAG
```

---

## 🧪 Testing Examples

### Test Chat (JavaScript/Fetch)
```javascript
const response = await fetch('https://app-wlanqwy7vuwmu.azurewebsites.net/api/chat', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    question: 'What is photosynthesis?',
    sessionId: 'test-session-1'
  })
});

const data = await response.json();
console.log(data.reply);
```

### Test File Upload (JavaScript)
```javascript
const formData = new FormData();
formData.append('file', fileInput.files[0]);
formData.append('subject', 'Mathematics');
formData.append('grade', 'Grade 10');

const response = await fetch('https://app-wlanqwy7vuwmu.azurewebsites.net/api/file/upload', {
  method: 'POST',
  body: formData
});

const data = await response.json();
console.log('File ID:', data.fileId);
```

---

## 🚨 Error Responses

All endpoints return errors in this format:

```json
{
  "status": "error",
  "message": "Description of what went wrong",
  "details": "Technical error details (dev mode only)"
}
```

**Common HTTP Status Codes:**
- `200 OK`: Success
- `400 Bad Request`: Invalid input
- `401 Unauthorized`: Missing or invalid JWT token
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error

---

## 📚 Postman Collection

Import this collection for easy testing:

```json
{
  "info": {
    "name": "School AI Chatbot API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Chat",
      "request": {
        "method": "POST",
        "url": "{{baseUrl}}/api/chat",
        "body": {
          "mode": "raw",
          "raw": "{\n  \"question\": \"What is photosynthesis?\",\n  \"sessionId\": \"test-123\"\n}"
        }
      }
    }
  ],
  "variable": [
    {
      "key": "baseUrl",
      "value": "https://app-wlanqwy7vuwmu.azurewebsites.net"
    }
  ]
}
```

---

**All endpoints now use SQL-based RAG with shared Azure SQL database!** ✅
