# Complete E2E Test: Generate Exam -> Upload Answer Sheet -> Poll Status -> Get Detailed Feedback
$baseUrl = "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net"
$studentId = "student_test_" + (Get-Date -Format "yyyyMMddHHmmss")

Write-Host ""
Write-Host ("="*80) -ForegroundColor Cyan
Write-Host "COMPLETE END-TO-END EXAM EVALUATION TEST" -ForegroundColor Cyan
Write-Host ("="*80) -ForegroundColor Cyan
Write-Host ""

# STEP 1: Generate Exam
Write-Host "[STEP 1] Generating exam with 5 questions..." -ForegroundColor Yellow
$examBody = @{
    subject = "Mathematics"
    grade = "2nd PUC"
} | ConvertTo-Json

try {
    $exam = Invoke-RestMethod -Uri "$baseUrl/api/exam/generate" -Method POST -Body $examBody -ContentType "application/json"
    $examId = $exam.examId
    
    Write-Host "Success! Exam Generated!" -ForegroundColor Green
    Write-Host "   Exam ID: $examId" -ForegroundColor White
    Write-Host "   Total Marks: $($exam.totalMarks)" -ForegroundColor White
    Write-Host "   Questions: $($exam.questionCount)" -ForegroundColor White
    
    Write-Host ""
    Write-Host "   Questions Overview:" -ForegroundColor Cyan
    foreach ($part in $exam.parts) {
        Write-Host "   - $($part.partName): $($part.totalQuestions) questions ($($part.questionType))" -ForegroundColor Gray
        foreach ($q in $part.questions) {
            $preview = $q.questionText.Substring(0, [Math]::Min(60, $q.questionText.Length))
            Write-Host "     Q$($q.questionNumber): $preview..." -ForegroundColor DarkGray
        }
    }
    Write-Host ""
} catch {
    Write-Host "Failed to generate exam" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# STEP 2: Create Mock Answer Sheet PDF
Write-Host "[STEP 2] Creating mock student answer sheet PDF..." -ForegroundColor Yellow

$pdfPath = "$env:TEMP\student_answer_$studentId.pdf"
$pdfContent = @"
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
<< /Length 450 >>
stream
BT
/F1 12 Tf
50 750 Td
(Student Answer Sheet) Tj
0 -30 Td
(Exam ID: $examId) Tj
0 -30 Td
(Student ID: $studentId) Tj
0 -40 Td
(Q3: Pythagorean Theorem Answer:) Tj
0 -20 Td
(The Pythagorean theorem states a2 + b2 = c2) Tj
0 -15 Td
(For triangle with sides 3 and 4:) Tj
0 -15 Td
(32 + 42 = 9 + 16 = 25) Tj
0 -15 Td
(c = sqrt25 = 5) Tj
0 -40 Td
(Q4: Quadratic Equation x2 - 5x + 6 = 0) Tj
0 -20 Td
(Factoring: (x-2)(x-3) = 0) Tj
0 -15 Td
(Solutions: x = 2 or x = 3) Tj
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
0000000293 00000 n
trailer
<< /Size 6 /Root 1 0 R >>
startxref
794
%%EOF
"@

[System.IO.File]::WriteAllText($pdfPath, $pdfContent)
Write-Host "Mock PDF created at: $pdfPath" -ForegroundColor Green
Write-Host "   Contains answers for Q3 (Pythagorean) and Q4 (Quadratic)" -ForegroundColor Gray
Write-Host ""

# STEP 3: Upload Answer Sheet
Write-Host "[STEP 3] Uploading student answer sheet..." -ForegroundColor Yellow

try {
    Add-Type -AssemblyName System.Net.Http
    
    $httpClient = New-Object System.Net.Http.HttpClient
    $content = New-Object System.Net.Http.MultipartFormDataContent
    
    # Add examId
    $examIdContent = New-Object System.Net.Http.StringContent($examId)
    $content.Add($examIdContent, "examId")
    
    # Add studentId
    $studentIdContent = New-Object System.Net.Http.StringContent($studentId)
    $content.Add($studentIdContent, "studentId")
    
    # Add PDF file
    $fileStream = [System.IO.File]::OpenRead($pdfPath)
    $fileContent = New-Object System.Net.Http.StreamContent($fileStream)
    $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("application/pdf")
    $content.Add($fileContent, "files", "answer.pdf")
    
    # Send request
    $response = $httpClient.PostAsync("$baseUrl/api/exam/upload-written", $content).Result
    $responseContent = $response.Content.ReadAsStringAsync().Result
    
    $fileStream.Close()
    $httpClient.Dispose()
    
    if ($response.IsSuccessStatusCode) {
        $uploadResponse = $responseContent | ConvertFrom-Json
        $submissionId = $uploadResponse.submissionId
        
        Write-Host "Answer sheet uploaded successfully!" -ForegroundColor Green
        Write-Host "   Submission ID: $submissionId" -ForegroundColor White
        Write-Host "   Status: $($uploadResponse.status)" -ForegroundColor White
        Write-Host ""
    } else {
        Write-Host "Failed to upload answer sheet - Status: $($response.StatusCode)" -ForegroundColor Red
        Write-Host $responseContent -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "Failed to upload answer sheet" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# STEP 4: Poll Evaluation Status
Write-Host "[STEP 4] Polling evaluation status (max 30 attempts, 5s interval)..." -ForegroundColor Yellow

$maxAttempts = 30
$pollInterval = 5
$attempt = 0
$isComplete = $false

while ($attempt -lt $maxAttempts -and -not $isComplete) {
    $attempt++
    Write-Host "   Attempt $attempt of $maxAttempts - Checking status..." -ForegroundColor Gray
    
    try {
        $statusResponse = Invoke-RestMethod -Uri "$baseUrl/api/exam/status/$submissionId" -Method GET
        
        $status = $statusResponse.status
        $statusColor = "Yellow"
        if ($status -eq "Completed") { $statusColor = "Green" }
        elseif ($status -eq "Failed") { $statusColor = "Red" }
        
        Write-Host "   Status: $status" -ForegroundColor $statusColor
        
        if ($status -eq "Completed") {
            $isComplete = $true
            Write-Host "Evaluation completed!" -ForegroundColor Green
            break
        } elseif ($status -eq "Failed") {
            Write-Host "Evaluation failed" -ForegroundColor Red
            if ($statusResponse.errorMessage) {
                Write-Host $statusResponse.errorMessage -ForegroundColor Red
            }
            exit 1
        }
        
        if ($attempt -lt $maxAttempts) {
            Start-Sleep -Seconds $pollInterval
        }
    } catch {
        Write-Host "   Error checking status" -ForegroundColor Yellow
        Write-Host "   " + $_.Exception.Message -ForegroundColor Yellow
        if ($attempt -lt $maxAttempts) {
            Start-Sleep -Seconds $pollInterval
        }
    }
}

if (-not $isComplete) {
    Write-Host "Evaluation did not complete within timeout period" -ForegroundColor Red
    exit 1
}

Write-Host ""

# STEP 5: Fetch Detailed Feedback
Write-Host "[STEP 5] Fetching detailed evaluation feedback..." -ForegroundColor Yellow

try {
    $resultResponse = Invoke-RestMethod -Uri "$baseUrl/api/exam/result/$examId/$studentId" -Method GET
    
    Write-Host "Detailed feedback retrieved!" -ForegroundColor Green
    Write-Host ""
    
    # Display Summary
    Write-Host ("="*80) -ForegroundColor Cyan
    Write-Host "EVALUATION SUMMARY" -ForegroundColor Cyan
    Write-Host ("="*80) -ForegroundColor Cyan
    
    Write-Host "Exam ID:        $($resultResponse.examId)" -ForegroundColor White
    Write-Host "Student ID:     $($resultResponse.studentId)" -ForegroundColor White
    Write-Host "Exam Title:     $($resultResponse.examTitle)" -ForegroundColor White
    Write-Host ""
    Write-Host "MCQ Score:      $($resultResponse.mcqScore) / $($resultResponse.mcqTotalMarks)" -ForegroundColor Cyan
    Write-Host "Subjective:     $($resultResponse.subjectiveScore) / $($resultResponse.subjectiveTotalMarks)" -ForegroundColor Cyan
    Write-Host "GRAND TOTAL:    $($resultResponse.grandScore) / $($resultResponse.grandTotalMarks)" -ForegroundColor Yellow
    Write-Host "Percentage:     $($resultResponse.percentage)%" -ForegroundColor $(if ($resultResponse.percentage -ge 35) { "Green" } else { "Red" })
    Write-Host "Grade:          $($resultResponse.grade)" -ForegroundColor Yellow
    $passStatus = if ($resultResponse.passed) { "PASSED" } else { "FAILED" }
    Write-Host "Status:         $passStatus" -ForegroundColor $(if ($resultResponse.passed) { "Green" } else { "Red" })
    Write-Host ""
    
    # Display MCQ Results
    if ($resultResponse.mcqResults -and $resultResponse.mcqResults.Count -gt 0) {
        Write-Host ("="*80) -ForegroundColor Cyan
        Write-Host "MCQ RESULTS" -ForegroundColor Cyan
        Write-Host ("="*80) -ForegroundColor Cyan
        
        foreach ($mcq in $resultResponse.mcqResults) {
            $color = if ($mcq.isCorrect) { "Green" } else { "Red" }
            $icon = if ($mcq.isCorrect) { "CORRECT" } else { "WRONG" }
            
            Write-Host ""
            Write-Host "$icon - Question $($mcq.questionId)" -ForegroundColor $color
            Write-Host "   Your Answer:     $($mcq.selectedOption)" -ForegroundColor White
            Write-Host "   Correct Answer:  $($mcq.correctAnswer)" -ForegroundColor Gray
            Write-Host "   Marks Awarded:   $($mcq.marksAwarded)" -ForegroundColor Yellow
        }
        Write-Host ""
    }
    
    # Display Subjective Results with Step-by-Step Analysis
    if ($resultResponse.subjectiveResults -and $resultResponse.subjectiveResults.Count -gt 0) {
        Write-Host ("="*80) -ForegroundColor Cyan
        Write-Host "SUBJECTIVE QUESTIONS - DETAILED EVALUATION" -ForegroundColor Cyan
        Write-Host ("="*80) -ForegroundColor Cyan
        
        foreach ($subj in $resultResponse.subjectiveResults) {
            Write-Host ""
            Write-Host ("-"*80) -ForegroundColor DarkGray
            Write-Host "QUESTION $($subj.questionNumber) (ID: $($subj.questionId))" -ForegroundColor Yellow
            Write-Host ("-"*80) -ForegroundColor DarkGray
            
            # Question Text
            Write-Host ""
            Write-Host "QUESTION:" -ForegroundColor Cyan
            Write-Host $subj.questionText -ForegroundColor White
            
            # Student Answer
            Write-Host ""
            Write-Host "YOUR ANSWER:" -ForegroundColor Cyan
            Write-Host $subj.studentAnswerEcho -ForegroundColor White
            
            # Marks
            $marksColor = "Yellow"
            if ($subj.isFullyCorrect) { $marksColor = "Green" }
            elseif ($subj.earnedMarks -eq 0) { $marksColor = "Red" }
            
            Write-Host ""
            Write-Host "MARKS AWARDED: $($subj.earnedMarks) / $($subj.maxMarks)" -ForegroundColor $marksColor
            
            # Step-by-Step Analysis
            if ($subj.stepAnalysis -and $subj.stepAnalysis.Count -gt 0) {
                Write-Host ""
                Write-Host "STEP-BY-STEP EVALUATION:" -ForegroundColor Cyan
                
                foreach ($step in $subj.stepAnalysis) {
                    $stepColor = if ($step.isCorrect) { "Green" } else { "Red" }
                    $stepIcon = if ($step.isCorrect) { "CORRECT" } else { "WRONG" }
                    
                    Write-Host ""
                    Write-Host "   $stepIcon - Step $($step.step): $($step.description)" -ForegroundColor $stepColor
                    Write-Host "      Marks: $($step.marksAwarded) / $($step.maxMarksForStep)" -ForegroundColor Yellow
                    Write-Host "      Feedback: $($step.feedback)" -ForegroundColor Gray
                }
            }
            
            # Overall Feedback
            Write-Host ""
            Write-Host "OVERALL FEEDBACK:" -ForegroundColor Cyan
            Write-Host $subj.overallFeedback -ForegroundColor White
            
            # Expected Answer
            Write-Host ""
            Write-Host "EXPECTED ANSWER:" -ForegroundColor Cyan
            Write-Host $subj.expectedAnswer -ForegroundColor Gray
        }
    }
    
    Write-Host ""
    Write-Host ("="*80) -ForegroundColor Cyan
    Write-Host "TEST COMPLETED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host ("="*80) -ForegroundColor Cyan
    Write-Host ""
    
} catch {
    Write-Host "Failed to fetch feedback" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
} finally {
    # Cleanup
    if (Test-Path $pdfPath) {
        Remove-Item $pdfPath -Force
    }
}
