# Test Script for New Endpoints
# Tests all migrated features locally

$baseUrl = "http://localhost:8080/api"

Write-Host "🧪 Testing Migrated Endpoints" -ForegroundColor Green
Write-Host ""

# Test 1: Chat health check
Write-Host "1️⃣ Testing Chat Health Check..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/chat/test" -Method Get
    Write-Host "✅ Chat endpoint: $response" -ForegroundColor Green
} catch {
    Write-Host "❌ Chat endpoint failed: $_" -ForegroundColor Red
}

Write-Host ""

# Test 2: Notes health check
Write-Host "2️⃣ Testing Notes Health Check..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/notes/test" -Method Get
    Write-Host "✅ Notes endpoint: $response" -ForegroundColor Green
} catch {
    Write-Host "❌ Notes endpoint failed: $_" -ForegroundColor Red
}

Write-Host ""

# Test 3: Chat with session
Write-Host "3️⃣ Testing Chat with Session..." -ForegroundColor Yellow
try {
    $chatBody = @{
        question = "What is photosynthesis?"
        sessionId = "test-session-$(Get-Date -Format 'yyyyMMddHHmmss')"
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "$baseUrl/chat" -Method Post `
        -ContentType "application/json" `
        -Body $chatBody

    Write-Host "✅ Chat Response:" -ForegroundColor Green
    Write-Host "   Session ID: $($response.sessionId)" -ForegroundColor White
    Write-Host "   Reply: $($response.reply.Substring(0, [Math]::Min(100, $response.reply.Length)))..." -ForegroundColor White
    Write-Host "   Context Count: $($response.contextCount)" -ForegroundColor White
} catch {
    Write-Host "❌ Chat failed: $_" -ForegroundColor Red
}

Write-Host ""

# Test 4: Generate study notes
Write-Host "4️⃣ Testing Study Notes Generation..." -ForegroundColor Yellow
try {
    $notesBody = @{
        topic = "Newton's Laws of Motion"
        subject = "Physics"
        grade = "9"
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "$baseUrl/notes/generate" -Method Post `
        -ContentType "application/json" `
        -Body $notesBody

    Write-Host "✅ Study Notes Generated:" -ForegroundColor Green
    Write-Host "   Note ID: $($response.noteId)" -ForegroundColor White
    Write-Host "   Topic: $($response.topic)" -ForegroundColor White
    Write-Host "   Preview: $($response.notes.Substring(0, [Math]::Min(150, $response.notes.Length)))..." -ForegroundColor White
} catch {
    Write-Host "❌ Study notes generation failed: $_" -ForegroundColor Red
}

Write-Host ""

# Test 5: Get study notes history
Write-Host "5️⃣ Testing Study Notes History..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/notes?limit=5" -Method Get

    Write-Host "✅ Study Notes History:" -ForegroundColor Green
    Write-Host "   Total Notes: $($response.count)" -ForegroundColor White
    
    if ($response.count -gt 0) {
        Write-Host "   Latest Note: $($response.notes[0].topic)" -ForegroundColor White
    }
} catch {
    Write-Host "❌ Study notes history failed: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎉 Testing Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📚 Next Steps:" -ForegroundColor Cyan
Write-Host "   1. Check MIGRATION.md for deployment guide" -ForegroundColor White
Write-Host "   2. Update frontend with sessionId support" -ForegroundColor White
Write-Host "   3. Deploy to Azure App Service" -ForegroundColor White
