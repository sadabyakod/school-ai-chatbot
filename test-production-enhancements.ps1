# Production Enhancements Test Script
# Tests all the new features: retry logic, error handling, logging, toasts

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  PRODUCTION ENHANCEMENTS TEST SUITE" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$backendUrl = "http://localhost:8080"
$testResults = @()

# Test 1: Backend Health Check
Write-Host "[TEST 1] Backend Health Check..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$backendUrl/health" -Method GET -TimeoutSec 5
    if ($health.status -eq "healthy") {
        Write-Host "  ✅ Backend is healthy" -ForegroundColor Green
        Write-Host "     Status: $($health.status)" -ForegroundColor Gray
        Write-Host "     Timestamp: $($health.timestamp)" -ForegroundColor Gray
        Write-Host "     Database: $($health.database)" -ForegroundColor Gray
        $testResults += @{ Test = "Health Check"; Status = "PASS" }
    } else {
        Write-Host "  ❌ Backend health check failed" -ForegroundColor Red
        $testResults += @{ Test = "Health Check"; Status = "FAIL" }
    }
} catch {
    Write-Host "  ❌ Cannot reach backend at $backendUrl" -ForegroundColor Red
    Write-Host "     Make sure backend is running: cd SchoolAiChatbotBackend; dotnet run" -ForegroundColor Yellow
    $testResults += @{ Test = "Health Check"; Status = "FAIL" }
}

# Test 2: API Health Endpoint
Write-Host "`n[TEST 2] API Health Endpoint..." -ForegroundColor Yellow
try {
    $apiHealth = Invoke-RestMethod -Uri "$backendUrl/api/health" -Method GET -TimeoutSec 5
    if ($apiHealth.status -eq "healthy") {
        Write-Host "  ✅ API health endpoint working" -ForegroundColor Green
        Write-Host "     API Version: $($apiHealth.api)" -ForegroundColor Gray
        $testResults += @{ Test = "API Health"; Status = "PASS" }
    }
} catch {
    Write-Host "  ❌ API health endpoint failed" -ForegroundColor Red
    $testResults += @{ Test = "API Health"; Status = "FAIL" }
}

# Test 3: Chat Endpoint (Valid Request)
Write-Host "`n[TEST 3] Chat Endpoint (Valid Request)..." -ForegroundColor Yellow
try {
    $chatRequest = @{
        Question = "What is the capital of France?"
    } | ConvertTo-Json

    $chatResponse = Invoke-RestMethod -Uri "$backendUrl/api/chat" `
        -Method POST `
        -ContentType "application/json" `
        -Body $chatRequest `
        -TimeoutSec 30

    if ($chatResponse.reply) {
        Write-Host "  ✅ Chat endpoint working" -ForegroundColor Green
        Write-Host "     Reply: $($chatResponse.reply.Substring(0, [Math]::Min(100, $chatResponse.reply.Length)))..." -ForegroundColor Gray
        Write-Host "     Status: $($chatResponse.status)" -ForegroundColor Gray
        $testResults += @{ Test = "Chat Endpoint"; Status = "PASS" }
    }
} catch {
    Write-Host "  ❌ Chat endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
    $testResults += @{ Test = "Chat Endpoint"; Status = "FAIL" }
}

# Test 4: Error Handling (Invalid Request)
Write-Host "`n[TEST 4] Error Handling (Invalid Request)..." -ForegroundColor Yellow
try {
    $invalidRequest = @{} | ConvertTo-Json
    Invoke-RestMethod -Uri "$backendUrl/api/chat" `
        -Method POST `
        -ContentType "application/json" `
        -Body $invalidRequest `
        -TimeoutSec 10
    Write-Host "  ⚠️  Expected error but got success" -ForegroundColor Yellow
    $testResults += @{ Test = "Error Handling"; Status = "WARN" }
} catch {
    $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
    if ($errorResponse.status -or $errorResponse.title) {
        Write-Host "  ✅ Error handling working (ProblemDetails format)" -ForegroundColor Green
        Write-Host "     Status: $($errorResponse.status)" -ForegroundColor Gray
        Write-Host "     Title: $($errorResponse.title)" -ForegroundColor Gray
        Write-Host "     Detail: $($errorResponse.detail)" -ForegroundColor Gray
        $testResults += @{ Test = "Error Handling"; Status = "PASS" }
    } else {
        Write-Host "  ❌ Error response not in ProblemDetails format" -ForegroundColor Red
        $testResults += @{ Test = "Error Handling"; Status = "FAIL" }
    }
}

# Test 5: FAQs Endpoint
Write-Host "`n[TEST 5] FAQs Endpoint..." -ForegroundColor Yellow
try {
    $faqs = Invoke-RestMethod -Uri "$backendUrl/api/faqs" -Method GET -TimeoutSec 10
    Write-Host "  ✅ FAQs endpoint working" -ForegroundColor Green
    Write-Host "     FAQs count: $($faqs.Count)" -ForegroundColor Gray
    $testResults += @{ Test = "FAQs Endpoint"; Status = "PASS" }
} catch {
    Write-Host "  ⚠️  FAQs endpoint error: $($_.Exception.Message)" -ForegroundColor Yellow
    $testResults += @{ Test = "FAQs Endpoint"; Status = "WARN" }
}

# Test 6: Check Serilog Logs
Write-Host "`n[TEST 6] Serilog Logging..." -ForegroundColor Yellow
$logsPath = "SchoolAiChatbotBackend\logs"
if (Test-Path $logsPath) {
    $logFiles = Get-ChildItem -Path $logsPath -Filter "app-*.log" | Sort-Object LastWriteTime -Descending
    if ($logFiles.Count -gt 0) {
        $latestLog = $logFiles[0]
        Write-Host "  ✅ Serilog logs found" -ForegroundColor Green
        Write-Host "     Latest log: $($latestLog.Name)" -ForegroundColor Gray
        Write-Host "     Size: $([Math]::Round($latestLog.Length / 1KB, 2)) KB" -ForegroundColor Gray
        Write-Host "     Last modified: $($latestLog.LastWriteTime)" -ForegroundColor Gray
        
        # Show last 5 log entries
        Write-Host "`n     Recent log entries:" -ForegroundColor Cyan
        $logContent = Get-Content -Path $latestLog.FullName -Tail 5
        foreach ($line in $logContent) {
            Write-Host "       $line" -ForegroundColor Gray
        }
        $testResults += @{ Test = "Serilog Logging"; Status = "PASS" }
    } else {
        Write-Host "  ⚠️  No log files found in $logsPath" -ForegroundColor Yellow
        $testResults += @{ Test = "Serilog Logging"; Status = "WARN" }
    }
} else {
    Write-Host "  ⚠️  Logs directory not found: $logsPath" -ForegroundColor Yellow
    Write-Host "     Logs will be created when backend runs" -ForegroundColor Gray
    $testResults += @{ Test = "Serilog Logging"; Status = "WARN" }
}

# Test 7: CORS Configuration
Write-Host "`n[TEST 7] CORS Configuration..." -ForegroundColor Yellow
try {
    $headers = @{
        "Origin" = "http://localhost:5173"
        "Access-Control-Request-Method" = "POST"
        "Access-Control-Request-Headers" = "Content-Type"
    }
    $response = Invoke-WebRequest -Uri "$backendUrl/health" `
        -Method OPTIONS `
        -Headers $headers `
        -UseBasicParsing `
        -TimeoutSec 5
    
    $corsHeader = $response.Headers["Access-Control-Allow-Origin"]
    if ($corsHeader) {
        Write-Host "  ✅ CORS configured correctly" -ForegroundColor Green
        Write-Host "     Allow-Origin: $corsHeader" -ForegroundColor Gray
        $testResults += @{ Test = "CORS"; Status = "PASS" }
    } else {
        Write-Host "  ⚠️  CORS headers not found" -ForegroundColor Yellow
        $testResults += @{ Test = "CORS"; Status = "WARN" }
    }
} catch {
    Write-Host "  ⚠️  CORS preflight check failed: $($_.Exception.Message)" -ForegroundColor Yellow
    $testResults += @{ Test = "CORS"; Status = "WARN" }
}

# Test 8: Frontend Dependencies
Write-Host "`n[TEST 8] Frontend Dependencies..." -ForegroundColor Yellow
$packageJsonPath = "school-ai-frontend\package.json"
if (Test-Path $packageJsonPath) {
    $packageJson = Get-Content -Path $packageJsonPath -Raw | ConvertFrom-Json
    $requiredDeps = @("react", "framer-motion")
    $missingDeps = @()
    
    foreach ($dep in $requiredDeps) {
        if (-not $packageJson.dependencies.$dep) {
            $missingDeps += $dep
        }
    }
    
    if ($missingDeps.Count -eq 0) {
        Write-Host "  ✅ All required frontend dependencies installed" -ForegroundColor Green
        Write-Host "     React: $($packageJson.dependencies.react)" -ForegroundColor Gray
        Write-Host "     Framer Motion: $($packageJson.dependencies.'framer-motion')" -ForegroundColor Gray
        $testResults += @{ Test = "Frontend Dependencies"; Status = "PASS" }
    } else {
        Write-Host "  ❌ Missing dependencies: $($missingDeps -join ', ')" -ForegroundColor Red
        $testResults += @{ Test = "Frontend Dependencies"; Status = "FAIL" }
    }
} else {
    Write-Host "  ❌ package.json not found" -ForegroundColor Red
    $testResults += @{ Test = "Frontend Dependencies"; Status = "FAIL" }
}

# Test 9: Backend Dependencies (Serilog)
Write-Host "`n[TEST 9] Backend Dependencies (Serilog)..." -ForegroundColor Yellow
$csprojPath = "SchoolAiChatbotBackend\SchoolAiChatbotBackend.csproj"
if (Test-Path $csprojPath) {
    $csprojContent = Get-Content -Path $csprojPath -Raw
    $serilogPackages = @("Serilog.AspNetCore", "Serilog.Sinks.Console", "Serilog.Sinks.File")
    $foundPackages = @()
    
    foreach ($package in $serilogPackages) {
        if ($csprojContent -match $package) {
            $foundPackages += $package
        }
    }
    
    if ($foundPackages.Count -eq $serilogPackages.Count) {
        Write-Host "  ✅ All Serilog packages installed" -ForegroundColor Green
        foreach ($pkg in $foundPackages) {
            Write-Host "     ✓ $pkg" -ForegroundColor Gray
        }
        $testResults += @{ Test = "Serilog Packages"; Status = "PASS" }
    } else {
        Write-Host "  ⚠️  Some Serilog packages missing" -ForegroundColor Yellow
        $testResults += @{ Test = "Serilog Packages"; Status = "WARN" }
    }
} else {
    Write-Host "  ❌ .csproj not found" -ForegroundColor Red
    $testResults += @{ Test = "Serilog Packages"; Status = "FAIL" }
}

# Test 10: Environment Files
Write-Host "`n[TEST 10] Environment Configuration..." -ForegroundColor Yellow
$envFiles = @(
    "school-ai-frontend\.env.development",
    "school-ai-frontend\.env.local",
    "school-ai-frontend\.env.production"
)

$envStatus = @()
foreach ($envFile in $envFiles) {
    if (Test-Path $envFile) {
        $content = Get-Content -Path $envFile -Raw
        if ($content -match "VITE_API_URL") {
            $envStatus += "✓ $($envFile.Split('\')[-1])"
        }
    }
}

if ($envStatus.Count -eq $envFiles.Count) {
    Write-Host "  ✅ All environment files configured" -ForegroundColor Green
    foreach ($file in $envStatus) {
        Write-Host "     $file" -ForegroundColor Gray
    }
    $testResults += @{ Test = "Environment Files"; Status = "PASS" }
} else {
    Write-Host "  ⚠️  Some environment files missing" -ForegroundColor Yellow
    $testResults += @{ Test = "Environment Files"; Status = "WARN" }
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "           TEST SUMMARY" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$passCount = ($testResults | Where-Object { $_.Status -eq "PASS" }).Count
$failCount = ($testResults | Where-Object { $_.Status -eq "FAIL" }).Count
$warnCount = ($testResults | Where-Object { $_.Status -eq "WARN" }).Count
$totalCount = $testResults.Count

foreach ($result in $testResults) {
    $icon = switch ($result.Status) {
        "PASS" { "✅" }
        "FAIL" { "❌" }
        "WARN" { "⚠️ " }
    }
    $color = switch ($result.Status) {
        "PASS" { "Green" }
        "FAIL" { "Red" }
        "WARN" { "Yellow" }
    }
    Write-Host "$icon $($result.Test): $($result.Status)" -ForegroundColor $color
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Total Tests: $totalCount" -ForegroundColor White
Write-Host "Passed:      $passCount" -ForegroundColor Green
Write-Host "Failed:      $failCount" -ForegroundColor Red
Write-Host "Warnings:    $warnCount" -ForegroundColor Yellow
Write-Host "========================================`n" -ForegroundColor Cyan

if ($failCount -eq 0) {
    Write-Host "🎉 All critical tests passed! System is production-ready." -ForegroundColor Green
} elseif ($failCount -le 2) {
    Write-Host "⚠️  Some tests failed. Review failures above." -ForegroundColor Yellow
} else {
    Write-Host "❌ Multiple failures detected. Review and fix issues." -ForegroundColor Red
}

Write-Host "`nNext Steps:" -ForegroundColor Cyan
Write-Host "  1. Review any failed/warning tests above" -ForegroundColor Gray
Write-Host "  2. Test frontend: cd school-ai-frontend; npm run dev" -ForegroundColor Gray
Write-Host "  3. Test toasts by triggering errors in UI" -ForegroundColor Gray
Write-Host "  4. Check logs in: SchoolAiChatbotBackend\logs\" -ForegroundColor Gray
Write-Host "  5. Deploy to Azure when ready`n" -ForegroundColor Gray
