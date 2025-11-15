# Updated System Architecture - After Migration

**Date:** November 15, 2025  
**Status:** ✅ Production Ready

---

## 🏗️ Complete System Architecture

```
┌───────────────────────────────────────────────────────────────────────┐
│                          FRONTEND LAYER                                │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │         React + Vite + TypeScript (school-ai-frontend)           │ │
│  │  ┌────────────┐  ┌─────────────┐  ┌──────────────┐              │ │
│  │  │ Chat UI    │  │ Study Notes │  │ File Upload  │              │ │
│  │  └────────────┘  └─────────────┘  └──────────────┘              │ │
│  │  Deployed: Azure Static Web Apps                                │ │
│  └──────────────────────────────────────────────────────────────────┘ │
└───────────────────────────────────────────────────────────────────────┘
                                  │
                                  │ HTTPS/JSON
                                  ▼
┌───────────────────────────────────────────────────────────────────────┐
│            ASP.NET CORE BACKEND (SchoolAiChatbotBackend)              │
│            Deployed: app-wlanqwy7vuwmu.azurewebsites.net              │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │                     CONTROLLERS                                  │ │
│  │  ┌──────────┐ ┌────────────┐ ┌───────────┐ ┌──────────┐        │ │
│  │  │   Chat   │ │   Notes    │ │   File    │ │   Auth   │        │ │
│  │  │Controller│ │ Controller │ │Controller │ │Controller│        │ │
│  │  └──────────┘ └────────────┘ └───────────┘ └──────────┘        │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │                      SERVICES                                    │ │
│  │  ┌────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │ │
│  │  │  RAGService    │  │ StudyNotes      │  │  OpenAIService  │  │ │
│  │  │ (SQL-based)    │  │   Service       │  │  (Azure/OpenAI) │  │ │
│  │  └────────────────┘  └─────────────────┘  └─────────────────┘  │ │
│  │  ┌────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │ │
│  │  │ ChatHistory    │  │ BlobStorage     │  │  JwtService     │  │ │
│  │  │   Service      │  │   Service       │  │                 │  │ │
│  │  └────────────────┘  └─────────────────┘  └─────────────────┘  │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │                   DATA ACCESS (EF CORE)                          │ │
│  │                      AppDbContext                                │ │
│  └──────────────────────────────────────────────────────────────────┘ │
└───────────────────────────────────────────────────────────────────────┘
        │                       │                          │
        │                       │                          │
        ▼                       ▼                          ▼
┌──────────────┐     ┌─────────────────────┐     ┌────────────────┐
│ Azure OpenAI │     │   Azure SQL DB      │     │  Azure Blob    │
│              │     │    (SHARED)         │     │   Storage      │
│ • GPT-4      │     │ ┌─────────────────┐ │     │                │
│ • Embeddings │     │ │  FileChunks     │ │     │ textbooks/     │
└──────────────┘     │ │  ChunkEmbeddings│ │     └────────────────┘
                     │ │  ChatHistories  │ │              │
                     │ │  StudyNotes     │ │              │
                     │ │  UploadedFiles  │ │              │
                     │ │  Users          │ │              │
                     │ └─────────────────┘ │              │
                     └─────────────────────┘              │
                                 ▲                        │
                                 │                        │
                                 │                  Blob Created
                                 │                  Event Trigger
                                 │                        │
┌────────────────────────────────────────────────────────┴─────────────┐
│              AZURE FUNCTIONS (Ingestion Pipeline)                     │
│              Consumption Plan (Serverless)                            │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │                  ProcessBlobFile Function                        │ │
│  │  ┌────────────────────────────────────────────────────────────┐ │ │
│  │  │  1. Detect new file in blob storage                        │ │ │
│  │  │  2. TextExtractionService → Extract text (PDF/DOCX/TXT)    │ │ │
│  │  │  3. ChunkingService → Split into ~512 token chunks         │ │ │
│  │  │  4. EmbeddingService → Generate 1536-dim embeddings        │ │ │
│  │  │  5. DatabaseService → Save to FileChunks table             │ │ │
│  │  │  6. DatabaseService → Save to ChunkEmbeddings table        │ │ │
│  │  │  7. Update UploadedFiles.Status = "Completed"              │ │ │
│  │  └────────────────────────────────────────────────────────────┘ │ │
│  └──────────────────────────────────────────────────────────────────┘ │
└───────────────────────────────────────────────────────────────────────┘
```

---

## 🔄 RAG Query Flow (SQL-Based)

```
Student Question
    │
    ▼
┌───────────────────────────────────┐
│ POST /api/chat                    │
│ { question, sessionId }           │
└───────────────────────────────────┘
    │
    ▼
┌───────────────────────────────────┐
│ ChatController                    │
└───────────────────────────────────┘
    │
    ▼
┌───────────────────────────────────┐
│ RAGService.GetRAGAnswerAsync()    │
└───────────────────────────────────┘
    │
    ├──► Step 1: Generate Query Embedding
    │    OpenAIService.GetEmbeddingAsync(question)
    │    → [0.123, 0.456, ..., 0.789] (1536 floats)
    │
    ├──► Step 2: SQL Similarity Search
    │    Load all ChunkEmbeddings from database
    │    Calculate: cosine_similarity(query_embedding, chunk_embedding)
    │    Formula: dot(A,B) / (||A|| * ||B||)
    │
    ├──► Step 3: Get Top-K Chunks
    │    Sort by similarity score (descending)
    │    SELECT TOP 5 FileChunks
    │    WHERE ChunkId IN (top similarity scores)
    │
    ├──► Step 4: Build Context
    │    Format chunks into prompt context:
    │    "Subject: Math | Grade: 10 | Chapter: 3
    │     Content: The Pythagorean theorem..."
    │
    ├──► Step 5: Generate Answer
    │    OpenAIService.GetChatCompletionAsync()
    │    Prompt = context + system instructions + question
    │    GPT-4 generates answer
    │
    ├──► Step 6: Save History
    │    ChatHistoryService.SaveChatHistoryAsync()
    │    INSERT INTO ChatHistories (userId, sessionId, message, reply, contextUsed)
    │
    └──► Step 7: Return Response
         {
           status: "success",
           sessionId: "abc123",
           question: "...",
           reply: "The Pythagorean theorem states...",
           timestamp: "2024-01-15T10:30:00Z"
         }
```

---

## 📤 File Upload & Processing Flow

```
User Uploads PDF
    │
    ▼
┌─────────────────────────────────────────┐
│ POST /api/file/upload                   │
│ multipart/form-data                     │
│ - file: textbook.pdf                    │
│ - subject: "Mathematics"                │
│ - grade: "Grade 10"                     │
│ - chapter: "Chapter 5"                  │
└─────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────┐
│ FileController                          │
└─────────────────────────────────────────┘
    │
    ├──► Upload to Blob Storage
    │    BlobStorageService.UploadFileToBlobAsync()
    │    → https://storage.blob.core.windows.net/textbooks/guid_textbook.pdf
    │
    ├──► Save Metadata
    │    INSERT INTO UploadedFiles
    │    (FileName, BlobUrl, Subject, Grade, Chapter, Status, UploadedAt)
    │    VALUES ('textbook.pdf', 'https://...', 'Math', 'Grade 10', 'Ch 5', 'Pending', NOW())
    │
    └──► Return Response
         {
           status: "success",
           fileId: 123,
           blobUrl: "https://...",
           message: "File uploaded. Processing will begin automatically."
         }

─────────────────────────────────────────────────────────────────

⏱️  Azure Functions Blob Trigger (Automatic)

    │
    ▼
┌─────────────────────────────────────────┐
│ ProcessBlobFile.cs                      │
│ Blob Trigger: textbooks/                │
└─────────────────────────────────────────┘
    │
    ├──► Step 1: Extract Text
    │    TextExtractionService.ExtractTextAsync(blobUrl)
    │    • PDF → PdfPig library
    │    • DOCX → DocumentFormat.OpenXml
    │    • TXT → ReadAllText
    │    → "Chapter 5: Trigonometry\n\nSine, cosine..."
    │
    ├──► Step 2: Chunk Text
    │    ChunkingService.ChunkTextAsync(text)
    │    • Split by ~512 tokens (GPT tokenizer)
    │    • Preserve sentence boundaries
    │    • Maintain context overlap
    │    → [chunk1, chunk2, chunk3, ...]
    │
    ├──► Step 3: Generate Embeddings (Parallel)
    │    FOR EACH chunk:
    │      EmbeddingService.GenerateEmbeddingAsync(chunk)
    │      → Azure OpenAI text-embedding-3-small
    │      → [0.123, 0.456, ..., 0.789] (1536 floats)
    │
    ├──► Step 4: Save to FileChunks
    │    INSERT INTO FileChunks (FileId, ChunkText, ChunkIndex, Subject, Grade, Chapter)
    │    VALUES (123, 'Sine is...', 0, 'Math', 'Grade 10', 'Ch 5'),
    │           (123, 'Cosine is...', 1, 'Math', 'Grade 10', 'Ch 5'),
    │           ...
    │
    ├──► Step 5: Save to ChunkEmbeddings
    │    INSERT INTO ChunkEmbeddings (ChunkId, EmbeddingVector)
    │    VALUES (1001, '[0.123,0.456,...]'),
    │           (1002, '[0.789,0.012,...]'),
    │           ...
    │
    └──► Step 6: Update Status
         UPDATE UploadedFiles
         SET Status = 'Completed', TotalChunks = 150
         WHERE Id = 123

─────────────────────────────────────────────────────────────────

✅ Backend can now use chunks for RAG queries!
```

---

## 🗄️ Database Schema (Azure SQL)

```
┌─────────────────────────┐
│     UploadedFiles       │
├─────────────────────────┤
│ Id (PK)          INT    │
│ FileName         NVARCHAR(500)
│ BlobUrl          NVARCHAR(500)  ← Azure Blob URL
│ UploadedAt       DATETIME2
│ Subject          NVARCHAR(100)
│ Grade            NVARCHAR(50)
│ Chapter          NVARCHAR(200)
│ Status           NVARCHAR(50)   ← Pending/Processing/Completed/Failed
│ TotalChunks      INT
│ UploadedBy       NVARCHAR(200)
└─────────────────────────┘
         │
         │ 1:N
         ▼
┌─────────────────────────┐
│      FileChunks         │
├─────────────────────────┤
│ Id (PK)          INT    │
│ FileId (FK)      INT ───┘
│ ChunkText        NVARCHAR(MAX) ← Extracted text chunk
│ ChunkIndex       INT           ← Sequence in file
│ CreatedAt        DATETIME2
│ Subject          NVARCHAR(100)
│ Grade            NVARCHAR(50)
│ Chapter          NVARCHAR(200)
└─────────────────────────┘
         │
         │ 1:1
         ▼
┌─────────────────────────┐
│   ChunkEmbeddings       │
├─────────────────────────┤
│ Id (PK)          INT    │
│ ChunkId (FK,UQ)  INT ───┘
│ EmbeddingVector  NVARCHAR(MAX) ← JSON: [0.1, 0.2, ..., 0.9]
│ CreatedAt        DATETIME2     ← (1536 floats)
└─────────────────────────┘

┌─────────────────────────┐
│    ChatHistories        │
├─────────────────────────┤
│ Id (PK)          INT    │
│ UserId           NVARCHAR(450)
│ SessionId        NVARCHAR(450) ← Session tracking
│ Message          NVARCHAR(MAX) ← User question
│ Reply            NVARCHAR(MAX) ← AI answer
│ Timestamp        DATETIME2
│ ContextUsed      NVARCHAR(MAX) ← JSON: Source chunks
│ ContextCount     INT
│ AuthenticatedUserId INT (FK, nullable)
└─────────────────────────┘

┌─────────────────────────┐
│      StudyNotes         │
├─────────────────────────┤
│ Id (PK)          INT    │
│ UserId           NVARCHAR(450)
│ Topic            NVARCHAR(500)
│ GeneratedNotes   NVARCHAR(MAX) ← Markdown content
│ SourceChunks     NVARCHAR(MAX) ← JSON: Source chunks
│ Subject          NVARCHAR(100)
│ Grade            NVARCHAR(50)
│ Chapter          NVARCHAR(200)
│ CreatedAt        DATETIME2
│ Rating           INT           ← 1-5 stars
│ AuthenticatedUserId INT (FK, nullable)
└─────────────────────────┘
```

### Indexes for Performance

```sql
-- FileChunks indexes
CREATE INDEX IX_FileChunks_FileId_ChunkIndex ON FileChunks(FileId, ChunkIndex);
CREATE INDEX IX_FileChunks_Subject_Grade_Chapter ON FileChunks(Subject, Grade, Chapter);

-- ChunkEmbeddings indexes
CREATE UNIQUE INDEX IX_ChunkEmbeddings_ChunkId ON ChunkEmbeddings(ChunkId);

-- ChatHistories indexes
CREATE INDEX IX_ChatHistories_UserId_SessionId_Timestamp 
    ON ChatHistories(UserId, SessionId, Timestamp);

-- StudyNotes indexes
CREATE INDEX IX_StudyNotes_UserId_CreatedAt ON StudyNotes(UserId, CreatedAt);
```

---

## 🔐 Security Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                         Frontend                             │
│  - JWT stored in localStorage                                │
│  - Sent in Authorization: Bearer <token>                     │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   ASP.NET Core Middleware                    │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  1. CORS Policy (AllowAnyOrigin in dev)            │   │
│  │  2. HTTPS Redirection                               │   │
│  │  3. JWT Authentication Middleware                   │   │
│  │  4. Authorization Middleware                        │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Controllers                             │
│  - [Authorize] attributes where needed                       │
│  - Role-based access control (Admin, Teacher, Student)       │
│  - IP-based user identification (fallback)                   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                        Services                              │
│  - Input validation                                          │
│  - Business rule enforcement                                 │
│  - User context isolation                                    │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    EF Core (Data Access)                     │
│  - Parameterized queries (SQL injection protection)          │
│  - Connection pooling                                        │
│  - Retry logic for transient failures                        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Azure SQL Database                      │
│  - Firewall rules (Allow Azure services)                    │
│  - TLS encryption in transit                                │
│  - Transparent Data Encryption at rest                       │
└─────────────────────────────────────────────────────────────┘
```

---

## 📊 Component Interaction Matrix

| Component | ChatController | NotesController | FileController | Azure Functions |
|-----------|---------------|-----------------|----------------|-----------------|
| **OpenAIService** | ✅ Chat completion | ✅ Study notes | ❌ | ✅ Embeddings |
| **RAGService** | ✅ Main RAG | ✅ Chunk retrieval | ❌ | ❌ |
| **BlobStorageService** | ❌ | ❌ | ✅ Upload | ❌ (direct SDK) |
| **ChatHistoryService** | ✅ Save history | ❌ | ❌ | ❌ |
| **StudyNotesService** | ❌ | ✅ Generate | ❌ | ❌ |
| **AppDbContext** | ✅ Read chunks | ✅ Save notes | ✅ Save metadata | ✅ Write chunks |

---

## 🚀 Deployment Configuration

### Environment Variables (App Service)

```bash
# Database
ConnectionStrings__SqlDb=Server=tcp:...;Database=...;User ID=...;Password=...;Encrypt=True;

# Azure Storage
AzureWebJobsStorage=DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;

# Azure OpenAI
AzureOpenAI__Endpoint=https://YOUR_RESOURCE.openai.azure.com/
AzureOpenAI__ApiKey=YOUR_API_KEY
AzureOpenAI__ChatDeployment=gpt-4
AzureOpenAI__EmbeddingDeployment=text-embedding-3-small

# Features
USE_REAL_EMBEDDINGS=true

# Security
Jwt__Key=YOUR_SECRET_KEY_MINIMUM_32_CHARACTERS
Jwt__Issuer=SchoolAiChatbotBackend
Jwt__Audience=SchoolAiChatbotUsers
```

---

## 📈 Performance Metrics

### Typical Response Times
- `/health` endpoint: **< 50ms**
- `/api/chat` (RAG query):
  - Embedding generation: **~500ms**
  - SQL similarity search: **~50-200ms** (1000 chunks)
  - Chat completion: **~1-2 seconds**
  - **Total: ~2-3 seconds**
- `/api/notes/generate`: **~3-10 seconds** (larger context)
- `/api/file/upload`: **< 1 second** (blob upload)
- Azure Functions processing: **30-300 seconds** (per file)

### Scalability
- **Horizontal scaling:** Azure App Service can scale out to multiple instances
- **Database:** Can upgrade to higher tier for more DTUs
- **Bottleneck:** OpenAI API rate limits (tier-dependent)

---

## ✅ Migration Checklist

- [x] Database schema aligned (FileChunks, ChunkEmbeddings)
- [x] Configuration keys compatible with Azure Functions
- [x] OpenAIService created (Azure OpenAI + OpenAI)
- [x] RAGService rewritten (SQL cosine similarity)
- [x] StudyNotesService updated (SQL-based RAG)
- [x] BlobStorageService created
- [x] Controllers updated (Chat, Notes, File)
- [x] DTOs created
- [x] Program.cs service registration complete
- [x] Documentation created (API, Migration, Deployment)

**Status: ✅ Ready for Production Deployment**

---

**Last Updated:** November 15, 2025  
**Azure App Service:** app-wlanqwy7vuwmu  
**Deployment Status:** Production Ready
