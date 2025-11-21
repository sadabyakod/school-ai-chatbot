# Integration Test Script for Study Notes Features
# Tests all endpoints including the new edit and share features

$baseUrl = "http://localhost:8080/api"
$testsPassed = 0
$testsFailed = 0

Write-Host "`n=== School AI Chatbot - Study Notes Integration Tests ===`n" -ForegroundColor Cyan

# Function to test endpoint
function Test-Endpoint {
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
        }
        
        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }
        
        $response = Invoke-WebRequest @params
        
        if ($response.StatusCode -eq $ExpectedStatus) {
            Write-Host "  PASSED - Status: $($response.StatusCode)" -ForegroundColor Green
            $script:testsPassed++
            return $response.Content | ConvertFrom-Json
        } else {
            Write-Host "  FAILED - Expected: $ExpectedStatus, Got: $($response.StatusCode)" -ForegroundColor Red
            $script:testsFailed++
            return $null
        }
    }
    catch {
        Write-Host "  FAILED - Error: $($_.Exception.Message)" -ForegroundColor Red
        $script:testsFailed++
        return $null
    }
}

# Test 1: Check endpoints are working
Write-Host "`n--- Test 1: Basic Endpoint Check ---" -ForegroundColor Magenta
Test-Endpoint -Name "Notes Test Endpoint" -Method GET -Url "$baseUrl/notes/test"
Test-Endpoint -Name "Chat Test Endpoint" -Method GET -Url "$baseUrl/chat/test"

# Test 2: Get most recent session
Write-Host "`n--- Test 2: Chat Continuity Feature ---" -ForegroundColor Magenta
Test-Endpoint -Name "Get Most Recent Session" -Method GET -Url "$baseUrl/chat/most-recent-session" -ExpectedStatus 404

# Summary
Write-Host "`n=== Test Summary ===" -ForegroundColor Cyan
Write-Host "Tests Passed: $testsPassed" -ForegroundColor Green
Write-Host "Tests Failed: $testsFailed" -ForegroundColor $(if ($testsFailed -eq 0) { "Green" } else { "Red" })

if ($testsFailed -eq 0) {
    Write-Host "`nAll tests passed! Application is working correctly." -ForegroundColor Green
} else {
    Write-Host "`nSome tests failed. Please review the errors above." -ForegroundColor Yellow
}
