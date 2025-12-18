# ============================================================================
# SIMPLE EXAM END-TO-END TEST SCRIPT
# Tests: Generate -> Submit MCQ -> Submit Written Answer -> Get Feedback
# ============================================================================

$baseUrl = "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net"
# Uncomment for local testing:
# $baseUrl = "http://localhost:8080"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "SIMPLE EXAM E2E TEST" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# ============================================================================
# STEP 1: Generate Simple Test Exam (1 MCQ + 1 Subjective)
# ============================================================================
Write-Host "[STEP 1] Generating simple test exam..." -ForegroundColor Yellow

$generateRequest = @{
    subject = "Mathematics"
    grade = "2nd PUC"
} | ConvertTo-Json

try {
    $exam = Invoke-RestMethod -Uri "$baseUrl/api/exam/generate" -Method POST -Body $generateRequest -ContentType "application/json"
    
    Write-Host "[OK] Exam generated successfully!" -ForegroundColor Green
    Write-Host "    Exam ID: $($exam.examId)" -ForegroundColor White
    Write-Host "    Subject: $($exam.subject)" -ForegroundColor White
    Write-Host "    Total Marks: $($exam.totalMarks)" -ForegroundColor White
    Write-Host "    Question Count: $($exam.questionCount)" -ForegroundColor White
    
    if ($exam.parts) {
        Write-Host "`n    Parts:" -ForegroundColor White
        foreach ($part in $exam.parts) {
            Write-Host "      - $($part.partName): $($part.totalQuestions) questions ($($part.marksPerQuestion) marks each)" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "[ERROR] Failed to generate exam" -ForegroundColor Red
    Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
        $reader.Close()
    }
    exit 1
}

# ============================================================================
# STEP 2: Extract Questions
# ============================================================================
Write-Host "`n[STEP 2] Extracting questions..." -ForegroundColor Yellow

$mcqQuestion = $null
$subjectiveQuestion = $null

foreach ($part in $exam.parts) {
    if ($part.questionType -like "*MCQ*") {
        $mcqQuestion = $part.questions[0]
        Write-Host "[OK] Found MCQ question:" -ForegroundColor Green
        Write-Host "    Q$($mcqQuestion.questionNumber): $($mcqQuestion.questionText)" -ForegroundColor White
        Write-Host "    Options: $($mcqQuestion.options -join ', ')" -ForegroundColor Gray
        Write-Host "    Correct: $($mcqQuestion.correctAnswer)" -ForegroundColor Gray
    } elseif ($part.marksPerQuestion -ge 5) {
        $subjectiveQuestion = $part.questions[0]
        Write-Host "[OK] Found Subjective question:" -ForegroundColor Green
        Write-Host "    Q$($subjectiveQuestion.questionNumber): $($subjectiveQuestion.questionText)" -ForegroundColor White
        Write-Host "    Marks: $($part.marksPerQuestion)" -ForegroundColor Gray
    }
}

if (-not $mcqQuestion -or -not $subjectiveQuestion) {
    Write-Host "[ERROR] Could not find required questions" -ForegroundColor Red
    exit 1
}

# ============================================================================
# STEP 3: Submit Written Answer for Subjective Question
# ============================================================================
Write-Host "`n[STEP 3] Submitting written answer..." -ForegroundColor Yellow

# Create a mock PDF with student's answer
$studentAnswer = @"
The Pythagorean theorem states that in a right triangle, 
a^2 + b^2 = c^2

For a triangle with sides 3 and 4:
Step 1: 3^2 + 4^2 = c^2
Step 2: 9 + 16 = c^2
Step 3: c^2 = 25
Step 4: c = 5

The hypotenuse is 5 units.
"@

$pdfContent = @"
%PDF-1.4
1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj
2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj
3 0 obj<</Type/Page/MediaBox[0 0 612 792]/Parent 2 0 R/Resources<</Font<</F1 4 0 R>>>>/Contents 5 0 R>>endobj
4 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj
5 0 obj<</Length 500>>stream
BT
/F1 12 Tf
50 700 Td
(Student Answer Sheet) Tj
0 -30 Td
(Question 2: Pythagorean Theorem) Tj
0 -30 Td
(The Pythagorean theorem: a^2 + b^2 = c^2) Tj
0 -20 Td
(For sides 3 and 4:) Tj
0 -20 Td
(Step 1: 3^2 + 4^2 = c^2) Tj
0 -20 Td
(Step 2: 9 + 16 = c^2) Tj
0 -20 Td
(Step 3: 25 = c^2) Tj
0 -20 Td
(Step 4: c = 5) Tj
0 -20 Td
(Answer: The hypotenuse is 5 units.) Tj
ET
endstream endobj
xref
0 6
trailer<</Size 6/Root 1 0 R>>
startxref
700
%%EOF
"@

$tempPdfPath = [System.IO.Path]::GetTempFileName() + ".pdf"
[System.IO.File]::WriteAllText($tempPdfPath, $pdfContent)

try {
    # Create multipart form data
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"
    
    $bodyLines = @(
        "--$boundary",
        "Content-Disposition: form-data; name=`"examId`"$LF",
        $exam.examId,
        "--$boundary",
        "Content-Disposition: form-data; name=`"questionId`"$LF",
        $subjectiveQuestion.questionId,
        "--$boundary",
        "Content-Disposition: form-data; name=`"studentId`"$LF",
        "student-test-123",
        "--$boundary",
        "Content-Disposition: form-data; name=`"questionNumber`"$LF",
        $subjectiveQuestion.questionNumber,
        "--$boundary",
        "Content-Disposition: form-data; name=`"file`"; filename=`"answer.pdf`"",
        "Content-Type: application/pdf$LF",
        [System.IO.File]::ReadAllText($tempPdfPath),
        "--$boundary--"
    ) -join $LF
    
    $response = Invoke-RestMethod -Uri "$baseUrl/api/exam/submit-written-answers" `
        -Method POST `
        -ContentType "multipart/form-data; boundary=$boundary" `
        -Body $bodyLines
    
    Write-Host "[OK] Written answer submitted!" -ForegroundColor Green
    Write-Host "    Submission ID: $($response.submissionId)" -ForegroundColor White
    Write-Host "    Status: $($response.status)" -ForegroundColor White
    Write-Host "    Message: $($response.message)" -ForegroundColor White
    
    $submissionId = $response.submissionId
    
} catch {
    Write-Host "[ERROR] Failed to submit written answer" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
        $reader.Close()
    }
    exit 1
} finally {
    Remove-Item $tempPdfPath -ErrorAction SilentlyContinue
}

# ============================================================================
# STEP 4: Check Submission Status (Poll until complete)
# ============================================================================
Write-Host "`n[STEP 4] Checking submission status..." -ForegroundColor Yellow

$maxAttempts = 20
$attempt = 0
$status = "processing"

while ($status -eq "processing" -and $attempt -lt $maxAttempts) {
    $attempt++
    Start-Sleep -Seconds 3
    
    try {
        $statusResponse = Invoke-RestMethod -Uri "$baseUrl/api/exam/submission-status/$submissionId" -Method GET
        $status = $statusResponse.status
        
        Write-Host "    Attempt $attempt/$maxAttempts - Status: $status" -ForegroundColor Gray
        
        if ($status -eq "completed") {
            Write-Host "[OK] Evaluation completed!" -ForegroundColor Green
            break
        } elseif ($status -eq "failed") {
            Write-Host "[ERROR] Evaluation failed" -ForegroundColor Red
            Write-Host "Error: $($statusResponse.error)" -ForegroundColor Red
            exit 1
        }
    } catch {
        Write-Host "[ERROR] Failed to check status" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

if ($status -ne "completed") {
    Write-Host "[ERROR] Evaluation timed out" -ForegroundColor Red
    exit 1
}

# ============================================================================
# STEP 5: Get Feedback with Step-by-Step Evaluation
# ============================================================================
Write-Host "`n[STEP 5] Fetching detailed feedback..." -ForegroundColor Yellow

try {
    $feedback = Invoke-RestMethod -Uri "$baseUrl/api/exam/submission-feedback/$submissionId" -Method GET
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "EVALUATION RESULTS" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
    
    # Score Summary
    Write-Host "[SCORE SUMMARY]" -ForegroundColor Yellow
    Write-Host "  Total Score: $($feedback.totalScore)/$($feedback.totalMarks)" -ForegroundColor White
    Write-Host "  Percentage: $($feedback.percentage)%" -ForegroundColor White
    Write-Host "  Grade: $($feedback.grade)" -ForegroundColor $(if ($feedback.percentage -ge 80) { "Green" } elseif ($feedback.percentage -ge 60) { "Yellow" } else { "Red" })
    
    # MCQ Results
    if ($feedback.mcqResults -and $feedback.mcqResults.Count -gt 0) {
        Write-Host "`n[MCQ RESULTS]" -ForegroundColor Yellow
        foreach ($mcq in $feedback.mcqResults) {
            $icon = if ($mcq.isCorrect) { "[OK]" } else { "[X]" }
            $color = if ($mcq.isCorrect) { "Green" } else { "Red" }
            
            Write-Host "  Q$($mcq.questionNumber): $icon" -ForegroundColor $color
            Write-Host "    Student Answer: $($mcq.studentAnswer)" -ForegroundColor Gray
            Write-Host "    Correct Answer: $($mcq.correctAnswer)" -ForegroundColor Gray
            Write-Host "    Score: $($mcq.score)/$($mcq.maxMarks)" -ForegroundColor Gray
        }
    }
    
    # Written Answer Results with Step-by-Step Evaluation
    if ($feedback.writtenResults -and $feedback.writtenResults.Count -gt 0) {
        Write-Host "`n[WRITTEN ANSWER RESULTS]" -ForegroundColor Yellow
        foreach ($written in $feedback.writtenResults) {
            Write-Host "  Q$($written.questionNumber): $($written.questionText)" -ForegroundColor White
            Write-Host "  Score: $($written.totalScore)/$($written.maxMarks)" -ForegroundColor White
            
            Write-Host "`n  [STUDENT ANSWER]" -ForegroundColor Cyan
            Write-Host "  $($written.extractedAnswer)" -ForegroundColor Gray
            
            Write-Host "`n  [EXPECTED ANSWER]" -ForegroundColor Cyan
            Write-Host "  $($written.expectedAnswer)" -ForegroundColor Gray
            
            if ($written.stepEvaluations -and $written.stepEvaluations.Count -gt 0) {
                Write-Host "`n  [STEP-BY-STEP EVALUATION]" -ForegroundColor Cyan
                foreach ($step in $written.stepEvaluations) {
                    $stepIcon = if ($step.marksAwarded -eq $step.maxMarks) { "[OK]" } else { "[X]" }
                    $stepColor = if ($step.marksAwarded -eq $step.maxMarks) { "Green" } elseif ($step.marksAwarded -gt 0) { "Yellow" } else { "Red" }
                    
                    Write-Host "  Step $($step.stepNumber): $stepIcon $($step.description)" -ForegroundColor $stepColor
                    Write-Host "    Marks: $($step.marksAwarded)/$($step.maxMarks)" -ForegroundColor Gray
                    Write-Host "    Feedback: $($step.feedback)" -ForegroundColor Gray
                }
            }
            
            Write-Host "`n  [OVERALL FEEDBACK]" -ForegroundColor Cyan
            Write-Host "  $($written.overallFeedback)" -ForegroundColor Gray
        }
    }
    
    # Recommendations
    if ($feedback.recommendations -and $feedback.recommendations.Count -gt 0) {
        Write-Host "`n[RECOMMENDATIONS]" -ForegroundColor Yellow
        foreach ($rec in $feedback.recommendations) {
            Write-Host "  - $rec" -ForegroundColor Gray
        }
    }
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "[OK] TEST COMPLETED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "========================================`n" -ForegroundColor Cyan
    
} catch {
    Write-Host "[ERROR] Failed to fetch feedback" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
        $reader.Close()
    }
    exit 1
}
