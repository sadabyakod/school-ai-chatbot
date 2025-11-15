# Test Frontend-Backend Integration
# This script tests the connection between React frontend and ASP.NET Core backend

Write-Host "====================================" -ForegroundColor Cyan
Write-Host "Frontend-Backend Integration Test" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$BACKEND_URL = "http://localhost:8080"
$FRONTEND_URL = "http://localhost:5173"

Write-Host "Testing Backend..." -ForegroundColor Yellow

# Test 1: Backend Health Check
Write-Host "`n[1/5] Testing backend health endpoint..." -ForegroundColor White
try {
    $response = Invoke-WebRequest -Uri "$BACKEND_URL/health" -Method GET -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Backend is healthy!" -ForegroundColor Green
        Write-Host "    Response: $($response.Content)" -ForegroundColor Gray
    }
} catch {
    Write-Host "❌ Backend is not responding. Please start it with:" -ForegroundColor Red
    Write-Host "    cd SchoolAiChatbotBackend; dotnet run" -ForegroundColor Yellow
    exit 1
}

# Test 2: API Health Check
Write-Host "`n[2/5] Testing API health endpoint..." -ForegroundColor White
try {
    $response = Invoke-WebRequest -Uri "$BACKEND_URL/api/health" -Method GET -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ API endpoint is healthy!" -ForegroundColor Green
        Write-Host "    Response: $($response.Content)" -ForegroundColor Gray
    }
} catch {
    Write-Host "❌ API endpoint failed" -ForegroundColor Red
}

# Test 3: Chat Endpoint
Write-Host "`n[3/5] Testing chat endpoint..." -ForegroundColor White
try {
    $body = @{
        Question = "What is mathematics?"
    } | ConvertTo-Json

    $response = Invoke-WebRequest -Uri "$BACKEND_URL/api/chat" -Method POST -Body $body -ContentType "application/json" -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        $data = $response.Content | ConvertFrom-Json
        Write-Host "✅ Chat endpoint working!" -ForegroundColor Green
        Write-Host "    Status: $($data.status)" -ForegroundColor Gray
        Write-Host "    Reply: $($data.reply.Substring(0, [Math]::Min(100, $data.reply.Length)))..." -ForegroundColor Gray
    }
} catch {
    Write-Host "❌ Chat endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: FAQs Endpoint
Write-Host "`n[4/5] Testing FAQs endpoint..." -ForegroundColor White
try {
    $response = Invoke-WebRequest -Uri "$BACKEND_URL/api/faqs" -Method GET -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        $data = $response.Content | ConvertFrom-Json
        Write-Host "✅ FAQs endpoint working!" -ForegroundColor Green
        Write-Host "    Found $($data.Count) FAQs" -ForegroundColor Gray
    }
} catch {
    Write-Host "⚠️  FAQs endpoint may not have data yet" -ForegroundColor Yellow
}

# Test 5: CORS Headers
Write-Host "`n[5/5] Testing CORS configuration..." -ForegroundColor White
try {
    $headers = @{
        "Origin" = $FRONTEND_URL
    }
    $response = Invoke-WebRequest -Uri "$BACKEND_URL/api/health" -Method GET -Headers $headers -UseBasicParsing
    $corsHeader = $response.Headers["Access-Control-Allow-Origin"]
    if ($corsHeader) {
        Write-Host "✅ CORS is properly configured!" -ForegroundColor Green
        Write-Host "    Access-Control-Allow-Origin: $corsHeader" -ForegroundColor Gray
    } else {
        Write-Host "⚠️  CORS headers not found (may still work)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ CORS test failed" -ForegroundColor Red
}

Write-Host "`n====================================" -ForegroundColor Cyan
Write-Host "Integration Test Summary" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Backend is ready at: $BACKEND_URL" -ForegroundColor Green
Write-Host "📋 Environment Configuration:" -ForegroundColor White
Write-Host "    Local Development: .env.development → http://localhost:8080" -ForegroundColor Gray
Write-Host "    Local Testing: .env.local → https://app-wlanqwy7vuwmu.azurewebsites.net" -ForegroundColor Gray
Write-Host "    Production: .env.production → https://app-wlanqwy7vuwmu.azurewebsites.net" -ForegroundColor Gray
Write-Host ""
Write-Host "🚀 To start frontend, run:" -ForegroundColor Yellow
Write-Host "    cd school-ai-frontend" -ForegroundColor White
Write-Host "    npm run dev" -ForegroundColor White
Write-Host ""
Write-Host "📱 Frontend will be available at: $FRONTEND_URL" -ForegroundColor Cyan
Write-Host ""
