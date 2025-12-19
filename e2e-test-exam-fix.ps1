# End-to-end test for exam flow
Write-Host "`n========== E2E EXAM FLOW TEST ==========" -ForegroundColor Cyan
$baseUrl = "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net"

# Step 1: Generate Exam
Write-Host "`n[Step 1] Generating exam..." -ForegroundColor Yellow
$body = '{"subject":"Math","grade":"10","chapter":"Algebra","questionCount":3,"useCache":false,"fastMode":true}'
try {
    $exam = Invoke-RestMethod -Uri "$baseUrl/api/exam-generator/generate-exam" -Method POST -ContentType "application/json" -Body $body -TimeoutSec 120
    Write-Host "SUCCESS: Exam generated - ID: $($exam.examId)" -ForegroundColor Green
    $examId = $exam.examId
} catch {
    Write-Host "FAILED: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Verify exam exists
Write-Host "`n[Step 2] Verifying exam exists..." -ForegroundColor Yellow
try {
    $retrieved = Invoke-RestMethod -Uri "$baseUrl/api/exam/$examId" -Method GET -TimeoutSec 30
    Write-Host "SUCCESS: Exam retrieved - Subject: $($retrieved.subject)" -ForegroundColor Green
} catch {
    Write-Host "FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 3: Test upload-written
Write-Host "`n[Step 3] Testing upload-written endpoint..." -ForegroundColor Yellow
$studentId = "e2e-test-" + (Get-Random)
$imagePath = "C:\school-ai-chatbot\test-answer-sheet.png"

Add-Type -AssemblyName System.Net.Http
$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromMinutes(2)

$content = [System.Net.Http.MultipartFormDataContent]::new()
$content.Add([System.Net.Http.StringContent]::new($examId), "examId")
$content.Add([System.Net.Http.StringContent]::new($studentId), "studentId")

$fileBytes = [System.IO.File]::ReadAllBytes($imagePath)
$fileContent = [System.Net.Http.ByteArrayContent]::new($fileBytes)
$fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("image/png")
$content.Add($fileContent, "files", "test-answer-sheet.png")

$response = $client.PostAsync("$baseUrl/api/exam-submission/upload-written", $content).GetAwaiter().GetResult()
$responseBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

if ($response.IsSuccessStatusCode) {
    Write-Host "SUCCESS: Upload completed!" -ForegroundColor Green
    Write-Host "Response: $responseBody"
} else {
    Write-Host "Status: $($response.StatusCode)" -ForegroundColor Red
    Write-Host "Response: $responseBody"
}

$client.Dispose()
Write-Host "`n========== TEST COMPLETE ==========" -ForegroundColor Cyan
