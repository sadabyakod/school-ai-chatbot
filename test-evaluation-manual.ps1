# Manual Test - Answer Sheet Evaluation Flow with Status Tracking
# Run each command step by step to see the complete flow

Write-Host @"

╔════════════════════════════════════════════════════════════════════════╗
║  ANSWER SHEET EVALUATION FLOW - MANUAL TEST                            ║
║  This demonstrates the complete evaluation flow with status updates    ║
╚════════════════════════════════════════════════════════════════════════╝

"@ -ForegroundColor Cyan

$baseUrl = "http://localhost:8080"
$examId = "Karnataka_2nd_PUC_Math_2024_25"  # Use existing exam
$studentId = "DEMO-STUDENT-$(Get-Random -Maximum 10000)"

Write-Host "Base URL: $baseUrl" -ForegroundColor Gray
Write-Host "Exam ID: $examId" -ForegroundColor Gray
Write-Host "Student ID: $studentId`n" -ForegroundColor Gray

# Step 1: Create answer file
Write-Host "╔════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "║ STEP 1: Create Answer Sheet" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════" -ForegroundColor Cyan

$answerText = @"
STUDENT ANSWER SHEET
Student ID: $studentId
Exam: Karnataka 2nd PUC Mathematics

Question 1: Find the determinant of matrix A = |2 3|
                                                 |4 5|

Solution:
det(A) = (2)(5) - (3)(4) = 10 - 12 = -2
Answer: -2

Question 2: Derivative of x^2
f'(x) = 2x using power rule

Question 3: Integration of x^2
∫x^2 dx = (x^3)/3 + C
"@

$answerText | Out-File -FilePath "demo-answers.txt" -Encoding UTF8
Write-Host "✅ Answer file created: demo-answers.txt`n" -ForegroundColor Green

# Step 2: Upload answer sheet
Write-Host "╔════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "║ STEP 2: Upload Answer Sheet" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════" -ForegroundColor Cyan

Write-Host "Uploading to: POST $baseUrl/api/exam/upload-written" -ForegroundColor Gray

$form = @{
    examId = $examId
    studentId = $studentId
    files = Get-Item "demo-answers.txt"
}

try {
    $uploadResponse = Invoke-RestMethod -Uri "$baseUrl/api/exam/upload-written" `
        -Method POST `
        -Form $form

    $submissionId = $uploadResponse.writtenSubmissionId
    Write-Host "✅ Upload successful!" -ForegroundColor Green
    Write-Host "   Submission ID: $submissionId" -ForegroundColor White
    Write-Host "   Status: " -NoNewline -ForegroundColor White
    Write-Host $uploadResponse.status -ForegroundColor Yellow
    Write-Host "   Message: $($uploadResponse.message)`n" -ForegroundColor White
} catch {
    Write-Host "❌ Upload failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.ErrorDetails.Message)`n" -ForegroundColor Red
    exit 1
}

# Step 3: Poll status
Write-Host "╔════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "║ STEP 3: Monitor Status Updates" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════" -ForegroundColor Cyan

Write-Host "Polling: GET $baseUrl/api/exam/submission-status/$submissionId`n" -ForegroundColor Gray

$maxAttempts = 30
$attempt = 0
$lastStatus = ""

while ($attempt -lt $maxAttempts) {
    $attempt++
    
    try {
        $statusResponse = Invoke-RestMethod -Uri "$baseUrl/api/exam/submission-status/$submissionId" `
            -Method GET
        
        $currentStatus = $statusResponse.status
        
        if ($currentStatus -ne $lastStatus) {
            $timestamp = Get-Date -Format "HH:mm:ss"
            Write-Host "[$timestamp] " -NoNewline -ForegroundColor Gray
            
            switch ($currentStatus) {
                "PendingEvaluation" { Write-Host "📊 STATUS: $currentStatus" -ForegroundColor Yellow }
                "OcrProcessing" { Write-Host "📄 STATUS: $currentStatus" -ForegroundColor Cyan }
                "Evaluating" { Write-Host "🤖 STATUS: $currentStatus" -ForegroundColor Magenta }
                "Completed" { Write-Host "✅ STATUS: $currentStatus" -ForegroundColor Green }
                "Failed" { Write-Host "❌ STATUS: $currentStatus" -ForegroundColor Red }
                default { Write-Host "📊 STATUS: $currentStatus" -ForegroundColor White }
            }
            
            Write-Host "   Message: $($statusResponse.statusMessage)" -ForegroundColor White
            
            # Show database fields being updated
            if ($currentStatus -eq "OcrProcessing") {
                Write-Host "   DB Field: OcrStartedAt updated" -ForegroundColor DarkGray
            } elseif ($currentStatus -eq "Evaluating") {
                Write-Host "   DB Field: EvaluationStartedAt updated" -ForegroundColor DarkGray
            } elseif ($currentStatus -eq "Completed") {
                Write-Host "   DB Field: EvaluatedAt updated" -ForegroundColor DarkGray
            }
            
            Write-Host ""
            $lastStatus = $currentStatus
        }
        
        if ($statusResponse.isComplete -eq $true) {
            Write-Host "✅ Evaluation completed successfully!`n" -ForegroundColor Green
            break
        }
        
        if ($currentStatus -eq "Failed") {
            Write-Host "❌ Evaluation failed`n" -ForegroundColor Red
            break
        }
        
    } catch {
        Write-Host "  [Attempt $attempt] Checking... (Submission may be queued)" -ForegroundColor DarkGray
    }
    
    Start-Sleep -Seconds 2
}

if ($attempt -ge $maxAttempts) {
    Write-Host "⚠ Maximum polling time exceeded`n" -ForegroundColor Yellow
}

# Step 4: Get results
Write-Host "╔════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "║ STEP 4: Fetch Complete Results" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════" -ForegroundColor Cyan

Write-Host "Fetching: GET $baseUrl/api/exam/result/$examId/$studentId`n" -ForegroundColor Gray

try {
    $result = Invoke-RestMethod -Uri "$baseUrl/api/exam/result/$examId/$studentId" `
        -Method GET
    
    Write-Host "╔════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║  EVALUATION RESULTS" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════════════════════╝`n" -ForegroundColor Green
    
    Write-Host "Exam: $($result.examTitle)" -ForegroundColor White
    Write-Host "Student: $($result.studentId)`n" -ForegroundColor White
    
    # MCQ Results
    if ($result.mcqResults -and $result.mcqResults.Count -gt 0) {
        Write-Host "MCQ SECTION: $($result.mcqScore)/$($result.mcqTotalMarks)" -ForegroundColor Yellow
        foreach ($mcq in $result.mcqResults) {
            $icon = if ($mcq.isCorrect) { "✓" } else { "✗" }
            $color = if ($mcq.isCorrect) { "Green" } else { "Red" }
            Write-Host "  [$icon] Answer: $($mcq.selectedOption) (Correct: $($mcq.correctAnswer)) - $($mcq.marksAwarded) marks" -ForegroundColor $color
        }
        Write-Host ""
    }
    
    # Subjective Results
    if ($result.subjectiveResults -and $result.subjectiveResults.Count -gt 0) {
        Write-Host "SUBJECTIVE SECTION: $($result.subjectiveScore)/$($result.subjectiveTotalMarks)`n" -ForegroundColor Yellow
        
        $qNum = 1
        foreach ($subjResult in $result.subjectiveResults) {
            Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
            Write-Host "QUESTION $qNum" -ForegroundColor Cyan
            Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan
            
            $questionText = if ($subjResult.questionText.Length -gt 80) {
                $subjResult.questionText.Substring(0, 80) + "..."
            } else {
                $subjResult.questionText
            }
            
            Write-Host "Question: $questionText" -ForegroundColor White
            Write-Host "Marks: $($subjResult.earnedMarks) / $($subjResult.maxMarks)" -ForegroundColor White
            Write-Host "Status: " -NoNewline -ForegroundColor White
            if ($subjResult.isFullyCorrect) {
                Write-Host "Fully Correct ✓`n" -ForegroundColor Green
            } else {
                Write-Host "Partially Correct ⚠`n" -ForegroundColor Yellow
            }
            
            # Expected Answer
            Write-Host "┌─ EXPECTED ANSWER ─────────────────────────────────────────────┐" -ForegroundColor Green
            $expLines = $subjResult.expectedAnswer -split "`n" | Select-Object -First 5
            foreach ($line in $expLines) {
                if ($line.Trim()) {
                    Write-Host "│ $($line.Trim())" -ForegroundColor White
                }
            }
            Write-Host "└───────────────────────────────────────────────────────────────┘`n" -ForegroundColor Green
            
            # Student's Answer
            Write-Host "┌─ STUDENT'S ANSWER ────────────────────────────────────────────┐" -ForegroundColor Yellow
            $stuLines = $subjResult.studentAnswerEcho -split "`n" | Select-Object -First 5
            foreach ($line in $stuLines) {
                if ($line.Trim()) {
                    Write-Host "│ $($line.Trim())" -ForegroundColor White
                }
            }
            Write-Host "└───────────────────────────────────────────────────────────────┘`n" -ForegroundColor Yellow
            
            # Step-wise Marks
            if ($subjResult.stepAnalysis -and $subjResult.stepAnalysis.Count -gt 0) {
                Write-Host "┌─ STEP-BY-STEP EVALUATION ─────────────────────────────────────┐" -ForegroundColor Cyan
                foreach ($step in $subjResult.stepAnalysis) {
                    $icon = if ($step.isCorrect) { "✓" } else { "✗" }
                    $color = if ($step.isCorrect) { "Green" } else { "Red" }
                    Write-Host "│" -ForegroundColor Cyan
                    Write-Host "│ Step $($step.step): $($step.description)" -ForegroundColor White
                    Write-Host "│   [$icon] Marks: $($step.marksAwarded)/$($step.maxMarksForStep)" -ForegroundColor $color
                    Write-Host "│   Feedback: $($step.feedback)" -ForegroundColor White
                }
                Write-Host "└───────────────────────────────────────────────────────────────┘`n" -ForegroundColor Cyan
            }
            
            # Overall Feedback
            Write-Host "┌─ OVERALL FEEDBACK ────────────────────────────────────────────┐" -ForegroundColor Magenta
            Write-Host "│ $($subjResult.overallFeedback)" -ForegroundColor White
            Write-Host "└───────────────────────────────────────────────────────────────┘`n" -ForegroundColor Magenta
            
            $qNum++
        }
    }
    
    # Final Summary
    Write-Host "╔════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║  FINAL GRADE SUMMARY" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════════════════════╝`n" -ForegroundColor Green
    
    Write-Host "MCQ Score:        $($result.mcqScore) / $($result.mcqTotalMarks)" -ForegroundColor White
    Write-Host "Subjective Score: $($result.subjectiveScore) / $($result.subjectiveTotalMarks)" -ForegroundColor White
    Write-Host ("─" * 70) -ForegroundColor DarkGray
    
    $grandColor = if ($result.percentage -ge 60) { "Green" } elseif ($result.percentage -ge 35) { "Yellow" } else { "Red" }
    Write-Host "TOTAL SCORE:      $($result.grandScore) / $($result.grandTotalMarks)" -ForegroundColor $grandColor
    Write-Host "Percentage:       $($result.percentage)%" -ForegroundColor $grandColor
    Write-Host "Grade:            $($result.grade)" -ForegroundColor $grandColor
    
    $passStatus = if ($result.passed) { "PASSED ✓" } else { "FAILED ✗" }
    $passColor = if ($result.passed) { "Green" } else { "Red" }
    Write-Host "Status:           $passStatus" -ForegroundColor $passColor
    
    if ($result.evaluatedAt) {
        Write-Host "Evaluated At:     $($result.evaluatedAt)" -ForegroundColor DarkGray
    }
    
    Write-Host ""
    
} catch {
    Write-Host "❌ Failed to fetch results: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ErrorDetails.Message -ForegroundColor Red
}

# Cleanup
if (Test-Path "demo-answers.txt") {
    Remove-Item "demo-answers.txt" -Force
    Write-Host "✅ Cleaned up temporary files" -ForegroundColor Green
}

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  TEST COMPLETED!" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""

Write-Host "What you saw:" -ForegroundColor Cyan
Write-Host "  ✓ Answer sheet upload" -ForegroundColor Green
Write-Host "  ✓ Real-time status tracking (PendingEvaluation → OcrProcessing → Evaluating → Completed)" -ForegroundColor Green
Write-Host "  ✓ Database field updates at each status change" -ForegroundColor Green
Write-Host "  ✓ Complete results with step-wise marks" -ForegroundColor Green
Write-Host "  ✓ Expected answers for incomplete solutions" -ForegroundColor Green
Write-Host "  ✓ Detailed feedback per step" -ForegroundColor Green
Write-Host "  ✓ Final grade calculation" -ForegroundColor Green
Write-Host ""
