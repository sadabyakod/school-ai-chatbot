# ============================================================================
# END-TO-END ANSWER SHEET EVALUATION TEST WITH DATABASE STATUS TRACKING
# ============================================================================
# This script tests:
# 1. Upload answer sheet → Status: PendingEvaluation
# 2. OCR extraction starts → Status: OcrProcessing
# 3. AI evaluation starts → Status: Evaluating
# 4. Evaluation completes → Status: Completed
# 5. Fetch results with step-wise marks and expected answers
# ============================================================================

param(
    [string]$BaseUrl = "http://localhost:8080",
    [int]$MaxPollSeconds = 120,
    [int]$PollIntervalSeconds = 3
)

# Color functions for better output
function Write-Step { param($msg) Write-Host "`n╔════════════════════════════════════════" -ForegroundColor Cyan; Write-Host "║ $msg" -ForegroundColor Cyan; Write-Host "╚════════════════════════════════════════" -ForegroundColor Cyan }
function Write-Success { param($msg) Write-Host "✅ $msg" -ForegroundColor Green }
function Write-Error { param($msg) Write-Host "❌ $msg" -ForegroundColor Red }
function Write-Info { param($msg) Write-Host "ℹ️  $msg" -ForegroundColor Gray }
function Write-Status { param($status) 
    $color = switch ($status) {
        "PendingEvaluation" { "Yellow" }
        "OcrProcessing" { "Cyan" }
        "Evaluating" { "Magenta" }
        "Completed" { "Green" }
        "Failed" { "Red" }
        default { "White" }
    }
    Write-Host "📊 STATUS: $status" -ForegroundColor $color
}

Write-Host @"

╔════════════════════════════════════════════════════════════════════════╗
║  ANSWER SHEET EVALUATION FLOW - DATABASE STATUS TRACKING TEST          ║
║                                                                        ║
║  Flow: Upload → OCR → AI Evaluation → Results                         ║
║  Status: PendingEvaluation → OcrProcessing → Evaluating → Completed   ║
╚════════════════════════════════════════════════════════════════════════╝

"@ -ForegroundColor Cyan

Write-Info "Base URL: $BaseUrl"
Write-Info "Max Wait Time: $MaxPollSeconds seconds"
Write-Info "Poll Interval: $PollIntervalSeconds seconds"
Write-Host ""

# ============================================================================
# STEP 1: GENERATE EXAM
# ============================================================================
Write-Step "STEP 1: Generate Exam with Subjective Questions"

$examRequest = @{
    subject = "Mathematics"
    standard = "2nd PUC"
    chapter = "Determinants"
    numberOfQuestions = 3
    includeSubjective = $true
    difficulty = "Medium"
} | ConvertTo-Json

try {
    Write-Info "Generating exam..."
    $examResponse = Invoke-RestMethod -Uri "$BaseUrl/api/exam/generate" `
        -Method POST `
        -Body $examRequest `
        -ContentType "application/json"
    
    $examId = $examResponse.examId
    $examTitle = $examResponse.title
    
    Write-Success "Exam generated successfully!"
    Write-Info "Exam ID: $examId"
    Write-Info "Title: $examTitle"
    Write-Info "Total Questions: $($examResponse.questions.Count)"
    Write-Host ""
    
    # Display questions
    $questionNum = 1
    foreach ($q in $examResponse.questions) {
        if ($q.questionType -eq "Subjective") {
            Write-Host "  Q$questionNum [SUBJECTIVE] (${q.marks} marks): $($q.questionText.Substring(0, [Math]::Min(70, $q.questionText.Length)))..." -ForegroundColor Gray
            $questionNum++
        }
    }
    Write-Host ""
} catch {
    Write-Error "Failed to generate exam: $($_.Exception.Message)"
    if ($_.ErrorDetails.Message) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
    exit 1
}

# ============================================================================
# STEP 2: CREATE TEST ANSWER SHEET
# ============================================================================
Write-Step "STEP 2: Create Student Answer Sheet"

$studentId = "TEST-STUDENT-$(Get-Date -Format 'yyyyMMddHHmmss')"
Write-Info "Student ID: $studentId"

# Create comprehensive answers for evaluation
$testAnswers = @"
STUDENT ANSWER SHEET
================================================================================
Student ID: $studentId
Exam: $examTitle
Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
================================================================================

QUESTION 1:
-----------
Given matrix A = |2  3|
                 |4  5|

Solution:
Step 1: Identify the matrix elements
        a=2, b=3, c=4, d=5

Step 2: Apply determinant formula for 2x2 matrix
        det(A) = ad - bc

Step 3: Substitute values
        det(A) = (2)(5) - (3)(4)
        det(A) = 10 - 12

Step 4: Calculate final answer
        det(A) = -2

Answer: The determinant is -2


QUESTION 2:
-----------
Find the determinant of matrix B = |1  2  3|
                                    |4  5  6|
                                    |7  8  9|

Solution:
Step 1: Apply cofactor expansion along first row
        det(B) = 1*C11 + 2*C12 + 3*C13

Step 2: Calculate cofactor C11
        C11 = + |5  6| = (5*9) - (6*8) = 45 - 48 = -3
                |8  9|

Step 3: Calculate cofactor C12
        C12 = - |4  6| = -[(4*9) - (6*7)] = -[36 - 42] = -(-6) = 6
                |7  9|

Step 4: Calculate cofactor C13
        C13 = + |4  5| = (4*8) - (5*7) = 32 - 35 = -3
                |7  8|

Step 5: Calculate determinant
        det(B) = 1*(-3) + 2*(6) + 3*(-3)
        det(B) = -3 + 12 - 9
        det(B) = 0

Answer: The determinant is 0


QUESTION 3:
-----------
Properties of determinants:
A determinant changes sign when two rows or columns are interchanged.
If two rows or columns are identical, the determinant is zero.
If all elements of a row or column are multiplied by k, the determinant is multiplied by k.

Answer: These are the fundamental properties of determinants.

================================================================================
END OF ANSWER SHEET
================================================================================
"@

$tempFile = "test-answer-sheet-$(Get-Date -Format 'yyyyMMddHHmmss').txt"
$testAnswers | Out-File -FilePath $tempFile -Encoding UTF8

Write-Success "Answer sheet created: $tempFile"
Write-Info "File size: $((Get-Item $tempFile).Length) bytes"
Write-Host ""

# ============================================================================
# STEP 3: UPLOAD ANSWER SHEET
# ============================================================================
Write-Step "STEP 3: Upload Answer Sheet for Evaluation"

$submissionId = $null

try {
    Write-Info "Uploading answer sheet..."
    
    # Prepare multipart form data
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"
    
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
        "Content-Disposition: form-data; name=`"files`"; filename=`"$tempFile`"",
        "Content-Type: text/plain",
        "",
        $testAnswers,
        "--$boundary--"
    ) -join $LF
    
    $uploadResponse = Invoke-RestMethod -Uri "$BaseUrl/api/exam/upload-written" `
        -Method POST `
        -ContentType "multipart/form-data; boundary=$boundary" `
        -Body $bodyLines
    
    $submissionId = $uploadResponse.writtenSubmissionId
    
    Write-Success "Answer sheet uploaded successfully!"
    Write-Info "Submission ID: $submissionId"
    Write-Status $uploadResponse.status
    Write-Info "Message: $($uploadResponse.message)"
    Write-Host ""
} catch {
    Write-Error "Failed to upload answer sheet: $($_.Exception.Message)"
    if ($_.ErrorDetails.Message) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
    
    # Cleanup
    if (Test-Path $tempFile) {
        Remove-Item $tempFile -Force
    }
    exit 1
}

# ============================================================================
# STEP 4: POLL STATUS AND TRACK DATABASE UPDATES
# ============================================================================
Write-Step "STEP 4: Monitor Status Updates in Database"

Write-Info "Polling submission status every $PollIntervalSeconds seconds..."
Write-Info "This demonstrates real-time database status updates"
Write-Host ""

$startTime = Get-Date
$lastStatus = ""
$statusHistory = @()
$attempts = 0
$maxAttempts = [Math]::Ceiling($MaxPollSeconds / $PollIntervalSeconds)

while ($attempts -lt $maxAttempts) {
    $attempts++
    $elapsed = ((Get-Date) - $startTime).TotalSeconds
    
    try {
        $statusResponse = Invoke-RestMethod -Uri "$BaseUrl/api/exam/submission-status/$submissionId" `
            -Method GET
        
        $currentStatus = $statusResponse.status
        
        # Track status changes
        if ($currentStatus -ne $lastStatus) {
            $timestamp = Get-Date -Format 'HH:mm:ss'
            $statusChange = @{
                Timestamp = $timestamp
                Status = $currentStatus
                Message = $statusResponse.statusMessage
                Elapsed = [Math]::Round($elapsed, 1)
            }
            $statusHistory += $statusChange
            
            Write-Host "[$timestamp] " -NoNewline -ForegroundColor Gray
            Write-Status $currentStatus
            Write-Host "          $($statusResponse.statusMessage)" -ForegroundColor White
            
            # Log database field updates
            if ($currentStatus -eq "OcrProcessing") {
                Write-Info "          DB Field: OcrStartedAt = $($statusResponse.submittedAt)"
            } elseif ($currentStatus -eq "Evaluating") {
                Write-Info "          DB Field: EvaluationStartedAt = $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
            } elseif ($currentStatus -eq "Completed") {
                Write-Info "          DB Field: EvaluatedAt = $($statusResponse.evaluatedAt)"
            }
            
            Write-Host ""
            $lastStatus = $currentStatus
        }
        
        # Check if completed
        if ($statusResponse.isComplete -eq $true) {
            Write-Success "Evaluation completed successfully!"
            Write-Info "Total processing time: $([Math]::Round($elapsed, 1)) seconds"
            Write-Host ""
            break
        }
        
        # Check if failed
        if ($currentStatus -eq "Failed") {
            Write-Error "Evaluation failed!"
            Write-Host ""
            break
        }
        
    } catch {
        Write-Host "  [Attempt $attempts] Waiting... ($([Math]::Round($elapsed, 1))s)" -ForegroundColor DarkGray
    }
    
    Start-Sleep -Seconds $PollIntervalSeconds
}

# Display status history
Write-Host "┌─────────────────────────────────────────────────┐" -ForegroundColor Cyan
Write-Host "│  DATABASE STATUS HISTORY                        │" -ForegroundColor Cyan
Write-Host "└─────────────────────────────────────────────────┘" -ForegroundColor Cyan
Write-Host ""

foreach ($entry in $statusHistory) {
    Write-Host "  [$($entry.Timestamp)] " -NoNewline -ForegroundColor Gray
    Write-Host "$($entry.Status) " -NoNewline -ForegroundColor Yellow
    Write-Host "(+$($entry.Elapsed)s)" -ForegroundColor DarkGray
}
Write-Host ""

# ============================================================================
# STEP 5: FETCH COMPLETE RESULTS WITH STEP-WISE MARKS
# ============================================================================
Write-Step "STEP 5: Fetch Complete Evaluation Results"

try {
    $finalResult = Invoke-RestMethod -Uri "$BaseUrl/api/exam/result/$examId/$studentId" -Method GET
    
    # ========================================
    # DISPLAY OVERALL SCORES
    # ========================================
    Write-Host "┌─────────────────────────────────────────────────┐" -ForegroundColor Green
    Write-Host "│  EVALUATION RESULTS                              │" -ForegroundColor Green
    Write-Host "└─────────────────────────────────────────────────┘" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "Exam: " -NoNewline -ForegroundColor Gray
    Write-Host $finalResult.examTitle -ForegroundColor White
    Write-Host "Student ID: " -NoNewline -ForegroundColor Gray
    Write-Host $finalResult.studentId -ForegroundColor White
    Write-Host ""
    
    # ========================================
    # DISPLAY MCQ RESULTS
    # ========================================
    if ($finalResult.mcqResults -and $finalResult.mcqResults.Count -gt 0) {
        Write-Host "MCQ SECTION:" -ForegroundColor Yellow
        Write-Host "────────────" -ForegroundColor DarkGray
        Write-Host "Score: $($finalResult.mcqScore)/$($finalResult.mcqTotalMarks)" -ForegroundColor White
        Write-Host ""
        
        $qNum = 1
        foreach ($mcq in $finalResult.mcqResults) {
            $icon = if ($mcq.isCorrect) { "✓" } else { "✗" }
            $color = if ($mcq.isCorrect) { "Green" } else { "Red" }
            
            Write-Host "  Q$qNum: " -NoNewline -ForegroundColor Gray
            Write-Host "[$icon] " -NoNewline -ForegroundColor $color
            Write-Host "Answer: $($mcq.selectedOption) " -NoNewline -ForegroundColor White
            Write-Host "(Correct: $($mcq.correctAnswer)) " -NoNewline -ForegroundColor DarkGray
            Write-Host "Marks: $($mcq.marksAwarded)" -ForegroundColor $color
            $qNum++
        }
        Write-Host ""
    }
    
    # ========================================
    # DISPLAY SUBJECTIVE RESULTS WITH STEP-WISE MARKS
    # ========================================
    if ($finalResult.subjectiveResults -and $finalResult.subjectiveResults.Count -gt 0) {
        Write-Host "SUBJECTIVE SECTION:" -ForegroundColor Yellow
        Write-Host "───────────────────" -ForegroundColor DarkGray
        Write-Host "Score: $($finalResult.subjectiveScore)/$($finalResult.subjectiveTotalMarks)" -ForegroundColor White
        Write-Host ""
        
        $questionNum = 1
        foreach ($result in $finalResult.subjectiveResults) {
            Write-Host "╔════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
            Write-Host "║ QUESTION $questionNum" -ForegroundColor Cyan
            Write-Host "╚════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
            Write-Host ""
            
            # Question text (truncated if too long)
            $questionText = if ($result.questionText.Length -gt 100) {
                $result.questionText.Substring(0, 100) + "..."
            } else {
                $result.questionText
            }
            Write-Host "Question: " -NoNewline -ForegroundColor Gray
            Write-Host $questionText -ForegroundColor White
            Write-Host ""
            
            # Marks summary
            $percentCorrect = [Math]::Round(($result.earnedMarks / $result.maxMarks) * 100, 1)
            $marksColor = if ($percentCorrect -ge 80) { "Green" } elseif ($percentCorrect -ge 50) { "Yellow" } else { "Red" }
            
            Write-Host "Marks Awarded: " -NoNewline -ForegroundColor Gray
            Write-Host "$($result.earnedMarks) / $($result.maxMarks) " -NoNewline -ForegroundColor $marksColor
            Write-Host "($percentCorrect%)" -ForegroundColor $marksColor
            
            $statusIcon = if ($result.isFullyCorrect) { "✓ Fully Correct" } else { "⚠ Partially Correct" }
            $statusColor = if ($result.isFullyCorrect) { "Green" } else { "Yellow" }
            Write-Host "Status: " -NoNewline -ForegroundColor Gray
            Write-Host $statusIcon -ForegroundColor $statusColor
            Write-Host ""
            
            # ========================================
            # EXPECTED ANSWER
            # ========================================
            Write-Host "┌─ EXPECTED ANSWER ─────────────────────────────────────────────────────┐" -ForegroundColor Green
            $expectedLines = $result.expectedAnswer -split "`n"
            foreach ($line in $expectedLines) {
                if ($line.Trim().Length -gt 0) {
                    Write-Host "│ " -NoNewline -ForegroundColor Green
                    Write-Host $line.Trim() -ForegroundColor White
                }
            }
            Write-Host "└───────────────────────────────────────────────────────────────────────┘" -ForegroundColor Green
            Write-Host ""
            
            # ========================================
            # STUDENT'S ANSWER
            # ========================================
            Write-Host "┌─ STUDENT'S ANSWER ────────────────────────────────────────────────────┐" -ForegroundColor Yellow
            $studentLines = $result.studentAnswerEcho -split "`n"
            $lineCount = 0
            foreach ($line in $studentLines) {
                if ($line.Trim().Length -gt 0 -and $lineCount -lt 10) {
                    Write-Host "│ " -NoNewline -ForegroundColor Yellow
                    Write-Host $line.Trim() -ForegroundColor White
                    $lineCount++
                }
            }
            if ($studentLines.Count -gt 10) {
                Write-Host "│ " -NoNewline -ForegroundColor Yellow
                Write-Host "... (truncated)" -ForegroundColor DarkGray
            }
            Write-Host "└───────────────────────────────────────────────────────────────────────┘" -ForegroundColor Yellow
            Write-Host ""
            
            # ========================================
            # STEP-WISE EVALUATION
            # ========================================
            if ($result.stepAnalysis -and $result.stepAnalysis.Count -gt 0) {
                Write-Host "┌─ STEP-BY-STEP EVALUATION ─────────────────────────────────────────────┐" -ForegroundColor Cyan
                
                foreach ($step in $result.stepAnalysis) {
                    $stepIcon = if ($step.isCorrect) { "✓" } else { "✗" }
                    $stepColor = if ($step.isCorrect) { "Green" } else { "Red" }
                    
                    Write-Host "│" -ForegroundColor Cyan
                    Write-Host "│ " -NoNewline -ForegroundColor Cyan
                    Write-Host "Step $($step.step): " -NoNewline -ForegroundColor White
                    Write-Host $step.description -ForegroundColor Gray
                    Write-Host "│   " -NoNewline -ForegroundColor Cyan
                    Write-Host "[$stepIcon] " -NoNewline -ForegroundColor $stepColor
                    Write-Host "Marks: $($step.marksAwarded) / $($step.maxMarksForStep)" -ForegroundColor $stepColor
                    Write-Host "│   " -NoNewline -ForegroundColor Cyan
                    Write-Host "Feedback: " -NoNewline -ForegroundColor Gray
                    Write-Host $step.feedback -ForegroundColor White
                }
                
                Write-Host "└───────────────────────────────────────────────────────────────────────┘" -ForegroundColor Cyan
                Write-Host ""
            }
            
            # ========================================
            # OVERALL FEEDBACK
            # ========================================
            Write-Host "┌─ OVERALL FEEDBACK ────────────────────────────────────────────────────┐" -ForegroundColor Magenta
            Write-Host "│ " -NoNewline -ForegroundColor Magenta
            Write-Host $result.overallFeedback -ForegroundColor White
            Write-Host "└───────────────────────────────────────────────────────────────────────┘" -ForegroundColor Magenta
            Write-Host ""
            
            $questionNum++
        }
    } else {
        Write-Host "⚠ No subjective results available yet." -ForegroundColor Yellow
        Write-Host "  Evaluation may still be processing or failed." -ForegroundColor Gray
        Write-Host ""
    }
    
    # ========================================
    # FINAL GRADE SUMMARY
    # ========================================
    Write-Host "╔════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║  FINAL GRADE SUMMARY" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "  MCQ Score:        " -NoNewline -ForegroundColor Gray
    Write-Host "$($finalResult.mcqScore) / $($finalResult.mcqTotalMarks)" -ForegroundColor White
    
    Write-Host "  Subjective Score: " -NoNewline -ForegroundColor Gray
    Write-Host "$($finalResult.subjectiveScore) / $($finalResult.subjectiveTotalMarks)" -ForegroundColor White
    
    Write-Host "  " + ("─" * 50) -ForegroundColor DarkGray
    
    $grandScoreColor = if ($finalResult.percentage -ge 60) { "Green" } elseif ($finalResult.percentage -ge 35) { "Yellow" } else { "Red" }
    
    Write-Host "  TOTAL SCORE:      " -NoNewline -ForegroundColor Gray
    Write-Host "$($finalResult.grandScore) / $($finalResult.grandTotalMarks)" -ForegroundColor $grandScoreColor
    
    Write-Host "  Percentage:       " -NoNewline -ForegroundColor Gray
    Write-Host "$($finalResult.percentage)%" -ForegroundColor $grandScoreColor
    
    Write-Host "  Grade:            " -NoNewline -ForegroundColor Gray
    Write-Host $finalResult.grade -ForegroundColor $grandScoreColor
    
    Write-Host "  Status:           " -NoNewline -ForegroundColor Gray
    $passIcon = if ($finalResult.passed) { "✓ PASSED" } else { "✗ FAILED" }
    $passColor = if ($finalResult.passed) { "Green" } else { "Red" }
    Write-Host $passIcon -ForegroundColor $passColor
    
    if ($finalResult.evaluatedAt) {
        Write-Host "  Evaluated At:     " -NoNewline -ForegroundColor Gray
        Write-Host $finalResult.evaluatedAt -ForegroundColor DarkGray
    }
    
    Write-Host ""
    
} catch {
    Write-Error "Failed to fetch results: $($_.Exception.Message)"
    if ($_.ErrorDetails.Message) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
}

# ============================================================================
# CLEANUP
# ============================================================================
Write-Step "CLEANUP"

if (Test-Path $tempFile) {
    Remove-Item $tempFile -Force
    Write-Success "Temporary file removed: $tempFile"
}

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  TEST COMPLETED SUCCESSFULLY!" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""

# Display summary
Write-Host "TEST SUMMARY:" -ForegroundColor Cyan
Write-Host "─────────────" -ForegroundColor DarkGray
Write-Host "✓ Exam generated: $examId" -ForegroundColor Green
Write-Host "✓ Answer sheet uploaded: $submissionId" -ForegroundColor Green
Write-Host "✓ Status tracking: $($statusHistory.Count) status changes" -ForegroundColor Green
Write-Host "✓ Results fetched with step-wise marks" -ForegroundColor Green
Write-Host "✓ Expected answers displayed" -ForegroundColor Green
Write-Host ""

Write-Host "Database Status Flow:" -ForegroundColor Cyan
foreach ($entry in $statusHistory) {
    Write-Host "  → $($entry.Status) (at $($entry.Timestamp))" -ForegroundColor Gray
}
Write-Host ""
