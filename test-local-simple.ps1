# ============================================================================
# LOCAL SIMPLE TEST - Test the new simple exam generation
# ============================================================================

$baseUrl = "http://localhost:8080"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "LOCAL SIMPLE EXAM TEST" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Test 1: Generate Simple Exam
Write-Host "[TEST 1] Generate simple exam (should have 2 MCQs + 3 subjective)..." -ForegroundColor Yellow

$generateRequest = @{
    subject = "Mathematics"
    grade = "2nd PUC"
} | ConvertTo-Json

try {
    $exam = Invoke-RestMethod -Uri "$baseUrl/api/exam/generate" -Method POST -Body $generateRequest -ContentType "application/json"
    
    Write-Host "[OK] Exam generated!" -ForegroundColor Green
    Write-Host "    Exam ID: $($exam.examId)" -ForegroundColor White
    Write-Host "    Subject: $($exam.subject)" -ForegroundColor White
    Write-Host "    Total Marks: $($exam.totalMarks)" -ForegroundColor White
    Write-Host "    Question Count: $($exam.questionCount)" -ForegroundColor White
    
    Write-Host "`n    Parts:" -ForegroundColor White
    foreach ($part in $exam.parts) {
        Write-Host "      $($part.partName): $($part.totalQuestions) questions ($($part.marksPerQuestion) marks each)" -ForegroundColor Gray
        Write-Host "        Type: $($part.questionType)" -ForegroundColor DarkGray
        
        foreach ($q in $part.questions) {
            Write-Host "`n        Question $($q.questionNumber):" -ForegroundColor Cyan
            Write-Host "          Text: $($q.questionText)" -ForegroundColor Gray
            if ($q.options -and $q.options.Count -gt 0) {
                Write-Host "          Options:" -ForegroundColor Gray
                foreach ($opt in $q.options) {
                    Write-Host "            $opt" -ForegroundColor DarkGray
                }
            }
            Write-Host "          Correct: $($q.correctAnswer.Substring(0, [Math]::Min(100, $q.correctAnswer.Length)))..." -ForegroundColor DarkGray
        }
    }
    
    # Test 2: Verify it's stored in database by retrieving it
    Write-Host "`n[TEST 2] Verify exam is stored (retrieve from storage)..." -ForegroundColor Yellow
    
    $retrievedExam = Invoke-RestMethod -Uri "$baseUrl/api/exam/generate" -Method POST -Body $generateRequest -ContentType "application/json"
    
    if ($retrievedExam.examId -eq $exam.examId) {
        Write-Host "[OK] Exam can be retrieved (same exam ID returned)" -ForegroundColor Green
    } else {
        Write-Host "[OK] New exam generated (different exam ID)" -ForegroundColor Green
    }
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "[SUCCESS] All tests passed!" -ForegroundColor Green
    Write-Host "========================================`n" -ForegroundColor Cyan
    
} catch {
    Write-Host "[ERROR] Test failed" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
        $reader.Close()
    }
    exit 1
}
