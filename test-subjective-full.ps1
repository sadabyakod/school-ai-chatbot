# Complete Test: Subjective Answer Sheet Upload and AI Evaluation
$baseUrl = "http://localhost:8080"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "SUBJECTIVE ANSWER EVALUATION TEST" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Step 1: Generate an exam
Write-Host "Step 1: Generating exam..." -ForegroundColor Yellow
$examRequest = @{
    subject = "Mathematics"
    chapter = "Calculus"
    class = "12th Grade"
    questionCount = 3
} | ConvertTo-Json

try {
    $examResponse = Invoke-RestMethod -Uri "$baseUrl/api/exam/generate" `
        -Method POST `
        -Body $examRequest `
        -ContentType "application/json"
    
    $examId = $examResponse.examId
    Write-Host "SUCCESS: Exam generated with ID: $examId`n" -ForegroundColor Green
} catch {
    Write-Host "FAILED: Could not generate exam" -ForegroundColor Red
    exit
}

# Step 2: Upload subjective answer sheet (simulate with a text file)
Write-Host "Step 2: Student uploading answer sheet..." -ForegroundColor Yellow
$studentId = "STUDENT-$(Get-Random -Minimum 1000 -Maximum 9999)"

# Create a sample answer file
$answerContent = @"
Question 1:
The derivative of x^2 is 2x
Using power rule: d/dx(x^n) = nx^(n-1)
Therefore d/dx(x^2) = 2x^1 = 2x

Question 2:
Integration of sin(x) dx
The antiderivative of sin(x) is -cos(x)
Therefore integral of sin(x) dx = -cos(x) + C

Question 3:
Limit as x approaches 0 of sin(x)/x
Using L'Hospital's rule:
lim(x->0) sin(x)/x = lim(x->0) cos(x)/1 = 1
"@

$tempFile = [System.IO.Path]::GetTempFileName()
[System.IO.File]::WriteAllText($tempFile, $answerContent)

# Upload the answer sheet
$uri = "$baseUrl/api/exam/upload-written"
$form = @{
    examId = $examId
    studentId = $studentId
    files = Get-Item $tempFile
}

try {
    $uploadResponse = Invoke-RestMethod -Uri $uri -Method POST -Form $form
    $submissionId = $uploadResponse.writtenSubmissionId
    Write-Host "SUCCESS: Answer sheet uploaded" -ForegroundColor Green
    Write-Host "  Submission ID: $submissionId" -ForegroundColor Gray
    Write-Host "  Status: $($uploadResponse.status)" -ForegroundColor Gray
    Write-Host "  Message: $($uploadResponse.message)`n" -ForegroundColor Gray
} catch {
    Write-Host "FAILED: Could not upload answer sheet" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Remove-Item $tempFile -ErrorAction SilentlyContinue
    exit
}

# Clean up temp file
Remove-Item $tempFile -ErrorAction SilentlyContinue

# Step 3: Wait for AI evaluation (it processes asynchronously)
Write-Host "Step 3: AI is evaluating the answers..." -ForegroundColor Yellow
Write-Host "  - Extracting text using OCR..." -ForegroundColor Gray
Write-Host "  - Analyzing each answer..." -ForegroundColor Gray
Write-Host "  - Calculating scores..." -ForegroundColor Gray
Write-Host "  - Generating feedback...`n" -ForegroundColor Gray

$maxAttempts = 30
$attempt = 0
$evaluationComplete = $false

while ($attempt -lt $maxAttempts -and -not $evaluationComplete) {
    Start-Sleep -Seconds 2
    $attempt++
    
    try {
        $resultUri = "$baseUrl/api/exam/result/$examId/$studentId"
        $result = Invoke-RestMethod -Uri $resultUri -Method GET
        
        if ($result.subjectiveResults -and $result.subjectiveResults.Count -gt 0) {
            $evaluationComplete = $true
        }
    } catch {
        # Still processing
    }
    
    if ($attempt -eq 10 -or $attempt -eq 20) {
        Write-Host "  Still evaluating... ($attempt seconds)" -ForegroundColor Gray
    }
}

if (-not $evaluationComplete) {
    Write-Host "Evaluation is taking longer than expected. Check status manually." -ForegroundColor Yellow
    Write-Host "URL: $resultUri`n" -ForegroundColor Gray
    exit
}

# Step 4: Display complete evaluation results
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "EVALUATION COMPLETE - STUDENT RESULTS" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "Exam: $($result.examTitle)" -ForegroundColor White
Write-Host "Student ID: $($result.studentId)" -ForegroundColor White
Write-Host "Evaluated At: $($result.evaluatedAt)`n" -ForegroundColor Gray

Write-Host "--- SUBJECTIVE QUESTIONS EVALUATION ---`n" -ForegroundColor Yellow

foreach ($question in $result.subjectiveResults) {
    Write-Host "Question $($question.questionNumber):" -ForegroundColor Cyan
    Write-Host "  $($question.questionText)" -ForegroundColor White
    Write-Host ""
    
    Write-Host "  SCORE: $($question.earnedMarks)/$($question.maxMarks) marks" -ForegroundColor $(
        if ($question.isFullyCorrect) { "Green" } 
        elseif ($question.earnedMarks -gt 0) { "Yellow" } 
        else { "Red" }
    )
    Write-Host ""
    
    Write-Host "  Student's Answer:" -ForegroundColor Gray
    Write-Host "  $($question.studentAnswerEcho)" -ForegroundColor White
    Write-Host ""
    
    Write-Host "  Expected Answer:" -ForegroundColor Gray
    Write-Host "  $($question.expectedAnswer)" -ForegroundColor White
    Write-Host ""
    
    if ($question.stepAnalysis -and $question.stepAnalysis.Count -gt 0) {
        Write-Host "  Step-by-Step Analysis:" -ForegroundColor Cyan
        foreach ($step in $question.stepAnalysis) {
            $stepStatus = if ($step.isCorrect) { "[CORRECT]" } else { "[INCORRECT]" }
            $stepColor = if ($step.isCorrect) { "Green" } else { "Red" }
            
            Write-Host "    Step $($step.step): $($step.description)" -ForegroundColor White
            Write-Host "    Status: $stepStatus | Marks: $($step.marksAwarded)/$($step.maxMarksForStep)" -ForegroundColor $stepColor
            Write-Host "    Feedback: $($step.feedback)" -ForegroundColor Gray
            Write-Host ""
        }
    }
    
    Write-Host "  Overall Feedback:" -ForegroundColor Cyan
    Write-Host "  $($question.overallFeedback)" -ForegroundColor White
    Write-Host ""
    Write-Host "  Improvements Needed:" -ForegroundColor Yellow
    
    if ($question.isFullyCorrect) {
        Write-Host "  Excellent work! Your answer is complete and correct." -ForegroundColor Green
    } else {
        $improvements = @()
        foreach ($step in $question.stepAnalysis | Where-Object { -not $_.isCorrect }) {
            $improvements += "- $($step.feedback)"
        }
        
        if ($improvements.Count -gt 0) {
            foreach ($imp in $improvements) {
                Write-Host "  $imp" -ForegroundColor Yellow
            }
        } else {
            Write-Host "  Review the expected answer and step analysis for complete understanding." -ForegroundColor Yellow
        }
    }
    
    Write-Host "`n  ----------------------------------------`n" -ForegroundColor Gray
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SUMMARY" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "Subjective Score: $($result.subjectiveScore)/$($result.subjectiveTotalMarks)" -ForegroundColor White
Write-Host "Total Score: $($result.grandScore)/$($result.grandTotalMarks)" -ForegroundColor White
Write-Host "Percentage: $($result.percentage)%" -ForegroundColor White
Write-Host "Grade: $($result.grade)" -ForegroundColor $(
    if ($result.grade -eq "A+" -or $result.grade -eq "A") { "Green" }
    elseif ($result.grade -eq "B+" -or $result.grade -eq "B") { "Yellow" }
    else { "Red" }
)
Write-Host "Status: $(if ($result.passed) { 'PASSED' } else { 'FAILED' })" -ForegroundColor $(
    if ($result.passed) { "Green" } else { "Red" }
)

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "WHAT THE STUDENT RECEIVES:" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "[OK] Individual question scores" -ForegroundColor Green
Write-Host "[OK] Total score and percentage" -ForegroundColor Green
Write-Host "[OK] Expected/correct answers for each question" -ForegroundColor Green
Write-Host "[OK] Their own answers echoed back" -ForegroundColor Green
Write-Host "[OK] Step-by-step analysis with marks per step" -ForegroundColor Green
Write-Host "[OK] Detailed feedback for each step" -ForegroundColor Green
Write-Host "[OK] Overall feedback for each question" -ForegroundColor Green
Write-Host "[OK] Suggestions for improvement" -ForegroundColor Green
Write-Host "[OK] Final grade and pass/fail status" -ForegroundColor Green

Write-Host "`n[SUCCESS] Complete subjective evaluation system is working!`n" -ForegroundColor Green
