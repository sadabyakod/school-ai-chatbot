# ============================================================================
# End-to-End Answer Sheet Evaluation Test
# Tests the complete flow: Generate Exam -> Upload Answer -> Poll Status -> Get Results
# ============================================================================

param(
    [string]$BaseUrl = "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net",
    [int]$MaxPollAttempts = 60,
    [int]$PollIntervalSeconds = 3
)

# Colors for output
function Write-Step { param($msg) Write-Host "`n━━━ $msg ━━━" -ForegroundColor Cyan }
function Write-Success { param($msg) Write-Host "✅ $msg" -ForegroundColor Green }
function Write-Error { param($msg) Write-Host "❌ $msg" -ForegroundColor Red }
function Write-Info { param($msg) Write-Host "ℹ️  $msg" -ForegroundColor Gray }
function Write-Progress { param($msg) Write-Host "⏳ $msg" -ForegroundColor Yellow }

Write-Host @"

╔══════════════════════════════════════════════════════════════════════╗
║     SMARTSTUDY ANSWER SHEET EVALUATION - END-TO-END TEST             ║
║                                                                      ║
║  Tests: Upload → OCR → AI Evaluation → Status → Feedback → Score    ║
╚══════════════════════════════════════════════════════════════════════╝

"@ -ForegroundColor Cyan

Write-Info "Base URL: $BaseUrl"
Write-Info "Max Poll Attempts: $MaxPollAttempts"
Write-Info "Poll Interval: ${PollIntervalSeconds}s"

$testResults = @{
    ExamGenerated = $false
    AnswerUploaded = $false
    StatusPolled = $false
    EvaluationCompleted = $false
    FeedbackReceived = $false
    ScoreCalculated = $false
}

# ============================================================================
# STEP 1: Generate Exam
# ============================================================================
Write-Step "STEP 1: Generate Karnataka 2nd PUC Mathematics Exam"

$examRequest = @{
    subject = "Mathematics"
    grade = "2nd PUC"
    chapter = "Differentiation"
    difficulty = "Medium"
    examType = "Mixed"
    numberOfMcq = 2
    numberOfSubjective = 2
} | ConvertTo-Json

try {
    Write-Progress "Generating exam..."
    $examResponse = Invoke-RestMethod -Uri "$BaseUrl/api/exam/generate" `
        -Method POST `
        -Body $examRequest `
        -ContentType "application/json" `
        -TimeoutSec 120

    $examId = $examResponse.examId
    $testResults.ExamGenerated = $true
    
    Write-Success "Exam generated successfully!"
    Write-Info "Exam ID: $examId"
    Write-Info "Subject: $($examResponse.subject)"
    Write-Info "Total Questions: $($examResponse.questions.Count)"
    Write-Info "Total Marks: $($examResponse.totalMarks)"
    
    # Display questions
    Write-Host "`nQuestions:" -ForegroundColor White
    foreach ($q in $examResponse.questions) {
        Write-Host "  - [$($q.questionId)] $($q.questionText.Substring(0, [Math]::Min(60, $q.questionText.Length)))..." -ForegroundColor Gray
    }
} catch {
    Write-Error "Failed to generate exam: $($_.Exception.Message)"
    Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    exit 1
}

# ============================================================================
# STEP 2: Create Sample Answer Sheet
# ============================================================================
Write-Step "STEP 2: Create Sample Student Answer Sheet"

$studentId = "TEST-STUDENT-$(Get-Date -Format 'yyyyMMddHHmmss')"
Write-Info "Student ID: $studentId"

# Create a sample answer file with mathematical content
$sampleAnswers = @"
STUDENT ANSWER SHEET
====================
Student ID: $studentId
Exam ID: $examId
Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm')

SECTION A - MCQ ANSWERS
-----------------------
Q1: B
Q2: A

SECTION B - SUBJECTIVE ANSWERS
------------------------------

Question 1: Find the derivative of f(x) = x^3 + 2x^2 - 5x + 3

Solution:
Given: f(x) = x^3 + 2x^2 - 5x + 3

Using the power rule of differentiation:
d/dx[x^n] = n*x^(n-1)

Step 1: Differentiate each term
- d/dx[x^3] = 3x^2
- d/dx[2x^2] = 4x  
- d/dx[-5x] = -5
- d/dx[3] = 0

Step 2: Combine the results
f'(x) = 3x^2 + 4x - 5

Answer: The derivative is f'(x) = 3x^2 + 4x - 5

---

Question 2: Prove that the derivative of sin(x) is cos(x)

Solution:
We need to prove: d/dx[sin(x)] = cos(x)

Using the first principle of differentiation:
f'(x) = lim(h→0) [f(x+h) - f(x)] / h

Step 1: Apply to sin(x)
d/dx[sin(x)] = lim(h→0) [sin(x+h) - sin(x)] / h

Step 2: Use the sum formula
sin(x+h) = sin(x)cos(h) + cos(x)sin(h)

Step 3: Substitute
= lim(h→0) [sin(x)cos(h) + cos(x)sin(h) - sin(x)] / h
= lim(h→0) [sin(x)(cos(h) - 1) + cos(x)sin(h)] / h

Step 4: Separate the limits
= sin(x) * lim(h→0)[(cos(h)-1)/h] + cos(x) * lim(h→0)[sin(h)/h]

Step 5: Apply standard limits
lim(h→0)[sin(h)/h] = 1
lim(h→0)[(cos(h)-1)/h] = 0

Step 6: Final result
= sin(x) * 0 + cos(x) * 1
= cos(x)

Therefore, d/dx[sin(x)] = cos(x) (Proved)

---
END OF ANSWER SHEET
"@

$tempAnswerFile = Join-Path $env:TEMP "student-answer-sheet.txt"
$sampleAnswers | Out-File -FilePath $tempAnswerFile -Encoding UTF8
Write-Success "Answer sheet created: $tempAnswerFile"

# ============================================================================
# STEP 3: Upload Answer Sheet
# ============================================================================
Write-Step "STEP 3: Upload Answer Sheet for Evaluation"

try {
    Write-Progress "Uploading answer sheet..."
    
    # Create multipart form data
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"
    
    $fileContent = Get-Content $tempAnswerFile -Raw
    
    $bodyLines = @(
        "--$boundary",
        "Content-Disposition: form-data; name=`"examId`"$LF",
        $examId,
        "--$boundary",
        "Content-Disposition: form-data; name=`"studentId`"$LF",
        $studentId,
        "--$boundary",
        "Content-Disposition: form-data; name=`"files`"; filename=`"answer-sheet.txt`"",
        "Content-Type: text/plain$LF",
        $fileContent,
        "--$boundary--$LF"
    ) -join $LF

    $uploadResponse = Invoke-RestMethod -Uri "$BaseUrl/api/exam/upload-written" `
        -Method POST `
        -ContentType "multipart/form-data; boundary=$boundary" `
        -Body $bodyLines `
        -TimeoutSec 60

    $submissionId = $uploadResponse.writtenSubmissionId
    $testResults.AnswerUploaded = $true
    
    Write-Success "Answer sheet uploaded successfully!"
    Write-Info "Submission ID: $submissionId"
    Write-Info "Status: $($uploadResponse.status)"
    Write-Info "Message: $($uploadResponse.message)"
    
} catch {
    Write-Error "Failed to upload answer sheet: $($_.Exception.Message)"
    
    # Check if it's a 409 Conflict (duplicate submission)
    if ($_.Exception.Response.StatusCode -eq 409) {
        Write-Info "Duplicate submission detected. Fetching existing submission..."
        $errorBody = $_.ErrorDetails.Message | ConvertFrom-Json
        $submissionId = $errorBody.existingSubmissionId
        Write-Info "Using existing Submission ID: $submissionId"
        $testResults.AnswerUploaded = $true
    } else {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
        exit 1
    }
}

# ============================================================================
# STEP 4: Poll for Evaluation Status
# ============================================================================
Write-Step "STEP 4: Poll for Evaluation Status (with Progress Updates)"

$attempt = 0
$statusHistory = @()
$finalStatus = $null

Write-Progress "Starting status polling..."
Write-Host ""

while ($attempt -lt $MaxPollAttempts) {
    $attempt++
    
    try {
        $statusResponse = Invoke-RestMethod -Uri "$BaseUrl/api/exam/submission-status/$submissionId" `
            -Method GET `
            -TimeoutSec 30
        
        $currentStatus = $statusResponse.status
        $statusMessage = $statusResponse.statusMessage
        $testResults.StatusPolled = $true
        
        # Track status changes
        if ($statusHistory.Count -eq 0 -or $statusHistory[-1] -ne $currentStatus) {
            $statusHistory += $currentStatus
            $timestamp = Get-Date -Format 'HH:mm:ss'
            
            # Display status with appropriate icon
            switch ($currentStatus) {
                "PendingEvaluation" { 
                    Write-Host "  [$timestamp] ⏳ $statusMessage" -ForegroundColor Yellow 
                }
                "OcrProcessing" { 
                    Write-Host "  [$timestamp] 📄 $statusMessage" -ForegroundColor Blue 
                }
                "Evaluating" { 
                    Write-Host "  [$timestamp] 🤖 $statusMessage" -ForegroundColor Magenta 
                }
                "Completed" { 
                    Write-Host "  [$timestamp] ✅ $statusMessage" -ForegroundColor Green 
                    $testResults.EvaluationCompleted = $true
                }
                "Failed" { 
                    Write-Host "  [$timestamp] ❌ $statusMessage" -ForegroundColor Red 
                }
                default { 
                    Write-Host "  [$timestamp] ❓ $currentStatus - $statusMessage" -ForegroundColor Gray 
                }
            }
        }
        
        # Check if complete or failed
        if ($statusResponse.isComplete -eq $true -or $currentStatus -eq "Completed") {
            $finalStatus = $statusResponse
            Write-Host ""
            Write-Success "Evaluation completed!"
            break
        }
        
        if ($currentStatus -eq "Failed") {
            Write-Host ""
            Write-Error "Evaluation failed!"
            $finalStatus = $statusResponse
            break
        }
        
    } catch {
        Write-Host "  [Attempt $attempt] Error polling status: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Wait before next poll
    Start-Sleep -Seconds $PollIntervalSeconds
}

if ($attempt -ge $MaxPollAttempts) {
    $timeoutMsg = "Timeout waiting for evaluation after $MaxPollAttempts attempts"
    Write-Error $timeoutMsg
}

Write-Info "Status History: $($statusHistory -join ' -> ')"

# ============================================================================
# STEP 5: Get Full Results with Feedback
# ============================================================================
Write-Step "STEP 5: Get Full Evaluation Results with Feedback"

try {
    Write-Progress "Fetching detailed results..."
    
    $resultResponse = Invoke-RestMethod -Uri "$BaseUrl/api/exam/result/$examId/$studentId" `
        -Method GET `
        -TimeoutSec 30
    
    $testResults.FeedbackReceived = $true
    $testResults.ScoreCalculated = $true
    
    Write-Success "Results retrieved successfully!"
    Write-Host ""
    
    # ─────────────────────────────────────────────────────────────
    # Score Summary
    # ─────────────────────────────────────────────────────────────
    Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║                    EXAM RESULTS                          ║" -ForegroundColor Cyan
    Write-Host "╠══════════════════════════════════════════════════════════╣" -ForegroundColor Cyan
    Write-Host "║  Exam: $($resultResponse.examTitle.PadRight(46))   ║" -ForegroundColor White
    Write-Host "║  Student: $($studentId.PadRight(43))   ║" -ForegroundColor White
    Write-Host "╠══════════════════════════════════════════════════════════╣" -ForegroundColor Cyan
    
    # MCQ Score
    $mcqPct = if ($resultResponse.mcqTotalMarks -gt 0) { 
        [math]::Round($resultResponse.mcqScore / $resultResponse.mcqTotalMarks * 100, 1) 
    } else { 0 }
    Write-Host "║  MCQ Score:        $($resultResponse.mcqScore)/$($resultResponse.mcqTotalMarks) ($mcqPct%)".PadRight(57) + "║" -ForegroundColor White
    
    # Subjective Score
    $subPct = if ($resultResponse.subjectiveTotalMarks -gt 0) { 
        [math]::Round($resultResponse.subjectiveScore / $resultResponse.subjectiveTotalMarks * 100, 1) 
    } else { 0 }
    Write-Host "║  Subjective Score: $($resultResponse.subjectiveScore)/$($resultResponse.subjectiveTotalMarks) ($subPct%)".PadRight(57) + "║" -ForegroundColor White
    
    Write-Host "╠══════════════════════════════════════════════════════════╣" -ForegroundColor Cyan
    
    # Grand Total
    $gradeColor = switch ($resultResponse.grade) {
        "A+" { "Green" }
        "A"  { "Green" }
        "B+" { "Yellow" }
        "B"  { "Yellow" }
        "C+" { "Yellow" }
        "C"  { "White" }
        "D"  { "White" }
        default { "Red" }
    }
    
    Write-Host "║  GRAND TOTAL:      $($resultResponse.grandScore)/$($resultResponse.grandTotalMarks)".PadRight(57) + "║" -ForegroundColor $gradeColor
    Write-Host "║  PERCENTAGE:       $($resultResponse.percentage)%".PadRight(57) + "║" -ForegroundColor $gradeColor
    Write-Host "║  GRADE:            $($resultResponse.grade)".PadRight(57) + "║" -ForegroundColor $gradeColor
    
    $passStatus = if ($resultResponse.passed) { "✅ PASSED" } else { "❌ FAILED" }
    Write-Host "║  STATUS:           $passStatus".PadRight(57) + "║" -ForegroundColor $(if ($resultResponse.passed) { "Green" } else { "Red" })
    
    Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    
    # ─────────────────────────────────────────────────────────────
    # MCQ Results
    # ─────────────────────────────────────────────────────────────
    if ($resultResponse.mcqResults -and $resultResponse.mcqResults.Count -gt 0) {
        Write-Host "`n📝 MCQ RESULTS:" -ForegroundColor White
        Write-Host "─────────────────────────────────────────────────────" -ForegroundColor Gray
        
        foreach ($mcq in $resultResponse.mcqResults) {
            $icon = if ($mcq.isCorrect) { "✓" } else { "✗" }
            $color = if ($mcq.isCorrect) { "Green" } else { "Red" }
            $correctInfo = if (-not $mcq.isCorrect) { " (Correct: $($mcq.correctAnswer))" } else { "" }
            Write-Host "  $icon $($mcq.questionId): Selected '$($mcq.selectedOption)'$correctInfo - $($mcq.marksAwarded) marks" -ForegroundColor $color
        }
    }
    
    # ─────────────────────────────────────────────────────────────
    # Subjective Results with Feedback
    # ─────────────────────────────────────────────────────────────
    if ($resultResponse.subjectiveResults -and $resultResponse.subjectiveResults.Count -gt 0) {
        Write-Host "`n📖 SUBJECTIVE RESULTS WITH FEEDBACK:" -ForegroundColor White
        Write-Host "═════════════════════════════════════════════════════" -ForegroundColor Gray
        
        foreach ($sub in $resultResponse.subjectiveResults) {
            $fullMarks = if ($sub.isFullyCorrect) { " ⭐" } else { "" }
            Write-Host "`n  Question $($sub.questionNumber): $($sub.questionText.Substring(0, [Math]::Min(50, $sub.questionText.Length)))..." -ForegroundColor White
            Write-Host "  Score: $($sub.earnedMarks)/$($sub.maxMarks)$fullMarks" -ForegroundColor $(if ($sub.isFullyCorrect) { "Green" } else { "Yellow" })
            
            # Step Analysis
            if ($sub.stepAnalysis -and $sub.stepAnalysis.Count -gt 0) {
                Write-Host "  Step Analysis:" -ForegroundColor Gray
                foreach ($step in $sub.stepAnalysis) {
                    $stepIcon = if ($step.isCorrect) { "✓" } else { "✗" }
                    $stepColor = if ($step.isCorrect) { "Green" } else { "Red" }
                    Write-Host "    $stepIcon Step $($step.step): $($step.description) - $($step.marksAwarded)/$($step.maxMarksForStep)" -ForegroundColor $stepColor
                    if ($step.feedback) {
                        Write-Host "      → $($step.feedback)" -ForegroundColor Gray
                    }
                }
            }
            
            # Overall Feedback
            if ($sub.overallFeedback) {
                Write-Host "  💡 Feedback: $($sub.overallFeedback)" -ForegroundColor Cyan
            }
        }
    }
    
} catch {
    Write-Error "Failed to get results: $($_.Exception.Message)"
    Write-Host $_.ErrorDetails.Message -ForegroundColor Red
}

# ============================================================================
# TEST SUMMARY
# ============================================================================
Write-Step "TEST SUMMARY"

Write-Host ""
$allPassed = $true

foreach ($test in $testResults.Keys) {
    $passed = $testResults[$test]
    $icon = if ($passed) { "✅" } else { "❌" }
    $color = if ($passed) { "Green" } else { "Red" }
    Write-Host "  $icon $test" -ForegroundColor $color
    if (-not $passed) { $allPassed = $false }
}

Write-Host ""
if ($allPassed) {
    Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║       🎉 ALL TESTS PASSED - SYSTEM WORKING E2E! 🎉       ║" -ForegroundColor Green
    Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Green
} else {
    Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Red
    Write-Host "║          ⚠️  SOME TESTS FAILED - CHECK ABOVE ⚠️          ║" -ForegroundColor Red
    Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Red
}

# Cleanup
if (Test-Path $tempAnswerFile) {
    Remove-Item $tempAnswerFile -Force
    Write-Info "Cleaned up temp file: $tempAnswerFile"
}

Write-Host ""
Write-Info "Test completed at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
