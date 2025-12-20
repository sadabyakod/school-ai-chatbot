# ============================================================
# Complete End-to-End Test: Exam Generation - Upload - Evaluation - Feedback
# ============================================================

param(
    [string]$BaseUrl = "http://localhost:8080"
)

Write-Host "`n" -NoNewline
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "     COMPLETE END-TO-END EXAM EVALUATION TEST                    " -ForegroundColor Cyan
Write-Host "     Generate - Upload - Status - Feedback with Step Analysis    " -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host ""

$studentId = "STU-TEST-$(Get-Random -Minimum 1000 -Maximum 9999)"
Write-Host "[INFO] Student ID: $studentId" -ForegroundColor Yellow
Write-Host "[INFO] Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host ""

# ============================================================
# STEP 1: Generate Exam with MCQ and Subjective Questions
# ============================================================
Write-Host "==================================================================" -ForegroundColor Magenta
Write-Host "STEP 1: GENERATE EXAM" -ForegroundColor Magenta
Write-Host "==================================================================" -ForegroundColor Magenta

$examRequest = @{
    subject = "Mathematics"
    chapter = "Differentiation"
    difficulty = "medium"
    mcqCount = 3
    subjectiveCount = 2
    studentClass = "12th Grade"
    board = "Karnataka PUC"
} | ConvertTo-Json

Write-Host "[...] Generating exam with 3 MCQ + 2 Subjective questions..." -ForegroundColor Yellow

try {
    $examResponse = Invoke-RestMethod -Uri "$BaseUrl/api/exam/generate" `
        -Method POST `
        -Body $examRequest `
        -ContentType "application/json"
    
    $examId = $examResponse.examId
    Write-Host "[OK] Exam Generated Successfully!" -ForegroundColor Green
    Write-Host "   Exam ID: $examId" -ForegroundColor White
    Write-Host "   Title: $($examResponse.title)" -ForegroundColor White
    
    # Show questions summary
    $mcqCount = 0
    $subjCount = 0
    foreach ($part in $examResponse.parts) {
        foreach ($q in $part.questions) {
            if ($part.questionType -match "MCQ") {
                $mcqCount++
            } else {
                $subjCount++
            }
        }
    }
    Write-Host "   MCQ Questions: $mcqCount" -ForegroundColor Cyan
    Write-Host "   Subjective Questions: $subjCount" -ForegroundColor Cyan
    Write-Host ""
}
catch {
    Write-Host "[FAIL] Failed to generate exam: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# ============================================================
# STEP 2: Create and Upload Mock Answer Sheet
# ============================================================
Write-Host "==================================================================" -ForegroundColor Magenta
Write-Host "STEP 2: UPLOAD ANSWER SHEET" -ForegroundColor Magenta
Write-Host "==================================================================" -ForegroundColor Magenta

# Create a mock answer file with some correct and some incorrect answers
$answerContent = @"
Student Answer Sheet
====================
Student ID: $studentId
Exam: $examId

MCQ Section:
1) B
2) A
3) C

Subjective Section:
Q1: To find the derivative of f(x) = x^3, I will use the power rule.
    The power rule states that d/dx(x^n) = n*x^(n-1)
    Therefore, f'(x) = 3*x^(3-1) = 3x^2
    Final Answer: f'(x) = 3x^2

Q2: [Not answered - student skipped this question]
"@

$answerFilePath = "$env:TEMP\test-answer-sheet-$studentId.txt"
$answerContent | Out-File -FilePath $answerFilePath -Encoding UTF8

Write-Host "[INFO] Created mock answer sheet at: $answerFilePath" -ForegroundColor Yellow
Write-Host "[...] Uploading answer sheet..." -ForegroundColor Yellow

try {
    # Use curl for multipart form upload
    $uploadResult = curl.exe -s -X POST "$BaseUrl/api/written-submission/submit-with-extraction" `
        -F "examId=$examId" `
        -F "studentId=$studentId" `
        -F "files=@$answerFilePath" | ConvertFrom-Json
    
    $submissionId = $uploadResult.writtenSubmissionId
    Write-Host "[OK] Answer Sheet Uploaded Successfully!" -ForegroundColor Green
    Write-Host "   Submission ID: $submissionId" -ForegroundColor White
    Write-Host "   Initial Status: $($uploadResult.status)" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host "[FAIL] Failed to upload answer sheet: $($_.Exception.Message)" -ForegroundColor Red
    # Clean up
    Remove-Item -Path $answerFilePath -Force -ErrorAction SilentlyContinue
    exit 1
}

# Clean up temp file
Remove-Item -Path $answerFilePath -Force -ErrorAction SilentlyContinue

# ============================================================
# STEP 3: Poll Status Until Evaluation Complete
# ============================================================
Write-Host "==================================================================" -ForegroundColor Magenta
Write-Host "STEP 3: MONITOR EVALUATION STATUS" -ForegroundColor Magenta
Write-Host "==================================================================" -ForegroundColor Magenta

$maxAttempts = 30
$attempt = 0
$isComplete = $false

Write-Host "[...] Waiting for evaluation to complete..." -ForegroundColor Yellow
Write-Host "   (Polling every 3 seconds, max $maxAttempts attempts)" -ForegroundColor Gray

while ($attempt -lt $maxAttempts -and -not $isComplete) {
    $attempt++
    
    try {
        $statusResponse = Invoke-RestMethod -Uri "$BaseUrl/api/written-submission/status/$submissionId" -Method GET
        
        $statusText = switch ($statusResponse.status) {
            0 { "PendingEvaluation" }
            1 { "OcrProcessing" }
            2 { "Completed" }
            3 { "Failed" }
            default { "Unknown ($($statusResponse.status))" }
        }
        
        $statusColor = "Yellow"
        if ($statusResponse.status -eq 2) { $statusColor = "Green" }
        elseif ($statusResponse.status -eq 3) { $statusColor = "Red" }
        
        Write-Host "   [$attempt/$maxAttempts] Status: $statusText" -ForegroundColor $statusColor
        
        if ($statusResponse.status -eq 2) {
            $isComplete = $true
            Write-Host "[OK] Evaluation Complete!" -ForegroundColor Green
            Write-Host "   Score: $($statusResponse.totalScore)/$($statusResponse.maxPossibleScore)" -ForegroundColor White
            Write-Host "   Percentage: $($statusResponse.percentage)%" -ForegroundColor White
            Write-Host "   Grade: $($statusResponse.grade)" -ForegroundColor White
            if ($statusResponse.evaluationResultBlobPath) {
                Write-Host "   Blob Path: $($statusResponse.evaluationResultBlobPath)" -ForegroundColor Gray
            }
        }
        elseif ($statusResponse.status -eq 3) {
            Write-Host "[FAIL] Evaluation Failed: $($statusResponse.errorMessage)" -ForegroundColor Red
            exit 1
        }
        else {
            Start-Sleep -Seconds 3
        }
    }
    catch {
        Write-Host "   [$attempt] Error checking status: $($_.Exception.Message)" -ForegroundColor Red
        Start-Sleep -Seconds 3
    }
}

if (-not $isComplete) {
    Write-Host "[FAIL] Timeout waiting for evaluation to complete" -ForegroundColor Red
    exit 1
}

Write-Host ""

# ============================================================
# STEP 4: Fetch Final Results with Feedback
# ============================================================
Write-Host "==================================================================" -ForegroundColor Magenta
Write-Host "STEP 4: FETCH STUDENT FEEDBACK AND RESULTS" -ForegroundColor Magenta
Write-Host "==================================================================" -ForegroundColor Magenta

try {
    $resultResponse = Invoke-RestMethod -Uri "$BaseUrl/api/exam/result/$examId/$studentId" -Method GET
    
    Write-Host ""
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host "                      EXAM RESULTS                                " -ForegroundColor Cyan
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host "  Exam ID: $($resultResponse.examId)" -ForegroundColor White
    Write-Host "  Student ID: $($resultResponse.studentId)" -ForegroundColor White
    Write-Host "------------------------------------------------------------------" -ForegroundColor Cyan
    Write-Host "  Grand Total: $($resultResponse.grandScore)/$($resultResponse.grandTotalMarks)" -ForegroundColor Yellow
    Write-Host "  Percentage: $($resultResponse.percentage)%" -ForegroundColor Yellow
    
    $gradeColor = "Green"
    if (-not $resultResponse.passed) { $gradeColor = "Red" }
    Write-Host "  Grade: $($resultResponse.grade)" -ForegroundColor $gradeColor
    
    $passText = "PASSED"
    if (-not $resultResponse.passed) { $passText = "FAILED" }
    Write-Host "  Status: $passText" -ForegroundColor $gradeColor
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host ""

    # MCQ RESULTS
    if ($resultResponse.mcqResults -and $resultResponse.mcqResults.Count -gt 0) {
        Write-Host "------------------------------------------------------------------" -ForegroundColor Blue
        Write-Host "  MCQ RESULTS: $($resultResponse.mcqScore)/$($resultResponse.mcqTotalMarks)" -ForegroundColor Blue
        Write-Host "------------------------------------------------------------------" -ForegroundColor Blue
        
        foreach ($mcq in $resultResponse.mcqResults) {
            $icon = "[CORRECT]"
            $color = "Green"
            if (-not $mcq.isCorrect) { 
                $icon = "[WRONG]"
                $color = "Red" 
            }
            
            Write-Host ""
            Write-Host "  Question: $($mcq.questionId)" -ForegroundColor White
            Write-Host "  $icon Student Answer: $($mcq.selectedOption)" -ForegroundColor $color
            Write-Host "     Correct Answer: $($mcq.correctAnswer)" -ForegroundColor Cyan
            Write-Host "     Marks: $($mcq.marksAwarded)" -ForegroundColor Yellow
        }
        Write-Host ""
    }

    # SUBJECTIVE RESULTS WITH STEP-BY-STEP ANALYSIS
    if ($resultResponse.subjectiveResults -and $resultResponse.subjectiveResults.Count -gt 0) {
        Write-Host "------------------------------------------------------------------" -ForegroundColor Magenta
        Write-Host "  SUBJECTIVE RESULTS: $($resultResponse.subjectiveScore)/$($resultResponse.subjectiveTotalMarks)" -ForegroundColor Magenta
        Write-Host "------------------------------------------------------------------" -ForegroundColor Magenta
        
        foreach ($subj in $resultResponse.subjectiveResults) {
            $icon = "[FULL MARKS]"
            $color = "Green"
            if (-not $subj.isFullyCorrect) { 
                $icon = "[PARTIAL]"
                if ($subj.earnedMarks -gt 0) { $color = "Yellow" } 
                else { $color = "Red" }
            }
            
            Write-Host ""
            Write-Host "  ================================================================" -ForegroundColor Gray
            Write-Host "  $icon Question $($subj.questionNumber): $($subj.questionId)" -ForegroundColor White
            Write-Host "  ================================================================" -ForegroundColor Gray
            
            # Question Text
            if ($subj.questionText) {
                $qText = $subj.questionText
                if ($qText.Length -gt 60) { 
                    $qText = $qText.Substring(0, 60) + "..." 
                }
                Write-Host "  Question: $qText" -ForegroundColor White
            }
            
            # Marks
            Write-Host "  Marks Awarded: $($subj.earnedMarks)/$($subj.maxMarks)" -ForegroundColor $color
            
            # Student Answer
            Write-Host ""
            Write-Host "  Student Answer:" -ForegroundColor Cyan
            if ($subj.studentAnswerEcho) {
                $answerLines = $subj.studentAnswerEcho -split "`n"
                foreach ($line in $answerLines) {
                    Write-Host "     $line" -ForegroundColor Gray
                }
            } else {
                Write-Host "     [Not answered]" -ForegroundColor Red
            }
            
            # Step-by-Step Analysis
            if ($subj.stepAnalysis -and $subj.stepAnalysis.Count -gt 0) {
                Write-Host ""
                Write-Host "  STEP-BY-STEP MARKING:" -ForegroundColor Yellow
                Write-Host "  +------+--------------------------------------+-------+--------+" -ForegroundColor DarkGray
                Write-Host "  | Step | Description                          | Marks | Status |" -ForegroundColor DarkGray
                Write-Host "  +------+--------------------------------------+-------+--------+" -ForegroundColor DarkGray
                
                foreach ($step in $subj.stepAnalysis) {
                    $stepIcon = "OK"
                    $stepColor = "Green"
                    if (-not $step.isCorrect) { 
                        $stepIcon = "X"
                        $stepColor = "Red" 
                    }
                    
                    $desc = $step.description
                    if ($desc.Length -gt 36) { 
                        $desc = $desc.Substring(0, 36) 
                    }
                    $desc = $desc.PadRight(36)
                    
                    $marks = "$($step.marksAwarded)/$($step.maxMarksForStep)".PadRight(5)
                    $stepNum = "$($step.step)".PadRight(4)
                    $status = $stepIcon.PadRight(6)
                    
                    Write-Host "  | $stepNum | $desc | $marks | " -NoNewline -ForegroundColor DarkGray
                    Write-Host "$status" -NoNewline -ForegroundColor $stepColor
                    Write-Host " |" -ForegroundColor DarkGray
                    
                    # Step feedback
                    if ($step.feedback -and $step.feedback.Length -gt 0) {
                        $fb = $step.feedback
                        if ($fb.Length -gt 50) { $fb = $fb.Substring(0, 50) + "..." }
                        Write-Host "  |      |   -> $fb" -ForegroundColor DarkGray
                    }
                }
                Write-Host "  +------+--------------------------------------+-------+--------+" -ForegroundColor DarkGray
            }
            
            # Overall Feedback (includes Model Answer if not fully correct)
            Write-Host ""
            Write-Host "  FEEDBACK:" -ForegroundColor Cyan
            if ($subj.overallFeedback) {
                $feedbackLines = $subj.overallFeedback -split "`n"
                foreach ($line in $feedbackLines) {
                    if ($line -match "Model Answer") {
                        Write-Host "     $line" -ForegroundColor Green
                    } else {
                        Write-Host "     $line" -ForegroundColor White
                    }
                }
            }
            
            # Expected Answer (always shown for reference if not fully correct)
            if ((-not $subj.isFullyCorrect) -and $subj.expectedAnswer) {
                Write-Host ""
                Write-Host "  EXPECTED ANSWER:" -ForegroundColor Green
                $expectedLines = $subj.expectedAnswer -split "`n"
                foreach ($line in $expectedLines) {
                    Write-Host "     $line" -ForegroundColor Green
                }
            }
        }
        Write-Host ""
    }

    # SUMMARY
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host "                         TEST COMPLETE                            " -ForegroundColor Cyan
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Summary:" -ForegroundColor White
    Write-Host "  [OK] Exam Generated: $examId" -ForegroundColor Green
    Write-Host "  [OK] Answer Sheet Uploaded: $submissionId" -ForegroundColor Green
    Write-Host "  [OK] Evaluation Completed" -ForegroundColor Green
    Write-Host "  [OK] Results Retrieved with Step-by-Step Feedback" -ForegroundColor Green
    Write-Host ""
    
    $finalColor = "Green"
    if (-not $resultResponse.passed) { $finalColor = "Red" }
    Write-Host "Final Score: $($resultResponse.grandScore)/$($resultResponse.grandTotalMarks) ($($resultResponse.percentage)%) - Grade: $($resultResponse.grade)" -ForegroundColor $finalColor
    Write-Host ""
}
catch {
    Write-Host "[FAIL] Failed to fetch results: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $_" -ForegroundColor Red
    exit 1
}

Write-Host "[DONE] End-to-End Test Completed Successfully!" -ForegroundColor Green
