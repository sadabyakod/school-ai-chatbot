# Entity Framework Core Migration Commands

## Create Migration

Run this to create the database migration for ChatHistory and StudyNote tables:

```powershell
cd c:\school-ai-chatbot\SchoolAiChatbotBackend
dotnet ef migrations add AddChatHistoryAndStudyNotes
```

## Apply Migration (Development)

Update your local database:

```powershell
cd c:\school-ai-chatbot\SchoolAiChatbotBackend
dotnet ef database update
```

## Apply Migration (Production)

Update production Azure SQL Database:

```powershell
cd c:\school-ai-chatbot\SchoolAiChatbotBackend

# Option 1: Using connection string from appsettings
dotnet ef database update --configuration Release

# Option 2: Using explicit connection string
dotnet ef database update --connection "YOUR_PRODUCTION_CONNECTION_STRING"
```

## View Pending Migrations

Check what migrations need to be applied:

```powershell
dotnet ef migrations list
```

## Rollback Migration (if needed)

Revert to previous migration:

```powershell
dotnet ef database update PreviousMigrationName
```

## Remove Last Migration (before applying)

If you need to undo the last migration (only if not yet applied):

```powershell
dotnet ef migrations remove
```

## Generate SQL Script (for manual review)

Generate SQL script without applying:

```powershell
dotnet ef migrations script > migration.sql
```

## Troubleshooting

### Error: "Build failed"
```powershell
# Ensure project builds first
dotnet build
dotnet ef migrations add AddChatHistoryAndStudyNotes
```

### Error: "No DbContext found"
```powershell
# Specify the DbContext explicitly
dotnet ef migrations add AddChatHistoryAndStudyNotes --context AppDbContext
```

### Error: "Connection string not found"
```powershell
# Set environment variable or use --connection flag
$env:ConnectionStrings__DefaultConnection="YOUR_CONNECTION_STRING"
dotnet ef database update
```

## Expected Migration Output

The migration should create:

1. **ChatHistories** table with columns:
   - Id, UserId, SessionId, Message, Reply, Timestamp, ContextUsed, ContextCount, AuthenticatedUserId

2. **StudyNotes** table with columns:
   - Id, UserId, Topic, GeneratedNotes, SourceChunks, Subject, Grade, Chapter, CreatedAt, AuthenticatedUserId, Rating

3. **Indexes:**
   - IX_ChatHistories_UserId_SessionId_Timestamp
   - IX_StudyNotes_UserId_CreatedAt

4. **Foreign Keys:**
   - ChatHistories.AuthenticatedUserId → Users.Id (nullable, restrict)
   - StudyNotes.AuthenticatedUserId → Users.Id (nullable, restrict)

## Production Deployment

When deploying to Azure App Service, migrations can be applied automatically:

### Option A: Manual migration after deployment
```powershell
# SSH into Azure App Service
az webapp ssh --name studyai-ingestion-345 --resource-group <your-rg>

# Run migration
dotnet ef database update
```

### Option B: Add migration to startup (not recommended for production)
```csharp
// In Program.cs (only for development)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();  // Auto-apply pending migrations
}
```

### Option C: Use GitHub Actions (recommended)
Add migration step to `.github/workflows/backend-deploy.yml`:

```yaml
- name: Apply Database Migrations
  run: |
    cd SchoolAiChatbotBackend
    dotnet ef database update --connection "${{ secrets.DB_CONNECTION_STRING }}"
```

## Verify Migration Success

After applying migration, verify tables exist:

```powershell
# Connect to database and run
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('ChatHistories', 'StudyNotes');
```

Expected output:
```
ChatHistories
StudyNotes
```
