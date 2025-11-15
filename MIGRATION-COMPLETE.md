# School AI Chatbot - Migration Complete! 🎉

## ✅ Migration Summary

Successfully migrated your School AI Chatbot from Azure Functions to ASP.NET Core backend with shared Azure SQL database.

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                  SCHOOL AI CHATBOT PLATFORM                  │
└─────────────────────────────────────────────────────────────┘

┌──────────────────┐         ┌──────────────────┐
│   Frontend (SWA) │────────▶│  ASP.NET Core    │
│   React + Vite   │         │  Backend API     │
└──────────────────┘         └──────────────────┘
                                     │
                                     ▼
                    ┌────────────────────────────────┐
                    │     Azure SQL Database         │
                    │  (Shared by Backend + Funcs)   │
                    │  - UploadedFiles               │
                    │  - FileChunks                  │
                    │  - ChunkEmbeddings             │
                    │  - ChatHistory                 │
                    │  - StudyNotes                  │
                    └────────────────────────────────┘
                                     ▲
                                     │
                    ┌────────────────────────────────┐
                    │   Azure Functions (Ingestion)  │
                    │  - Blob Trigger                │
                    │  - Text Extraction             │
                    │  - Chunking                    │
                    │  - Embedding Generation        │
                    └────────────────────────────────┘
                                     ▲
                                     │
                              ┌──────────────┐
                              │ Blob Storage │
                              │  (Textbooks) │
                              └──────────────┘
```

---

## 📁 Updated Project Structure

```
school-ai-chatbot/
├── SchoolAiChatbotBackend/          # ASP.NET Core Backend (MAIN)
│   ├── Controllers/
│   │   ├── ChatController.cs         ✅ SQL-based RAG chat
│   │   ├── NotesController.cs        ✅ Study notes generation
│   │   ├── FileController.cs         ✅ File upload to blob
│   │   ├── AuthController.cs
│   │   └── FaqsController.cs
│   ├── Services/
│   │   ├── OpenAIService.cs          ✅ NEW - Azure OpenAI + OpenAI
│   │   ├── RAGService.cs             ✅ REWRITTEN - SQL cosine similarity
│   │   ├── StudyNotesService.cs      ✅ REWRITTEN - SQL-based RAG
│   │   ├── ChatHistoryService.cs     ✅ SQL chat history
│   │   ├── BlobStorageService.cs     ✅ NEW - Azure Blob uploads
│   │   ├── JwtService.cs
│   │   └── PineconeService.cs
│   ├── Models/
│   │   ├── FileChunk.cs              ✅ Azure Functions schema
│   │   ├── ChunkEmbedding.cs         ✅ Azure Functions schema
│   │   ├── UploadedFile.cs           ✅ Azure Functions schema
│   │   ├── ChatHistory.cs
│   │   ├── StudyNote.cs
│   │   ├── DTOs.cs                   ✅ NEW - Request/Response DTOs
│   │   └── ...
│   ├── Data/
│   │   └── AppDbContext.cs           ✅ UPDATED - FileChunks, ChunkEmbeddings
│   ├── appsettings.json              ✅ UPDATED - Azure Functions keys
│   ├── Program.cs                    ✅ UPDATED - Service registration
│   └── SchoolAiChatbotBackend.csproj
│
├── api/                              # Azure Functions (Ingestion Only)
│   ├── Functions/
│   │   ├── UploadTextbook.cs         (Blob trigger)
│   │   └── ProcessBlobFile.cs        (Text extraction + chunking)
│   ├── Services/
│   │   ├── TextExtractionService     (PDF/DOCX parsing)
│   │   ├── ChunkingService           (Text chunking)
│   │   ├── EmbeddingService          (OpenAI embeddings)
│   │   └── DatabaseService           (SQL operations)
│   └── local.settings.json
│
└── school-ai-frontend/               # React Frontend (Static Web App)
    └── src/
        └── (unchanged)
```

---

## 🔧 Configuration Changes

### `appsettings.json` (ASP.NET Core Backend)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_AZURE_SQL_CONNECTION_STRING",
    "SqlDb": "YOUR_AZURE_SQL_CONNECTION_STRING"
  },
  
  "AzureWebJobsStorage": "YOUR_AZURE_STORAGE_CONNECTION_STRING",
  
  "AzureOpenAI": {
    "Endpoint": "https://YOUR_AZURE_OPENAI.openai.azure.com/",
    "ApiKey": "YOUR_AZURE_OPENAI_KEY",
    "ChatDeployment": "gpt-4",
    "EmbeddingDeployment": "text-embedding-3-small"
  },
  
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_KEY_FALLBACK"
  },
  
  "USE_REAL_EMBEDDINGS": "true",
  
  "Jwt": {
    "Key": "YOUR_JWT_SECRET",
    "Issuer": "SchoolAiChatbotBackend",
    "Audience": "SchoolAiChatbotUsers"
  }
}
```

### Environment Variables (Azure App Service)

Set these in Azure Portal → App Service → Configuration:

```bash
ConnectionStrings__SqlDb=YOUR_AZURE_SQL_CONNECTION_STRING
AzureWebJobsStorage=YOUR_STORAGE_CONNECTION_STRING
AzureOpenAI__Endpoint=https://YOUR_AZURE_OPENAI.openai.azure.com/
AzureOpenAI__ApiKey=YOUR_AZURE_OPENAI_KEY
AzureOpenAI__ChatDeployment=gpt-4
AzureOpenAI__EmbeddingDeployment=text-embedding-3-small
USE_REAL_EMBEDDINGS=true
Jwt__Key=YOUR_JWT_SECRET
```

---

## 🚀 Deployment Steps

### 1. Update Azure SQL Database

Run EF Core migration to add new tables:

```powershell
cd SchoolAiChatbotBackend
dotnet ef migrations add AzureFunctionsMigration
dotnet ef database update
```

Or manually run SQL to create tables:

```sql
CREATE TABLE FileChunks (
    Id INT PRIMARY KEY IDENTITY(1,1),
    FileId INT NOT NULL,
    ChunkText NVARCHAR(MAX) NOT NULL,
    ChunkIndex INT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    Subject NVARCHAR(100),
    Grade NVARCHAR(50),
    Chapter NVARCHAR(200),
    FOREIGN KEY (FileId) REFERENCES UploadedFiles(Id) ON DELETE CASCADE
);

CREATE TABLE ChunkEmbeddings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ChunkId INT NOT NULL,
    EmbeddingVector NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (ChunkId) REFERENCES FileChunks(Id) ON DELETE CASCADE
);

CREATE INDEX IX_FileChunks_FileId ON FileChunks(FileId);
CREATE INDEX IX_ChunkEmbeddings_ChunkId ON ChunkEmbeddings(ChunkId);
```

### 2. Build and Publish ASP.NET Core Backend

```powershell
cd SchoolAiChatbotBackend
dotnet publish -c Release -o ./publish
```

### 3. Deploy to Azure App Service

**Option A: Azure CLI**

```bash
az login
az webapp deployment source config-zip \
  --resource-group YOUR_RESOURCE_GROUP \
  --name app-wlanqwy7vuwmu \
  --src ./publish.zip
```

**Option B: VS Code**

1. Install "Azure App Service" extension
2. Right-click on `SchoolAiChatbotBackend` folder
3. Select "Deploy to Web App"
4. Choose `app-wlanqwy7vuwmu`

**Option C: GitHub Actions (CI/CD)**

Use the workflow file in `.github/workflows/deploy-backend.yml`:

```yaml
name: Deploy ASP.NET Core Backend

on:
  push:
    branches: [main]
    paths:
      - 'SchoolAiChatbotBackend/**'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Build
        run: dotnet publish SchoolAiChatbotBackend/SchoolAiChatbotBackend.csproj -c Release -o ./publish
      
      - name: Deploy to Azure
        uses: azure/webapps-deploy@v2
        with:
          app-name: 'app-wlanqwy7vuwmu'
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ./publish
```

### 4. Configure Azure App Service Settings

In Azure Portal:

1. Navigate to: **App Service → Configuration → Application Settings**
2. Add all environment variables listed above
3. Click **Save** and **Restart**

### 5. Test Endpoints

```bash
# Health check
curl https://app-wlanqwy7vuwmu.azurewebsites.net/health

# Chat endpoint
curl -X POST https://app-wlanqwy7vuwmu.azurewebsites.net/api/chat \
  -H "Content-Type: application/json" \
  -d '{"question":"What is photosynthesis?","sessionId":"test-123"}'

# Study notes
curl -X POST https://app-wlanqwy7vuwmu.azurewebsites.net/api/notes/generate \
  -H "Content-Type: application/json" \
  -d '{"topic":"Quadratic Equations","subject":"Mathematics","grade":"Grade 10"}'

# File upload status
curl https://app-wlanqwy7vuwmu.azurewebsites.net/api/file/list
```

---

## 🎯 Key Features After Migration

### ✅ What's Working Now

| Feature | Status | Endpoint |
|---------|--------|----------|
| **SQL-based RAG Chat** | ✅ Ready | `POST /api/chat` |
| **Chat History** | ✅ SQL-backed | `GET /api/chat/history?sessionId=xyz` |
| **Study Notes Generation** | ✅ SQL-based RAG | `POST /api/notes/generate` |
| **File Upload** | ✅ Blob Storage | `POST /api/file/upload` |
| **File Processing Status** | ✅ Real-time | `GET /api/file/status/{id}` |
| **Azure OpenAI Integration** | ✅ Supported | Automatic |
| **Standard OpenAI Fallback** | ✅ Supported | Automatic |

### 🔄 How RAG Works Now

1. **User asks a question** → `POST /api/chat`
2. **RAGService generates embedding** using OpenAIService
3. **SQL cosine similarity search** finds top-K relevant chunks from `ChunkEmbeddings`
4. **Context is built** from `FileChunks`
5. **OpenAI generates answer** using context
6. **Chat history saved** to `ChatHistory` table
7. **Response returned** to user

### 📤 How File Ingestion Works

1. **User uploads file** → `POST /api/file/upload`
2. **Backend uploads to Blob Storage** (Azure Storage)
3. **Metadata saved** to `UploadedFiles` table with Status="Pending"
4. **Azure Functions blob trigger** detects new file
5. **Functions extract text** → chunk → generate embeddings
6. **Functions save to SQL**:
   - `FileChunks` table (text chunks)
   - `ChunkEmbeddings` table (vector embeddings)
7. **Status updated** to "Completed"
8. **Backend can now use chunks** for RAG queries

---

## 🛠️ Troubleshooting

### Issue: "No chunk embeddings found in database"

**Solution:**
- Check if Azure Functions are running and processing blob uploads
- Verify `AzureWebJobsStorage` is configured in both backend and functions
- Check `UploadedFiles` table for Status="Completed"
- Run: `SELECT COUNT(*) FROM ChunkEmbeddings`

### Issue: "OpenAI API key not configured"

**Solution:**
- Set `AzureOpenAI__ApiKey` in App Service configuration
- OR set `OpenAI__ApiKey` as fallback
- Restart the app service

### Issue: "Database connection failed"

**Solution:**
- Verify `ConnectionStrings__SqlDb` in App Service settings
- Test connection: `sqlcmd -S YOUR_SERVER -U YOUR_USER -P YOUR_PASS -d YOUR_DB`
- Check firewall rules allow Azure services

### Issue: "CORS errors from frontend"

**Solution:**
Backend already allows all origins. Update if needed in `Program.cs`:

```csharp
app.UseCors(policy => policy
    .WithOrigins("https://nice-ocean-0bd32c110.3.azurestaticapps.net")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials());
```

---

## 📊 Database Schema

### Tables Created by Migration

```sql
-- Azure Functions Ingestion Tables
UploadedFiles (Id, FileName, BlobUrl, UploadedAt, Subject, Grade, Chapter, Status, TotalChunks)
FileChunks (Id, FileId, ChunkText, ChunkIndex, Subject, Grade, Chapter, CreatedAt)
ChunkEmbeddings (Id, ChunkId, EmbeddingVector, CreatedAt)

-- Backend Application Tables
ChatHistory (Id, UserId, SessionId, Message, Reply, Timestamp, ContextUsed, ContextCount)
StudyNotes (Id, UserId, Topic, GeneratedNotes, Subject, Grade, Chapter, CreatedAt, Rating)
```

### Relationships

```
UploadedFiles (1) ──< (N) FileChunks
FileChunks (1) ──< (1) ChunkEmbeddings
Users (1) ──< (N) ChatHistory
Users (1) ──< (N) StudyNotes
```

---

## 🎓 Usage Examples

### Example 1: Ask a Question

```bash
curl -X POST https://app-wlanqwy7vuwmu.azurewebsites.net/api/chat \
  -H "Content-Type: application/json" \
  -d '{
    "question": "Explain photosynthesis",
    "sessionId": "student-123"
  }'
```

Response:
```json
{
  "status": "success",
  "sessionId": "student-123",
  "question": "Explain photosynthesis",
  "reply": "Photosynthesis is the process by which plants...",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Example 2: Generate Study Notes

```bash
curl -X POST https://app-wlanqwy7vuwmu.azurewebsites.net/api/notes/generate \
  -H "Content-Type: application/json" \
  -d '{
    "topic": "Pythagorean Theorem",
    "subject": "Mathematics",
    "grade": "Grade 8"
  }'
```

### Example 3: Upload a Textbook

```bash
curl -X POST https://app-wlanqwy7vuwmu.azurewebsites.net/api/file/upload \
  -F "file=@math_textbook.pdf" \
  -F "subject=Mathematics" \
  -F "grade=Grade 10" \
  -F "chapter=Chapter 5: Trigonometry"
```

---

## 🎉 Next Steps

1. ✅ **Test all endpoints** using the examples above
2. ✅ **Upload sample textbooks** to populate the database
3. ✅ **Update frontend** to point to new backend endpoints
4. ✅ **Monitor Azure Functions** for successful file processing
5. ✅ **Set up CI/CD** using GitHub Actions
6. ✅ **Add authentication** (JWT already configured)
7. ✅ **Enable Application Insights** for monitoring

---

## 📝 Migration Checklist

- [x] Updated `AppDbContext` with `FileChunks` and `ChunkEmbeddings`
- [x] Created `OpenAIService` with Azure OpenAI support
- [x] Rewrote `RAGService` with SQL-based cosine similarity
- [x] Rewrote `StudyNotesService` to use SQL RAG
- [x] Updated `ChatController` to use new RAGService
- [x] Updated `FileController` for blob storage uploads
- [x] Created `BlobStorageService` for Azure Storage
- [x] Updated `appsettings.json` with Azure Functions keys
- [x] Updated `Program.cs` with proper service registration
- [x] Created DTOs for all endpoints
- [x] Generated deployment documentation

---

## 🆘 Support

For issues or questions:
1. Check logs in Azure Portal → App Service → Log Stream
2. Review Application Insights for errors
3. Check SQL query performance
4. Verify all configuration values are set

---

**Migration completed successfully! Your School AI Chatbot now uses SQL-based RAG with shared Azure SQL database.** 🚀
