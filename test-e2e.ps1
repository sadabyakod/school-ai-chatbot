# ============================================
# School AI Chatbot - End-to-End Testing Script
# Tests Frontend to Backend Integration
# ============================================

$ErrorActionPreference = "Continue"
$backendUrl = "http://localhost:8080"
$frontendUrl = "http://localhost:5173"
$testsPassed = 0
$testsFailed = 0

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  School AI Chatbot - E2E Testing" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Function to test API endpoint
function Test-ApiEndpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [object]$Body = $null,
        [int]$ExpectedStatus = 200
    )
    
    Write-Host "Testing: $Name" -ForegroundColor Yellow
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            ContentType = "application/json"
            ErrorAction = "Stop"
            TimeoutSec = 30
        }
        
        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }
        
        $response = Invoke-WebRequest @params
        
        if ($response.StatusCode -eq $ExpectedStatus) {
            Write-Host "  PASSED - Status: $($response.StatusCode)" -ForegroundColor Green
            $script:testsPassed++
            
            # Try to parse JSON response
            try {
                return $response.Content | ConvertFrom-Json
            } catch {
                return $response.Content
            }
        } else {
            Write-Host "  FAILED - Expected: $ExpectedStatus, Got: $($response.StatusCode)" -ForegroundColor Red
            $script:testsFailed++
            return $null
        }
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq $ExpectedStatus) {
            Write-Host "  PASSED - Status: $statusCode (Expected failure)" -ForegroundColor Green
            $script:testsPassed++
        } else {
            Write-Host "  FAILED - Error: $($_.Exception.Message)" -ForegroundColor Red
            $script:testsFailed++
        }
        return $null
    }
}

# Function to check if service is running
function Test-ServiceRunning {
    param([string]$Url, [string]$ServiceName)
    
    Write-Host "Checking $ServiceName..." -ForegroundColor Yellow
    try {
        $response = Invoke-WebRequest -Uri $Url -Method GET -TimeoutSec 5 -ErrorAction Stop
        Write-Host "  $ServiceName is running!" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "  $ServiceName is NOT running at $Url" -ForegroundColor Red
        return $false
    }
}

Write-Host "=== Step 1: Service Health Checks ===" -ForegroundColor Magenta
Write-Host ""

$backendRunning = Test-ServiceRunning -Url "$backendUrl/api/chat/test" -ServiceName "Backend API"
$frontendRunning = Test-ServiceRunning -Url $frontendUrl -ServiceName "Frontend"

if (-not $backendRunning) {
    Write-Host "`nBackend is not running. Starting backend..." -ForegroundColor Yellow
    Write-Host "Please wait..." -ForegroundColor Gray
    
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd 'c:\school-ai-chatbot\SchoolAiChatbotBackend'; dotnet run" -WindowStyle Minimized
    
    Write-Host "Waiting for backend to start (15 seconds)..." -ForegroundColor Gray
    Start-Sleep -Seconds 15
    
    $backendRunning = Test-ServiceRunning -Url "$backendUrl/api/chat/test" -ServiceName "Backend API (retry)"
}

if (-not $frontendRunning) {
    Write-Host "`nFrontend is not running. Starting frontend..." -ForegroundColor Yellow
    Write-Host "Please wait..." -ForegroundColor Gray
    
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd 'c:\school-ai-chatbot\school-ai-frontend'; npm run dev" -WindowStyle Minimized
    
    Write-Host "Waiting for frontend to start (10 seconds)..." -ForegroundColor Gray
    Start-Sleep -Seconds 10
}

if (-not $backendRunning) {
    Write-Host "`nERROR: Backend is not running. Cannot proceed with tests." -ForegroundColor Red
    Write-Host "Please start the backend manually:" -ForegroundColor Yellow
    Write-Host "  cd c:\school-ai-chatbot\SchoolAiChatbotBackend" -ForegroundColor Cyan
    Write-Host "  dotnet run" -ForegroundColor Cyan
    exit 1
}

Write-Host "`n=== Step 2: Backend API Tests ===" -ForegroundColor Magenta
Write-Host ""

# Test basic endpoints
Test-ApiEndpoint -Name "Chat Endpoint Test" -Method GET -Url "$backendUrl/api/chat/test"
Test-ApiEndpoint -Name "Notes Endpoint Test" -Method GET -Url "$backendUrl/api/notes/test"

# Test chat continuity feature
Write-Host "`n--- Chat Continuity Feature ---" -ForegroundColor Cyan
$chatRequest = @{
    Question = "What is photosynthesis?"
    Language = "en"
}
$chatResponse = Test-ApiEndpoint -Name "Send Chat Message" -Method POST -Url "$backendUrl/api/chat" -Body $chatRequest

if ($chatResponse) {
    Write-Host "  Chat Response Preview: $($chatResponse.reply.Substring(0, [Math]::Min(100, $chatResponse.reply.Length)))..." -ForegroundColor Gray
    Write-Host "  Session ID: $($chatResponse.sessionId)" -ForegroundColor Gray
}

$recentSession = Test-ApiEndpoint -Name "Get Most Recent Session" -Method GET -Url "$backendUrl/api/chat/most-recent-session"

if ($recentSession -and $recentSession.sessionId) {
    Write-Host "  Retrieved Session ID: $($recentSession.sessionId)" -ForegroundColor Gray
}

# Test study notes features
Write-Host "`n--- Study Notes Features ---" -ForegroundColor Cyan

# Get existing notes
$notesResponse = Test-ApiEndpoint -Name "Get User Study Notes" -Method GET -Url "$backendUrl/api/notes?limit=5"

if ($notesResponse -and $notesResponse.notes -and $notesResponse.notes.Count -gt 0) {
    Write-Host "  Found $($notesResponse.count) existing notes" -ForegroundColor Gray
    $existingNoteId = $notesResponse.notes[0].id
    
    # Test retrieve specific note
    Write-Host "`n--- Test Existing Note Operations ---" -ForegroundColor Cyan
    $note = Test-ApiEndpoint -Name "Get Note by ID" -Method GET -Url "$backendUrl/api/notes/$existingNoteId"
    
    if ($note) {
        # Test update note
        $updateRequest = @{
            Content = "# Updated Notes`n`nThis content has been edited for testing.`n`n## Key Points`n- Test point 1`n- Test point 2"
        }
        $updated = Test-ApiEndpoint -Name "Update Note Content" -Method PUT -Url "$backendUrl/api/notes/$existingNoteId" -Body $updateRequest
        
        # Test share note
        $shared = Test-ApiEndpoint -Name "Share Note" -Method POST -Url "$backendUrl/api/notes/$existingNoteId/share"
        
        if ($shared -and $shared.shareToken) {
            Write-Host "  Share Token: $($shared.shareToken)" -ForegroundColor Gray
            
            # Test public access to shared note
            $publicNote = Test-ApiEndpoint -Name "Access Shared Note (Public)" -Method GET -Url "$backendUrl/api/notes/shared/$($shared.shareToken)"
            
            # Test unshare
            Test-ApiEndpoint -Name "Unshare Note" -Method POST -Url "$backendUrl/api/notes/$existingNoteId/unshare"
            
            # Verify unshared note is not accessible
            Test-ApiEndpoint -Name "Verify Unshared (Should Fail)" -Method GET -Url "$backendUrl/api/notes/shared/$($shared.shareToken)" -ExpectedStatus 404
        }
        
        # Test rating
        $rateRequest = @{ Rating = 5 }
        Test-ApiEndpoint -Name "Rate Note (5 stars)" -Method POST -Url "$backendUrl/api/notes/$existingNoteId/rate" -Body $rateRequest
    }
} else {
    Write-Host "  No existing notes found. Note operations skipped." -ForegroundColor Gray
}

# Test error handling
Write-Host "`n--- Error Handling Tests ---" -ForegroundColor Cyan
Test-ApiEndpoint -Name "Get Non-existent Note" -Method GET -Url "$backendUrl/api/notes/99999" -ExpectedStatus 404
Test-ApiEndpoint -Name "Update Non-existent Note" -Method PUT -Url "$backendUrl/api/notes/99999" -Body @{Content="test"} -ExpectedStatus 404

Write-Host "`n=== Step 3: Frontend Integration Check ===" -ForegroundColor Magenta
Write-Host ""

if ($frontendRunning) {
    Write-Host "Frontend is accessible at: $frontendUrl" -ForegroundColor Green
    Write-Host "You can now manually test the following:" -ForegroundColor Yellow
    Write-Host "  1. Open $frontendUrl in your browser" -ForegroundColor Cyan
    Write-Host "  2. Test chat functionality" -ForegroundColor Cyan
    Write-Host "  3. Test file upload (if applicable)" -ForegroundColor Cyan
    Write-Host "  4. Check FAQs section" -ForegroundColor Cyan
    Write-Host "  5. Verify all features work end-to-end" -ForegroundColor Cyan
} else {
    Write-Host "Frontend is not accessible. Please start it manually:" -ForegroundColor Yellow
    Write-Host "  cd c:\school-ai-chatbot\school-ai-frontend" -ForegroundColor Cyan
    Write-Host "  npm run dev" -ForegroundColor Cyan
}

Write-Host "`n=== Step 4: Test Summary ===" -ForegroundColor Magenta
Write-Host ""
Write-Host "Backend Tests Passed: $testsPassed" -ForegroundColor Green
Write-Host "Backend Tests Failed: $testsFailed" -ForegroundColor $(if ($testsFailed -eq 0) { "Green" } else { "Red" })

if ($testsFailed -eq 0) {
    Write-Host "`nSUCCESS: All backend tests passed!" -ForegroundColor Green
    Write-Host "The application is ready for use." -ForegroundColor Green
} else {
    Write-Host "`nWARNING: Some tests failed. Please review the errors above." -ForegroundColor Yellow
}

Write-Host "`n=== Next Steps ===" -ForegroundColor Magenta
Write-Host "1. Backend is running at: $backendUrl" -ForegroundColor Cyan
Write-Host "2. Frontend should be at: $frontendUrl" -ForegroundColor Cyan
Write-Host "3. Open your browser and test the application manually" -ForegroundColor Cyan
Write-Host "4. Press Ctrl+C in the minimized windows to stop the services when done" -ForegroundColor Cyan
Write-Host ""
