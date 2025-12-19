$baseUrl = "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net"
Write-Host "Testing exam generation and upload sync..." -ForegroundColor Cyan

# Generate exam
$examBody = '{"subject":"Mathematics","grade":"2nd PUC"}'
$examResponse = Invoke-RestMethod -Uri "$baseUrl/api/exam/generate" -Method POST -Body $examBody -ContentType "application/json" -TimeoutSec 120
Write-Host "Generated ExamId: $($examResponse.examId)" -ForegroundColor Green

if ($examResponse._storageWarning) {
    Write-Host "Storage Warning: $($examResponse._storageWarning)" -ForegroundColor Red
} else {
    Write-Host "Storage: OK (no warning)" -ForegroundColor Green
}

# Test retrieval
$retrieved = Invoke-RestMethod -Uri "$baseUrl/api/exam/$($examResponse.examId)" -Method GET
Write-Host "Retrieved OK: $($retrieved.examId)" -ForegroundColor Green
