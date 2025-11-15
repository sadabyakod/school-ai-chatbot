# Quick Start Script for Migration Testing
# Run this script to test the migrated backend locally

Write-Host "🚀 Starting School AI Chatbot Backend Migration Test" -ForegroundColor Green
Write-Host ""

# Step 1: Build the project
Write-Host "📦 Building project..." -ForegroundColor Yellow
cd c:\school-ai-chatbot\SchoolAiChatbotBackend
dotnet build

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed. Please fix compilation errors." -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful!" -ForegroundColor Green
Write-Host ""

# Step 2: Create and apply migrations (optional - comment out if already done)
Write-Host "🗄️ Creating database migration..." -ForegroundColor Yellow
dotnet ef migrations add AddChatHistoryAndStudyNotes 2>$null

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Migration created successfully!" -ForegroundColor Green
} else {
    Write-Host "⚠️ Migration already exists or failed. Continuing..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🗄️ Applying database migrations..." -ForegroundColor Yellow
dotnet ef database update

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Migration failed. Check database connection." -ForegroundColor Red
    exit 1
}

Write-Host "✅ Database updated successfully!" -ForegroundColor Green
Write-Host ""

# Step 3: Start the backend server
Write-Host "🌐 Starting backend server on http://localhost:8080..." -ForegroundColor Yellow
Write-Host ""
Write-Host "📌 Test endpoints:" -ForegroundColor Cyan
Write-Host "   - http://localhost:8080/api/chat/test" -ForegroundColor White
Write-Host "   - http://localhost:8080/api/notes/test" -ForegroundColor White
Write-Host ""
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Gray
Write-Host ""

dotnet run
