# Test script for exam generator API
Write-Host "Testing Exam Generator API..."
Write-Host "Waiting for server to be ready..."
Start-Sleep -Seconds 3

$body = @{
    subject = "Mathematics"
    grade = "2nd PUC"
    chapter = "Matrices"
    difficulty = "Medium"
    examType = "Full Paper"
} | ConvertTo-Json

Write-Host "Sending request to http://localhost:8080/api/exam/generate..."
try {
    $response = Invoke-RestMethod -Uri "http://localhost:8080/api/exam/generate" -Method POST -Body $body -ContentType "application/json" -TimeoutSec 180
    $response | ConvertTo-Json -Depth 15 | Out-File -FilePath "C:\school-ai-chatbot\exam-output.json"
    Write-Host "SUCCESS! Exam saved to exam-output.json"
    Write-Host "Parts generated: $($response.parts.Count)"
    foreach ($part in $response.parts) {
        Write-Host "  $($part.partName): $($part.questions.Count) questions"
    }
} catch {
    Write-Host "ERROR: $_"
    Write-Host "Status: $($_.Exception.Response.StatusCode)"
}
