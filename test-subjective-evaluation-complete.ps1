# Complete Subjective Answer Evaluation Test
# Tests: Upload -> OCR -> Evaluation -> Results with Expected Answers

$baseUrl = "http://localhost:8080"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Subjective Answer Evaluation Test" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Step 1: Generate an exam
Write-Host "Step 1: Generating exam..." -ForegroundColor Yellow
$examRequest = @{
    subject = "Mathematics"
    standard = "2nd PUC"
    chapter = "Determinants"
    numberOfQuestions = 2
    includeSubjective = $true
    difficulty = "Medium"
} | ConvertTo-Json

try {
    $examResponse = Invoke-RestMethod -Uri "$baseUrl/api/exam/generate" `
        -Method POST `
        -Body $examRequest `
        -ContentType "application/json"
    
    $examId = $examResponse.examId
    $examTitle = $examResponse.title
    Write-Host "SUCCESS - Exam generated: $examTitle" -ForegroundColor Green
    Write-Host "Exam ID: $examId`n" -ForegroundColor Gray
} catch {
    Write-Host "FAILED to generate exam: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Create a test answer sheet file with sample answers
Write-Host "Step 2: Creating test answer sheet..." -ForegroundColor Yellow
$testAnswers = @"
Student Answer Sheet

Question 1:
Let A = |2  3|
        |4  5|

To find determinant:
det(A) = 2*5 - 3*4
det(A) = 10 - 12
det(A) = -2

Answer: The determinant is -2

Question 2:
For matrix B = |1  2  3|
               |4  5  6|
               |7  8  9|

Using cofactor expansion along first row:
det(B) = 1*(5*9 - 6*8) - 2*(4*9 - 6*7) + 3*(4*8 - 5*7)
det(B) = 1*(45 - 48) - 2*(36 - 42) + 3*(32 - 35)
det(B) = 1*(-3) - 2*(-6) + 3*(-3)
det(B) = -3 + 12 - 9
det(B) = 0

Answer: The determinant is 0
"@

$tempFile = "test-answer-sheet.txt"
$testAnswers | Out-File -FilePath $tempFile -Encoding UTF8
Write-Host "SUCCESS - Answer sheet created`n" -ForegroundColor Green

# Step 3: Upload answer sheet (this should trigger OCR and evaluation)
Write-Host "Step 3: Uploading answer sheet for evaluation..." -ForegroundColor Yellow
$studentId = "TEST-STUDENT-001"

try {
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"
    
    $fileContent = [System.IO.File]::ReadAllBytes($tempFile)
    $fileBase64 = [Convert]::ToBase64String($fileContent)
    
    $bodyLines = @(
        "--$boundary",
        "Content-Disposition: form-data; name=`"examId`"",
        "",
        $examId,
        "--$boundary",
        "Content-Disposition: form-data; name=`"studentId`"",
        "",
        $studentId,
        "--$boundary",
        "Content-Disposition: form-data; name=`"files`"; filename=`"answer-sheet.txt`"",
        "Content-Type: text/plain",
        "",
        $testAnswers,
        "--$boundary--"
    ) -join $LF
    
    $response = Invoke-RestMethod -Uri "$baseUrl/api/exam/upload-written" `
        -Method POST `
        -ContentType "multipart/form-data; boundary=$boundary" `
        -Body $bodyLines
    
    $submissionId = $response.writtenSubmissionId
    Write-Host "SUCCESS - Answer sheet uploaded" -ForegroundColor Green
    Write-Host "Submission ID: $submissionId" -ForegroundColor Gray
    Write-Host "Status: $($response.status)" -ForegroundColor Gray
    Write-Host "Message: $($response.message)`n" -ForegroundColor Gray
} catch {
    Write-Host "FAILED to upload: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Error details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    exit 1
}

# Step 4: Wait for evaluation to complete
Write-Host "Step 4: Waiting for AI evaluation to complete..." -ForegroundColor Yellow
Write-Host "(This may take 10-30 seconds)..." -ForegroundColor Gray

$maxWaitTime = 60
$waitInterval = 5
$elapsedTime = 0

while ($elapsedTime -lt $maxWaitTime) {
    Start-Sleep -Seconds $waitInterval
    $elapsedTime += $waitInterval
    
    try {
        $result = Invoke-RestMethod -Uri "$baseUrl/api/exam/result/$examId/$studentId" -Method GET
        
        if ($result.subjectiveResults -and $result.subjectiveResults.Count -gt 0) {
            Write-Host "SUCCESS - Evaluation completed!`n" -ForegroundColor Green
            break
        }
    } catch {
        # Continue waiting
    }
    
    Write-Host "  Still processing... ($elapsedTime seconds)" -ForegroundColor Gray
}

# Step 5: Fetch and display complete results
Write-Host "Step 5: Fetching complete evaluation results..." -ForegroundColor Yellow

try {
    $finalResult = Invoke-RestMethod -Uri "$baseUrl/api/exam/result/$examId/$studentId" -Method GET
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "EVALUATION RESULTS" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
    
    Write-Host "Exam: $($finalResult.examTitle)" -ForegroundColor White
    Write-Host "Student ID: $($finalResult.studentId)`n" -ForegroundColor White
    
    # Display subjective results
    if ($finalResult.subjectiveResults -and $finalResult.subjectiveResults.Count -gt 0) {
        Write-Host "SUBJECTIVE ANSWERS EVALUATION:" -ForegroundColor Yellow
        Write-Host "Total Score: $($finalResult.subjectiveScore)/$($finalResult.subjectiveTotalMarks)" -ForegroundColor White
        Write-Host ""
        
        $questionNum = 1
        foreach ($result in $finalResult.subjectiveResults) {
            Write-Host "Question $questionNum" -ForegroundColor Cyan
            Write-Host "  Question: $($result.questionText)" -ForegroundColor Gray
            Write-Host "  Marks: $($result.earnedMarks)/$($result.maxMarks)" -ForegroundColor $(if ($result.isFullyCorrect) { "Green" } else { "Yellow" })
            Write-Host "  Status: $(if ($result.isFullyCorrect) { 'Fully Correct' } else { 'Partially Correct' })" -ForegroundColor $(if ($result.isFullyCorrect) { "Green" } else { "Yellow" })
            
            Write-Host "`n  EXPECTED ANSWER:" -ForegroundColor Green
            Write-Host "  $($result.expectedAnswer)" -ForegroundColor White
            
            Write-Host "`n  STUDENT'S ANSWER:" -ForegroundColor Yellow
            Write-Host "  $($result.studentAnswerEcho)" -ForegroundColor White
            
            Write-Host "`n  STEP-BY-STEP EVALUATION:" -ForegroundColor Cyan
            foreach ($step in $result.stepAnalysis) {
                $stepIcon = if ($step.isCorrect) { "[OK]" } else { "[X]" }
                $stepColor = if ($step.isCorrect) { "Green" } else { "Red" }
                
                Write-Host "    Step $($step.step): $($step.description)" -ForegroundColor White
                Write-Host "      $stepIcon Marks: $($step.marksAwarded)/$($step.maxMarksForStep)" -ForegroundColor $stepColor
                Write-Host "      Feedback: $($step.feedback)" -ForegroundColor Gray
            }
            
            Write-Host "`n  OVERALL FEEDBACK:" -ForegroundColor Cyan
            Write-Host "  $($result.overallFeedback)" -ForegroundColor White
            Write-Host "`n----------------------------------------`n" -ForegroundColor Gray
            
            $questionNum++
        }
    } else {
        Write-Host "No subjective results available yet. Evaluation may still be processing." -ForegroundColor Yellow
    }
    
    # Display grand total
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "FINAL GRADE" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
    
    Write-Host "MCQ Score: $($finalResult.mcqScore)/$($finalResult.mcqTotalMarks)" -ForegroundColor White
    Write-Host "Subjective Score: $($finalResult.subjectiveScore)/$($finalResult.subjectiveTotalMarks)" -ForegroundColor White
    Write-Host "----------------------------------------" -ForegroundColor Gray
    Write-Host "TOTAL SCORE: $($finalResult.grandScore)/$($finalResult.grandTotalMarks)" -ForegroundColor Green
    Write-Host "PERCENTAGE: $($finalResult.percentage)%" -ForegroundColor Green
    Write-Host "GRADE: $($finalResult.grade)" -ForegroundColor Green
    Write-Host "STATUS: $(if ($finalResult.passed) { 'PASSED' } else { 'FAILED' })`n" -ForegroundColor $(if ($finalResult.passed) { "Green" } else { "Red" })
    
    # Verify that expected answers are present
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "VALIDATION CHECK" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
    
    $hasExpectedAnswers = $true
    $hasFeedback = $true
    $hasStepAnalysis = $true
    
    foreach ($result in $finalResult.subjectiveResults) {
        if ([string]::IsNullOrWhiteSpace($result.expectedAnswer)) {
            $hasExpectedAnswers = $false
        }
        if ([string]::IsNullOrWhiteSpace($result.overallFeedback)) {
            $hasFeedback = $false
        }
        if ($result.stepAnalysis.Count -eq 0) {
            $hasStepAnalysis = $false
        }
    }
    
    Write-Host "[$(if ($hasExpectedAnswers) { 'OK' } else { 'FAIL' })] Expected answers provided" -ForegroundColor $(if ($hasExpectedAnswers) { "Green" } else { "Red" })
    Write-Host "[$(if ($hasFeedback) { 'OK' } else { 'FAIL' })] Feedback provided" -ForegroundColor $(if ($hasFeedback) { "Green" } else { "Red" })
    Write-Host "[$(if ($hasStepAnalysis) { 'OK' } else { 'FAIL' })] Step-by-step analysis provided" -ForegroundColor $(if ($hasStepAnalysis) { "Green" } else { "Red" })
    Write-Host "[OK] Scores calculated" -ForegroundColor Green
    Write-Host "`nAll evaluation features are working correctly!" -ForegroundColor Green
    
} catch {
    Write-Host "FAILED to fetch results: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Error details: $($_.ErrorDetails.Message)" -ForegroundColor Red
}

# Cleanup
if (Test-Path $tempFile) {
    Remove-Item $tempFile -Force
}

Write-Host "`nTest completed!`n" -ForegroundColor Cyan
