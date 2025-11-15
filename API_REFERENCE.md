# API Endpoints Reference - Post Migration

## Chat Endpoints

### POST /api/chat
Ask a question with RAG context and conversation continuity.

**Request:**
```json
{
  "question": "What is photosynthesis?",
  "sessionId": "optional-session-id"  // If not provided, creates new session
}
```

**Response:**
```json
{
  "status": "success",
  "sessionId": "generated-or-provided-session-id",
  "question": "What is photosynthesis?",
  "reply": "Photosynthesis is the process by which plants...",
  "contextCount": 5,
  "usedChunks": [
    { "subject": "Biology", "grade": "10", "chapter": "Life Processes" }
  ]
}
```

### GET /api/chat/history
Get chat history for a specific session.

**Query Parameters:**
- `sessionId` (required): Session ID to retrieve
- `limit` (optional, default: 10): Number of messages to return

**Response:**
```json
{
  "status": "success",
  "sessionId": "abc-123",
  "count": 3,
  "messages": [
    {
      "id": 1,
      "message": "What is photosynthesis?",
      "reply": "Photosynthesis is...",
      "timestamp": "2025-01-08T12:00:00Z",
      "contextCount": 5
    }
  ]
}
```

### GET /api/chat/sessions
Get all chat sessions for the current user.

**Query Parameters:**
- `limit` (optional, default: 20): Number of sessions to return

**Response:**
```json
{
  "status": "success",
  "count": 5,
  "sessions": ["session-1", "session-2", "session-3"]
}
```

---

## Study Notes Endpoints

### POST /api/notes/generate
Generate AI-powered study notes for a topic.

**Request:**
```json
{
  "topic": "Photosynthesis",
  "subject": "Biology",      // Optional
  "grade": "10",            // Optional
  "chapter": "Life Processes"  // Optional
}
```

**Response:**
```json
{
  "status": "success",
  "noteId": 42,
  "topic": "Photosynthesis",
  "notes": "## Photosynthesis\n\n### Key Concepts\n- **Chlorophyll** absorbs...",
  "subject": "Biology",
  "grade": "10",
  "chapter": "Life Processes",
  "createdAt": "2025-01-08T12:00:00Z"
}
```

### GET /api/notes
Get user's study notes history.

**Query Parameters:**
- `limit` (optional, default: 20): Number of notes to return

**Response:**
```json
{
  "status": "success",
  "count": 3,
  "notes": [
    {
      "id": 42,
      "topic": "Photosynthesis",
      "subject": "Biology",
      "grade": "10",
      "chapter": "Life Processes",
      "createdAt": "2025-01-08T12:00:00Z",
      "rating": 5,
      "preview": "## Photosynthesis\n\n### Key Concepts..."
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
    "topic": "Photosynthesis",
    "notes": "## Photosynthesis\n\n...(full markdown content)",
    "subject": "Biology",
    "grade": "10",
    "chapter": "Life Processes",
    "createdAt": "2025-01-08T12:00:00Z",
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

## File Upload Endpoint

### POST /api/file/upload
Upload a PDF or text file for ingestion.

**Request (multipart/form-data):**
- `file`: File to upload
- `Class`: Grade/class (e.g., "10")
- `subject`: Subject name (e.g., "Biology")
- `chapter`: Chapter name (e.g., "Life Processes")

**Example (curl):**
```bash
curl -X POST "https://studyai-ingestion-345.azurewebsites.net/api/file/upload?code=KEY" \
  -F "file=@biology_chapter1.pdf" \
  -F "Class=10" \
  -F "subject=Biology" \
  -F "chapter=Life Processes"
```

**Response:**
```json
{
  "status": "success",
  "message": "File uploaded and processed successfully",
  "fileName": "biology_chapter1.pdf",
  "chunksProcessed": 45,
  "pineconeUploaded": true
}
```

---

## Authentication

All endpoints require the Azure Function Key:

**Query Parameter:**
```
?code=lm8CB_r6ty6AE7agTnD1LJ5Em0b6Yoitc_95UzXDKLziAzFuzGRupw==
```

**Example:**
```
POST https://studyai-ingestion-345.azurewebsites.net/api/chat?code=KEY
```

---

## Error Responses

All endpoints return errors in this format:

```json
{
  "status": "error",
  "message": "Human-readable error message",
  "debug": "Technical error details (in development only)"
}
```

**Common HTTP Status Codes:**
- `200 OK` - Success
- `400 Bad Request` - Invalid input
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

---

## Base URL

**Development:** `http://localhost:8080/api`  
**Production:** `https://studyai-ingestion-345.azurewebsites.net/api`

---

## Testing

### Test Chat
```bash
curl -X POST "http://localhost:8080/api/chat" \
  -H "Content-Type: application/json" \
  -d '{"question": "What is gravity?", "sessionId": "test-123"}'
```

### Test Study Notes
```bash
curl -X POST "http://localhost:8080/api/notes/generate" \
  -H "Content-Type: application/json" \
  -d '{"topic": "Newton Laws", "subject": "Physics", "grade": "9"}'
```

### Test File Upload
```bash
curl -X POST "http://localhost:8080/api/file/upload" \
  -F "file=@test.pdf" \
  -F "Class=10" \
  -F "subject=Science" \
  -F "chapter=Chapter1"
```
