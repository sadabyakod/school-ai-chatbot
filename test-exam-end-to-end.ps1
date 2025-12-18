# End-to-End Exam System Test
# Tests: Generate Questions -> Upload Answer Sheet -> Check Status -> Get Feedback

$API_BASE = "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net/api"
# $API_BASE = "http://localhost:8080/api"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "EXAM SYSTEM - END TO END TEST" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Step 1: Generate Exam Questions
Write-Host "STEP 1: Generating Exam Questions..." -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray

$generateBody = @{
    subject = "Mathematics"
    grade = "10"
    medium = "English"
    totalMarks = 20
    writtenAnswerMarks = 15
    mcqMarks = 5
} | ConvertTo-Json

try {
    $examResponse = Invoke-RestMethod -Uri "$API_BASE/exam/generate" `
        -Method POST `
        -Body $generateBody `
        -ContentType "application/json"
    
    Write-Host "✓ Exam generated successfully!" -ForegroundColor Green
    Write-Host "  Exam ID: $($examResponse.examId)" -ForegroundColor White
    Write-Host "  Total Questions: $($examResponse.totalQuestions)" -ForegroundColor White
    Write-Host "  MCQs: $($examResponse.mcqs.Count)" -ForegroundColor White
    Write-Host "  Written Questions: $($examResponse.writtenQuestions.Count)" -ForegroundColor White
    
    $examId = $examResponse.examId
    
    # Display generated questions
    Write-Host "`n  Generated Questions:" -ForegroundColor Cyan
    Write-Host "  -------------------" -ForegroundColor Gray
    
    if ($examResponse.mcqs) {
        Write-Host "`n  MCQ Questions:" -ForegroundColor Magenta
        foreach ($mcq in $examResponse.mcqs) {
            Write-Host "    Q$($mcq.questionNumber). $($mcq.questionText) [$($mcq.marks) marks]" -ForegroundColor White
            Write-Host "       Correct Answer: $($mcq.correctAnswer)" -ForegroundColor Green
        }
    }
    
    if ($examResponse.writtenQuestions) {
        Write-Host "`n  Written Answer Questions:" -ForegroundColor Magenta
        foreach ($wq in $examResponse.writtenQuestions) {
            Write-Host "`n    Q$($wq.questionNumber). $($wq.questionText) [$($wq.marks) marks]" -ForegroundColor White
            if ($wq.rubric -and $wq.rubric.steps) {
                Write-Host "       Rubric Steps ($($wq.rubric.steps.Count)):" -ForegroundColor Yellow
                foreach ($step in $wq.rubric.steps) {
                    Write-Host "         - $($step.description) [$($step.marks) marks]" -ForegroundColor Gray
                }
            }
        }
    }
}
catch {
    Write-Host "✗ Failed to generate exam" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "  Response: $responseBody" -ForegroundColor Red
    }
    exit 1
}

Start-Sleep -Seconds 2

# Step 2: Upload Student Answer Sheet
Write-Host "`n`nSTEP 2: Uploading Student Answer Sheet..." -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray

# Create a test PDF file (mock student answer sheet)
$testPdfPath = "$env:TEMP\student_answer_sheet_$examId.pdf"

# Create a simple PDF with proper escaping
$pdfContent = @'
%PDF-1.4
1 0 obj
<< /Type /Catalog /Pages 2 0 R >>
endobj
2 0 obj
<< /Type /Pages /Kids [3 0 R] /Count 1 >>
endobj
3 0 obj
<< /Type /Page /Parent 2 0 R /Resources 4 0 R /MediaBox [0 0 612 792] /Contents 5 0 R >>
endobj
4 0 obj
<< /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >> >>
endobj
5 0 obj
<< /Length 300 >>
stream
BT
/F1 12 Tf
50 700 Td
(Student Answer Sheet) Tj
0 -20 Td
(Mathematics - Grade 10) Tj
0 -40 Td
(Q1. The area of a circle is calculated using A = pi r squared) Tj
0 -20 Td
(Q2. Volume of sphere equals 4/3 times pi r cubed) Tj
0 -20 Td
(Q3. Pythagoras theorem states a squared plus b squared equals c squared) Tj
ET
endstream
endobj
xref
0 6
0000000000 65535 f 
0000000009 00000 n 
0000000058 00000 n 
0000000115 00000 n 
0000000214 00000 n 
0000000308 00000 n 
trailer
<< /Size 6 /Root 1 0 R >>
startxref
650
%%EOF
'@
    
try {
    [System.IO.File]::WriteAllText($testPdfPath, $pdfContent)
    Write-Host "  Created test answer sheet: $testPdfPath" -ForegroundColor Gray
}
catch {
    Write-Host "  Warning: Could not create PDF, using text file instead" -ForegroundColor Yellow
    "Student Answer Sheet for Exam $examId" | Out-File -FilePath $testPdfPath -Encoding UTF8
}

# Upload using multipart form data
try {
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"
    
    $fileBytes = [System.IO.File]::ReadAllBytes($testPdfPath)
    $fileName = "student_answers_$examId.pdf"
    
    $bodyLines = (
        "--$boundary",
        "Content-Disposition: form-data; name=`"file`"; filename=`"$fileName`"",
        "Content-Type: application/pdf$LF",
        [System.Text.Encoding]::GetEncoding("iso-8859-1").GetString($fileBytes),
        "--$boundary",
        "Content-Disposition: form-data; name=`"studentName`"$LF",
        "Test Student",
        "--$boundary",
        "Content-Disposition: form-data; name=`"examId`"$LF",
        "$examId",
        "--$boundary--$LF"
    ) -join $LF
    
    $uploadResponse = Invoke-RestMethod -Uri "$API_BASE/exam/submit-written-answers" `
        -Method POST `
        -ContentType "multipart/form-data; boundary=$boundary" `
        -Body $bodyLines
    
    Write-Host "✓ Answer sheet uploaded successfully!" -ForegroundColor Green
    Write-Host "  Submission ID: $($uploadResponse.submissionId)" -ForegroundColor White
    Write-Host "  Status: $($uploadResponse.status)" -ForegroundColor White
    Write-Host "  Message: $($uploadResponse.message)" -ForegroundColor Cyan
    
    $submissionId = $uploadResponse.submissionId
}
catch {
    Write-Host "✗ Failed to upload answer sheet" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "  Response: $responseBody" -ForegroundColor Red
    }
    
    # Clean up
    Remove-Item $testPdfPath -ErrorAction SilentlyContinue
    exit 1
}

# Clean up test file
Remove-Item $testPdfPath -ErrorAction SilentlyContinue

Start-Sleep -Seconds 2

# Step 3: Check Status (poll until evaluation is complete)
Write-Host "`n`nSTEP 3: Checking Evaluation Status..." -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray

$maxAttempts = 10
$attemptCount = 0
$evaluationComplete = $false

while (-not $evaluationComplete -and $attemptCount -lt $maxAttempts) {
    $attemptCount++
    Write-Host "  Attempt $attemptCount/$maxAttempts..." -ForegroundColor Gray
    
    try {
        $statusResponse = Invoke-RestMethod -Uri "$API_BASE/exam/submission-status/$submissionId" `
            -Method GET
        
        Write-Host "    Status: $($statusResponse.status)" -ForegroundColor $(
            if ($statusResponse.status -eq "completed") { "Green" }
            elseif ($statusResponse.status -eq "evaluating" -or $statusResponse.status -eq "processing") { "Yellow" }
            else { "White" }
        )
        
        if ($statusResponse.status -eq "completed") {
            $evaluationComplete = $true
            Write-Host "`n✓ Evaluation completed!" -ForegroundColor Green
            
            if ($statusResponse.totalMarks) {
                Write-Host "  Total Marks: $($statusResponse.marksObtained)/$($statusResponse.totalMarks)" -ForegroundColor Cyan
            }
        }
        elseif ($statusResponse.status -eq "failed" -or $statusResponse.status -eq "error") {
            Write-Host "✗ Evaluation failed!" -ForegroundColor Red
            Write-Host "  Error: $($statusResponse.message)" -ForegroundColor Red
            exit 1
        }
        else {
            Write-Host "    Still processing... waiting 3 seconds" -ForegroundColor Yellow
            Start-Sleep -Seconds 3
        }
    }
    catch {
        Write-Host "  ✗ Error checking status: $($_.Exception.Message)" -ForegroundColor Red
        Start-Sleep -Seconds 3
    }
}

if (-not $evaluationComplete) {
    Write-Host "`n✗ Evaluation did not complete within timeout period" -ForegroundColor Red
    Write-Host "  You can check status later using submission ID: $submissionId" -ForegroundColor Yellow
    exit 1
}

Start-Sleep -Seconds 1

# Step 4: Get Detailed Feedback
Write-Host "`n`nSTEP 4: Fetching Detailed Feedback..." -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray

try {
    $feedbackResponse = Invoke-RestMethod -Uri "$API_BASE/exam/submission-feedback/$submissionId" `
        -Method GET
    
    Write-Host "✓ Feedback retrieved successfully!`n" -ForegroundColor Green
    
    # Display overall results
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "EVALUATION RESULTS" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    
    Write-Host "`nStudent: $($feedbackResponse.studentName)" -ForegroundColor White
    Write-Host "Exam ID: $($feedbackResponse.examId)" -ForegroundColor White
    Write-Host "Subject: $($feedbackResponse.subject)" -ForegroundColor White
    Write-Host "Grade: $($feedbackResponse.grade)" -ForegroundColor White
    
    Write-Host "`n📊 SCORE SUMMARY:" -ForegroundColor Magenta
    Write-Host "   Total Marks: $($feedbackResponse.marksObtained)/$($feedbackResponse.totalMarks)" -ForegroundColor Cyan
    $percentage = [math]::Round(($feedbackResponse.marksObtained / $feedbackResponse.totalMarks) * 100, 2)
    Write-Host "   Percentage: $percentage%" -ForegroundColor Cyan
    
    # Display MCQ Results
    if ($feedbackResponse.mcqResults -and $feedbackResponse.mcqResults.Count -gt 0) {
        Write-Host "`n`n📝 MCQ RESULTS:" -ForegroundColor Magenta
        Write-Host "   MCQ Score: $($feedbackResponse.mcqMarksObtained)/$($feedbackResponse.mcqTotalMarks)" -ForegroundColor Cyan
        
        foreach ($mcq in $feedbackResponse.mcqResults) {
            $icon = if ($mcq.isCorrect) { "[OK]" } else { "[X]" }
            $color = if ($mcq.isCorrect) { "Green" } else { "Red" }
            
            Write-Host "`n   $icon Q$($mcq.questionNumber): $($mcq.questionText)" -ForegroundColor $color
            Write-Host "      Student Answer: $($mcq.studentAnswer)" -ForegroundColor White
            Write-Host "      Correct Answer: $($mcq.correctAnswer)" -ForegroundColor Green
            Write-Host "      Marks: $($mcq.marksAwarded)/$($mcq.totalMarks)" -ForegroundColor Cyan
        }
    }
    
    # Display Written Answer Results with Step-by-Step Evaluation
    if ($feedbackResponse.writtenResults -and $feedbackResponse.writtenResults.Count -gt 0) {
        Write-Host "`n`n✍️  WRITTEN ANSWER RESULTS:" -ForegroundColor Magenta
        Write-Host "   Written Score: $($feedbackResponse.writtenMarksObtained)/$($feedbackResponse.writtenTotalMarks)" -ForegroundColor Cyan
        
        foreach ($written in $feedbackResponse.writtenResults) {
            Write-Host "`n   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
            Write-Host "   Q$($written.questionNumber): $($written.questionText)" -ForegroundColor Yellow
            Write-Host "   Total Marks: $($written.marksAwarded)/$($written.totalMarks)" -ForegroundColor Cyan
            
            Write-Host "`n   📄 STUDENT ANSWER:" -ForegroundColor White
            Write-Host "   $($written.studentAnswer)" -ForegroundColor Gray
            
            Write-Host "`n   ✅ EXPECTED ANSWER:" -ForegroundColor Green
            Write-Host "   $($written.expectedAnswer)" -ForegroundColor Gray
            
            if ($written.stepEvaluation -and $written.stepEvaluation.Count -gt 0) {
                Write-Host "`n   📋 STEP-BY-STEP EVALUATION:" -ForegroundColor Magenta
                
                $stepNumber = 1
                foreach ($step in $written.stepEvaluation) {
                    $stepIcon = if ($step.achieved) { "[OK]" } else { "[X]" }
                    $stepColor = if ($step.achieved) { "Green" } else { "Red" }
                    
                    Write-Host "`n      $stepIcon Step ${stepNumber}: $($step.description)" -ForegroundColor $stepColor
                    Write-Host "         Marks Awarded: $($step.marksAwarded)/$($step.maxMarks)" -ForegroundColor Cyan
                    Write-Host "         Status: $($step.status)" -ForegroundColor $(
                        if ($step.status -eq "Fully Achieved") { "Green" }
                        elseif ($step.status -eq "Partially Achieved") { "Yellow" }
                        else { "Red" }
                    )
                    
                    if ($step.feedback) {
                        Write-Host "         Feedback: $($step.feedback)" -ForegroundColor White
                    }
                    
                    $stepNumber++
                }
            }
            
            Write-Host "`n   💬 OVERALL FEEDBACK:" -ForegroundColor Cyan
            Write-Host "   $($written.feedback)" -ForegroundColor White
        }
    }
    
    # Overall feedback
    if ($feedbackResponse.overallFeedback) {
        Write-Host "`n`n========================================" -ForegroundColor Cyan
        Write-Host "OVERALL FEEDBACK" -ForegroundColor Cyan
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host "$($feedbackResponse.overallFeedback)" -ForegroundColor White
    }
    
    # Recommendations
    if ($feedbackResponse.recommendations -and $feedbackResponse.recommendations.Count -gt 0) {
        Write-Host "`n`n💡 RECOMMENDATIONS:" -ForegroundColor Yellow
        foreach ($rec in $feedbackResponse.recommendations) {
            Write-Host "   • $rec" -ForegroundColor White
        }
    }
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "TEST COMPLETED SUCCESSFULLY! ✓" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
}
catch {
    Write-Host "✗ Failed to retrieve feedback" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "  Response: $responseBody" -ForegroundColor Red
    }
    exit 1
}

Write-Host "`n"
