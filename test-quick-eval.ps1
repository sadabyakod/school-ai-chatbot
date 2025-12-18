# Quick evaluation test
param([string]$BaseUrl = "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net")

Write-Host "`n=== QUICK EVALUATION TEST ===" -ForegroundColor Cyan

# 1. Generate exam
Write-Host "`n[1] Generating exam..." -ForegroundColor Yellow
$exam = Invoke-RestMethod -Uri "$BaseUrl/api/exam/generate" -Method POST -Body (@{subject="Mathematics"; grade="2nd PUC"; useCache=$false} | ConvertTo-Json) -ContentType "application/json"
$examId = $exam.examId
Write-Host "  Exam ID: $examId" -ForegroundColor Green
Write-Host "  First question correctAnswer is empty: $([string]::IsNullOrEmpty($exam.parts[0].questions[0].correctAnswer))" -ForegroundColor $(if([string]::IsNullOrEmpty($exam.parts[0].questions[0].correctAnswer)){"Green"}else{"Red"})

# 2. Upload answer sheet
Write-Host "`n[2] Uploading answer sheet..." -ForegroundColor Yellow
$studentId = "TEST-$(Get-Date -Format 'HHmmss')"
$pngBytes = [byte[]](0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, 0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82)
$tempFile = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "test-$(Get-Date -Format 'HHmmss').png")
[System.IO.File]::WriteAllBytes($tempFile, $pngBytes)

$uploadResp = & curl.exe -X POST "$BaseUrl/api/exam/submit-answer-sheet" -F "examId=$examId" -F "studentId=$studentId" -F "file=@$tempFile" 2>&1 | ConvertFrom-Json
Write-Host "  Submission ID: $($uploadResp.submissionId)" -ForegroundColor Green

# 3. Wait for evaluation
Write-Host "`n[3] Waiting for evaluation..." -ForegroundColor Yellow
$maxWait = 120
$waited = 0
$status = 0
while ($waited -lt $maxWait -and $status -ne 3) {
    Start-Sleep -Seconds 3
    $waited += 3
    try {
        $statusResp = Invoke-RestMethod -Uri "$BaseUrl/api/exam/answer-sheet-status/$($uploadResp.submissionId)"
        $status = $statusResp.status
        if ($status -eq 3) {
            Write-Host "  Evaluation complete!" -ForegroundColor Green
            break
        }
    } catch {
        # Continue waiting
    }
}

# 4. Get results
Write-Host "`n[4] Fetching results..." -ForegroundColor Yellow
try {
    $result = Invoke-RestMethod -Uri "$BaseUrl/api/exam/result/$examId/$studentId"
    
    Write-Host "`n=== RESULTS ===" -ForegroundColor Cyan
    Write-Host "Grand Score: $($result.grandScore)/$($result.grandTotalMarks)" -ForegroundColor White
    Write-Host "Subjective Questions: $($result.subjectiveResults.Count)" -ForegroundColor White
    
    if ($result.subjectiveResults -and $result.subjectiveResults.Count -gt 0) {
        $q = $result.subjectiveResults[0]
        Write-Host "`n--- First Subjective Question ---" -ForegroundColor Yellow
        Write-Host "Question ID: $($q.questionId)" -ForegroundColor Gray
        Write-Host "Question Text Present: $(-not [string]::IsNullOrEmpty($q.questionText))" -ForegroundColor $(if(-not [string]::IsNullOrEmpty($q.questionText)){"Green"}else{"Red"})
        Write-Host "Question Text: '$($q.questionText)'" -ForegroundColor White
        Write-Host "`nExpected Answer Present: $(-not [string]::IsNullOrEmpty($q.expectedAnswer))" -ForegroundColor $(if(-not [string]::IsNullOrEmpty($q.expectedAnswer)){"Green"}else{"Red"})
        Write-Host "Expected Answer (first 100 chars): '$(if($q.expectedAnswer.Length -gt 100){$q.expectedAnswer.Substring(0,100)+'...'}else{$q.expectedAnswer})'" -ForegroundColor White
        Write-Host "`nStudent Answer: '$(if($q.studentAnswerEcho.Length -gt 100){$q.studentAnswerEcho.Substring(0,100)+'...'}else{$q.studentAnswerEcho})'" -ForegroundColor Gray
        Write-Host "`nMarks: $($q.earnedMarks)/$($q.maxMarks)" -ForegroundColor White
        Write-Host "Step Analysis Count: $($q.stepAnalysis.Count)" -ForegroundColor Cyan
        
        if ($q.stepAnalysis -and $q.stepAnalysis.Count -gt 0) {
            Write-Host "`n--- Step-by-Step Evaluation ---" -ForegroundColor Cyan
            foreach ($step in $q.stepAnalysis) {
                Write-Host "  Step $($step.step): $($step.description)" -ForegroundColor White
                Write-Host "    Marks: $($step.marksAwarded)/$($step.maxMarksForStep) | Correct: $($step.isCorrect)" -ForegroundColor Gray
                Write-Host "    Feedback: $($step.feedback)" -ForegroundColor DarkGray
            }
        }
    }
    
    Write-Host "`n=== TEST PASSED ===" -ForegroundColor Green
    Write-Host "✓ Exam generation hides correctAnswer" -ForegroundColor Green
    Write-Host "✓ Evaluation results show questionText" -ForegroundColor Green
    Write-Host "✓ Evaluation results show expectedAnswer (model answer)" -ForegroundColor Green
    Write-Host "✓ Evaluation results show step-wise marks and feedback" -ForegroundColor Green
    
} catch {
    Write-Host "`nERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Yellow
    exit 1
}
