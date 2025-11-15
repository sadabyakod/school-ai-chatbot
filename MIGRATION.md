# Azure Functions to ASP.NET Core Migration Guide

## Overview
This document describes the migration of features from the Azure Functions project (`C:\SmartStudyFunc`) to the consolidated ASP.NET Core backend (`SchoolAiChatbotBackend`).

## Migration Date
**Completed:** [Current Date]

## Migrated Features

### 1. **SearchRagQuery** → ChatController with RAGService
- **What it was:** Azure Function for RAG-powered question answering
- **What it became:** 
  - `ChatController` - `/api/chat` endpoint with database-backed chat history
  - `RAGService` - Consolidated RAG pipeline (embedding → vector search → context retrieval)
  - `ChatHistoryService` - SQL-backed conversation history

**Key Changes:**
- ✅ Replaced in-memory `ConversationMemory` with SQL database storage
- ✅ Added `sessionId` parameter for conversation continuity
- ✅ Created `ChatHistory` table with indexes for performance
- ✅ Moved RAG logic from controller into dedicated service

**New Endpoints:**
```
POST   /api/chat                    # Ask a question with RAG context
GET    /api/chat/history            # Get chat history for a session
GET    /api/chat/sessions           # Get all user sessions
```

**Frontend Changes Required:**
```typescript
// Add sessionId to chat requests
const response = await fetch(buildApiUrl('/chat'), {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    question: userQuestion,
    sessionId: currentSessionId  // Add this field
  })
});
```

---

### 2. **GenerateStudyNotes** → NotesController with StudyNotesService
- **What it was:** Azure Function to generate AI-powered study notes
- **What it became:**
  - `NotesController` - `/api/notes` endpoints
  - `StudyNotesService` - AI-powered notes generation using RAG
  - `StudyNote` table - Persistent storage with ratings

**Key Changes:**
- ✅ Uses `RAGService` to retrieve relevant syllabus content
- ✅ Generates comprehensive markdown-formatted study notes
- ✅ Saves notes to database with source chunk tracking
- ✅ Added rating system (1-5 stars) for user feedback

**New Endpoints:**
```
POST   /api/notes/generate          # Generate study notes for a topic
GET    /api/notes                   # Get user's study notes history
GET    /api/notes/{id}              # Get specific study note by ID
POST   /api/notes/{id}/rate         # Rate a study note
```

**Example Request:**
```json
POST /api/notes/generate
{
  "topic": "Photosynthesis",
  "subject": "Biology",
  "grade": "10",
  "chapter": "Life Processes"
}
```

**Example Response:**
```json
{
  "status": "success",
  "noteId": 42,
  "topic": "Photosynthesis",
  "notes": "## Photosynthesis\n\n### Key Concepts\n- **Photosynthesis** is...",
  "subject": "Biology",
  "grade": "10",
  "chapter": "Life Processes",
  "createdAt": "2025-01-08T12:00:00Z"
}
```

---

### 3. **Chat History SQL** → ChatHistoryService
- **What it was:** Azure Functions SQL-backed chat storage
- **What it became:** `ChatHistoryService` with EF Core integration

**Database Schema:**
```sql
CREATE TABLE ChatHistories (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId NVARCHAR(255) NOT NULL,
    SessionId NVARCHAR(255) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    Reply NVARCHAR(MAX) NOT NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ContextUsed NVARCHAR(MAX),  -- JSON of syllabus chunks used
    ContextCount INT NOT NULL,
    AuthenticatedUserId INT NULL,
    
    INDEX IX_ChatHistory_UserSession (UserId, SessionId, Timestamp)
);
```

**Benefits:**
- ✅ Persistent conversation history across sessions
- ✅ Efficient querying with composite indexes
- ✅ Automatic cleanup of old messages (built-in method)
- ✅ Support for anonymous and authenticated users

---

### 4. **UploadTextbook** → FileController (Ingestion-Only)
- **What it was:** Azure Function for PDF upload and processing
- **What it became:** `FileController` focused solely on ingestion pipeline

**Ingestion Pipeline:**
1. Upload PDF/text file
2. Extract text content
3. Chunk into 1024-character segments
4. Generate embeddings using OpenAI
5. Store in Pinecone (vectors) + SQL Database (metadata + chunks)

**Key Changes:**
- ✅ Removed all chat/query logic (moved to ChatController)
- ✅ Dedicated to: Upload → Chunk → Embed → Store
- ✅ Keeps Azure Functions role minimal (if needed)

**Endpoint:**
```
POST   /api/file/upload             # Upload PDF/textbook
```

---

### 5. **EmbeddingService** → RAGService
- **What it was:** Azure Functions service for embeddings and chunking
- **What it became:** Consolidated into `RAGService`

**RAGService Methods:**
```csharp
Task<List<float>> GetEmbeddingAsync(string text);
Task<List<SyllabusChunk>> SearchSimilarContentAsync(string query, int topK = 5);
Task<string> BuildContextTextAsync(List<SyllabusChunk> chunks);
```

**Benefits:**
- ✅ Centralized RAG logic (used by Chat, Notes, and File controllers)
- ✅ Easier testing and maintenance
- ✅ Consistent embedding generation across all features

---

## Architecture Changes

### Before Migration
```
Azure Functions (C:\SmartStudyFunc)
├── SearchRagQuery (Chat + RAG)
├── GenerateStudyNotes
├── UploadTextbook
├── Chat History SQL
└── EmbeddingService

ASP.NET Core Backend
├── Simple chat (in-memory)
└── File upload
```

### After Migration
```
ASP.NET Core Backend (Consolidated)
├── Controllers/
│   ├── ChatController → RAG + Chat History
│   ├── NotesController → Study Notes Generation
│   └── FileController → PDF Ingestion Only
├── Services/
│   ├── RAGService → Embedding + Vector Search
│   ├── ChatHistoryService → SQL Chat Storage
│   ├── StudyNotesService → AI Notes Generation
│   ├── OpenAiChatService → OpenAI Integration
│   └── PineconeService → Vector DB
└── Models/
    ├── ChatHistory
    ├── StudyNote
    └── SyllabusChunk

Azure Functions (Optional - Minimal)
└── UploadTextbook (if serverless ingestion needed)
```

---

## Database Migrations

### Run EF Core Migrations

```powershell
# Navigate to backend project
cd c:\school-ai-chatbot\SchoolAiChatbotBackend

# Create migration for new tables
dotnet ef migrations add AddChatHistoryAndStudyNotes

# Apply migration to database
dotnet ef database update
```

### New Tables Created
1. **ChatHistories** - Chat conversation history
2. **StudyNotes** - Generated study notes

---

## Deployment Steps

### 1. Update Backend (ASP.NET Core)

```powershell
# Build and test locally
cd c:\school-ai-chatbot\SchoolAiChatbotBackend
dotnet build
dotnet run

# Test new endpoints
curl http://localhost:8080/api/chat/test
curl http://localhost:8080/api/notes/test
```

### 2. Apply Database Migrations

```powershell
# Update production database
dotnet ef database update --connection "YOUR_PRODUCTION_CONNECTION_STRING"
```

### 3. Deploy to Azure App Service

```powershell
# Publish to Azure
az webapp up --name studyai-ingestion-345 --resource-group <your-rg>

# Or use GitHub Actions (already configured)
git add .
git commit -m "Migrated Azure Functions features to ASP.NET Core"
git push origin main
```

### 4. Update Frontend Environment Variables

```bash
# school-ai-frontend/.env.production
VITE_API_URL=https://studyai-ingestion-345.azurewebsites.net/api
VITE_AZURE_FUNCTION_KEY=lm8CB_r6ty6AE7agTnD1LJ5Em0b6Yoitc_95UzXDKLziAzFuzGRupw==
```

### 5. Test All Endpoints

```bash
# Test chat with session
curl -X POST https://studyai-ingestion-345.azurewebsites.net/api/chat?code=KEY \
  -H "Content-Type: application/json" \
  -d '{"question": "What is photosynthesis?", "sessionId": "test-session-1"}'

# Test study notes generation
curl -X POST https://studyai-ingestion-345.azurewebsites.net/api/notes/generate?code=KEY \
  -H "Content-Type: application/json" \
  -d '{"topic": "Gravity", "subject": "Physics", "grade": "9"}'

# Test file upload
curl -X POST https://studyai-ingestion-345.azurewebsites.net/api/file/upload?code=KEY \
  -F "file=@textbook.pdf" \
  -F "Class=10" \
  -F "subject=Science" \
  -F "chapter=Chapter1"
```

---

## Retiring Azure Functions (`C:\SmartStudyFunc`)

### Option 1: Complete Removal
If all features are successfully migrated and tested:

1. **Stop Azure Functions deployment**
   ```powershell
   az functionapp stop --name <your-function-app> --resource-group <your-rg>
   ```

2. **Archive the project**
   ```powershell
   # Create backup
   Compress-Archive -Path C:\SmartStudyFunc -DestinationPath C:\Backups\SmartStudyFunc-Archived.zip
   
   # Optionally delete local folder
   Remove-Item -Path C:\SmartStudyFunc -Recurse -Force
   ```

3. **Delete Azure resources** (if no longer needed)
   ```powershell
   az functionapp delete --name <your-function-app> --resource-group <your-rg>
   ```

### Option 2: Keep Minimal Azure Functions
If you prefer serverless PDF processing:

1. **Keep only** `UploadTextbook` function
2. **Remove** SearchRagQuery, GenerateStudyNotes, ChatHistory functions
3. **Update** function to call ASP.NET Core backend for embedding/storage

---

## Performance Improvements

### Before
- ❌ In-memory chat history (lost on restart)
- ❌ Duplicate RAG logic across functions
- ❌ No conversation continuity
- ❌ No study notes history

### After
- ✅ SQL-backed chat history with indexes
- ✅ Centralized RAGService (DRY principle)
- ✅ Session-based conversation continuity
- ✅ Persistent study notes with ratings
- ✅ Better error handling and logging

---

## New Frontend Integration

### ChatBot.tsx - Add Session Support

```typescript
// Track current session
const [sessionId, setSessionId] = useState<string | null>(null);

const sendMessage = async (message: string) => {
  const response = await fetch(buildApiUrl('/chat'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      question: message,
      sessionId: sessionId  // Include session ID
    })
  });
  
  const data = await response.json();
  setSessionId(data.sessionId);  // Save session ID from response
};
```

### New StudyNotes Component

```typescript
// school-ai-frontend/src/StudyNotes.tsx
const generateNotes = async (topic: string, subject?: string) => {
  const response = await fetch(buildApiUrl('/notes/generate'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ topic, subject })
  });
  
  const data = await response.json();
  return data.notes;  // Markdown-formatted notes
};
```

---

## Rollback Plan

If issues arise during migration:

1. **Keep Azure Functions running** until backend is stable
2. **Frontend can switch** between endpoints using environment variables
3. **Database migrations** can be rolled back:
   ```powershell
   dotnet ef database update PreviousMigrationName
   ```

---

## Testing Checklist

- [ ] Chat with RAG context works
- [ ] Chat history persists across sessions
- [ ] Session continuity ("explain more") works
- [ ] Study notes generation works
- [ ] Study notes history retrieval works
- [ ] File upload and embedding works
- [ ] All endpoints accessible via Azure Function Key
- [ ] Database migrations applied successfully
- [ ] Frontend updated with sessionId support
- [ ] Performance benchmarked (response times)

---

## Support and Troubleshooting

### Common Issues

**Issue:** Chat history not saving
- **Fix:** Check database connection string in `appsettings.json`
- **Check:** Verify migrations were applied with `dotnet ef migrations list`

**Issue:** RAG returns no context
- **Fix:** Ensure Pinecone service is configured correctly
- **Check:** Verify `Pinecone:Host` and `Pinecone:ApiKey` in configuration

**Issue:** Study notes generation fails
- **Fix:** Check OpenAI API key and quota
- **Check:** Verify RAGService is retrieving chunks

### Logs and Monitoring

```powershell
# View backend logs
az webapp log tail --name studyai-ingestion-345 --resource-group <your-rg>

# Check database logs
# Use Azure Portal → SQL Database → Query Performance Insight
```

---

## Conclusion

The migration consolidates all AI-powered features into the ASP.NET Core backend, providing:
- ✅ Better maintainability (single codebase)
- ✅ Persistent chat history and study notes
- ✅ Improved RAG architecture
- ✅ Easier testing and deployment
- ✅ Cost optimization (fewer Azure Functions)

**Next Steps:**
1. Apply database migrations
2. Deploy updated backend to Azure
3. Update frontend with new endpoints
4. Test all features thoroughly
5. Retire Azure Functions project (optional)

---

**Questions?** Contact the development team or refer to the [README.md](../README.md) for more details.
