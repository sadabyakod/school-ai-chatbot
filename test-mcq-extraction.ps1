# Test MCQ Answer Extraction from Answer Sheets
# This script demonstrates the MCQ extraction and evaluation feature

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "MCQ Answer Extraction & Evaluation Test" -ForegroundColor Cyan
Write-Host "============================================`n" -ForegroundColor Cyan

$baseUrl = "http://localhost:8080"

# Step 1: Generate a test exam with MCQ questions
Write-Host "Step 1: Generating test exam with MCQ questions..." -ForegroundColor Yellow

$examRequest = @{
    subject = "Mathematics"
    chapter = "Algebra"
    difficulty = "medium"
    mcqCount = 5
    subjectiveCount = 2
    studentClass = "10th Grade"
} | ConvertTo-Json

try {
    $exam = Invoke-RestMethod -Uri "$baseUrl/api/exam/generate" `
        -Method POST `
        -Body $examRequest `
        -ContentType "application/json"
    
    Write-Host "✓ Exam generated successfully!" -ForegroundColor Green
    Write-Host "  Exam ID: $($exam.examId)" -ForegroundColor Gray
    Write-Host "  MCQ Questions: $($exam.parts[0].questions.Count)" -ForegroundColor Gray
    
    $examId = $exam.examId
    
    # Display MCQ questions and correct answers
    Write-Host "`nMCQ Questions and Correct Answers:" -ForegroundColor Cyan
    $questionNum = 1
    foreach ($question in $exam.parts[0].questions) {
        Write-Host "  Q$questionNum. $($question.questionText)" -ForegroundColor White
        Write-Host "     Correct Answer: $($question.correctAnswer)" -ForegroundColor Green
        $questionNum++
    }
}
catch {
    Write-Host "✗ Failed to generate exam: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Create a simulated answer sheet text
Write-Host "`n`nStep 2: Simulating student answer sheet upload..." -ForegroundColor Yellow

# Simulate student answers (some correct, some wrong)
$studentAnswers = @"
Student Answer Sheet - Mathematics Algebra Test
Name: Test Student
Roll No: 12345

MCQ Answers:
1) A
2) B
3) C
4) D
5) A

Subjective Answers:
Q6. Solve: x + 5 = 10
Answer: x = 5

Q7. Find the value of y if 2y = 12
Answer: y = 6
"@

# Create a temporary text file (simulating OCR output from image)
$tempFile = [System.IO.Path]::GetTempFileName()
$tempFile = [System.IO.Path]::ChangeExtension($tempFile, ".txt")
$studentAnswers | Out-File -FilePath $tempFile -Encoding UTF8

Write-Host "✓ Answer sheet created (simulated)" -ForegroundColor Green
Write-Host "  File: $tempFile" -ForegroundColor Gray

# Step 3: Upload answer sheet
Write-Host "`nStep 3: Uploading answer sheet..." -ForegroundColor Yellow

$studentId = "test-student-$(Get-Random -Maximum 9999)"

try {
    # Note: In production, this would be an image file
    # For testing, we're using a text file that simulates OCR output
    
    $form = @{
        examId = $examId
        studentId = $studentId
        files = Get-Item $tempFile
    }
    
    $uploadResult = Invoke-RestMethod -Uri "$baseUrl/api/exam/upload-written" `
        -Method POST `
        -Form $form
    
    Write-Host "✓ Answer sheet uploaded successfully!" -ForegroundColor Green
    Write-Host "  Submission ID: $($uploadResult.writtenSubmissionId)" -ForegroundColor Gray
    Write-Host "  Status: $($uploadResult.status)" -ForegroundColor Gray
    Write-Host "  Message: $($uploadResult.message)" -ForegroundColor Gray
}
catch {
    Write-Host "✗ Failed to upload answer sheet: $($_.Exception.Message)" -ForegroundColor Red
    Remove-Item $tempFile -ErrorAction SilentlyContinue
    exit 1
}

# Step 4: Wait for async processing
Write-Host "`nStep 4: Waiting for async processing (MCQ extraction + evaluation)..." -ForegroundColor Yellow
Write-Host "  Processing: OCR → MCQ Extraction → MCQ Evaluation → Subjective Evaluation" -ForegroundColor Gray

for ($i = 1; $i -le 10; $i++) {
    Write-Host "  ." -NoNewline -ForegroundColor Gray
    Start-Sleep -Seconds 1
}
Write-Host " Done!" -ForegroundColor Green

# Step 5: Retrieve exam result
Write-Host "`nStep 5: Retrieving exam result..." -ForegroundColor Yellow

try {
    $result = Invoke-RestMethod -Uri "$baseUrl/api/exam/result/$examId/$studentId" `
        -Method GET
    
    Write-Host "✓ Exam result retrieved successfully!" -ForegroundColor Green
    
    # Display MCQ Results
    Write-Host "`n" + ("="*80) -ForegroundColor Cyan
    Write-Host "MCQ RESULTS (Extracted from Answer Sheet)" -ForegroundColor Cyan
    Write-Host ("="*80) -ForegroundColor Cyan
    
    Write-Host "`nScore: $($result.mcqScore)/$($result.mcqTotalMarks)" -ForegroundColor $(if ($result.mcqScore -ge $result.mcqTotalMarks * 0.6) { "Green" } else { "Red" })
    Write-Host "Percentage: $([math]::Round(($result.mcqScore / $result.mcqTotalMarks * 100), 2))%`n" -ForegroundColor Gray
    
    $qNum = 1
    foreach ($mcqResult in $result.mcqResults) {
        $statusIcon = if ($mcqResult.isCorrect) { "✓" } else { "✗" }
        $statusColor = if ($mcqResult.isCorrect) { "Green" } else { "Red" }
        
        Write-Host "Q$qNum. " -NoNewline -ForegroundColor White
        Write-Host "$statusIcon " -NoNewline -ForegroundColor $statusColor
        Write-Host "Student Answer: $($mcqResult.selectedOption) | Correct: $($mcqResult.correctAnswer) | " -NoNewline -ForegroundColor Gray
        Write-Host "Marks: $($mcqResult.marksAwarded)" -ForegroundColor $(if ($mcqResult.isCorrect) { "Green" } else { "Red" })
        
        $qNum++
    }
    
    # Display Subjective Results
    Write-Host "`n" + ("="*80) -ForegroundColor Cyan
    Write-Host "SUBJECTIVE RESULTS" -ForegroundColor Cyan
    Write-Host ("="*80) -ForegroundColor Cyan
    
    Write-Host "`nScore: $($result.subjectiveScore)/$($result.subjectiveTotalMarks)" -ForegroundColor $(if ($result.subjectiveScore -ge $result.subjectiveTotalMarks * 0.6) { "Green" } else { "Yellow" })
    Write-Host "Questions Evaluated: $($result.subjectiveResults.Count)`n" -ForegroundColor Gray
    
    # Display Grand Total
    Write-Host ("="*80) -ForegroundColor Cyan
    Write-Host "FINAL RESULT" -ForegroundColor Cyan
    Write-Host ("="*80) -ForegroundColor Cyan
    
    Write-Host "`nGrand Total: $($result.grandScore)/$($result.grandTotalMarks)" -ForegroundColor $(if ($result.passed) { "Green" } else { "Red" })
    Write-Host "Percentage: $($result.percentage)%" -ForegroundColor $(if ($result.passed) { "Green" } else { "Red" })
    Write-Host "Grade: $($result.grade)" -ForegroundColor $(if ($result.percentage -ge 60) { "Green" } elseif ($result.percentage -ge 35) { "Yellow" } else { "Red" })
    Write-Host "Status: $(if ($result.passed) { "PASSED ✓" } else { "FAILED ✗" })" -ForegroundColor $(if ($result.passed) { "Green" } else { "Red" })
    
    if ($result.evaluatedAt) {
        Write-Host "Evaluated At: $($result.evaluatedAt)" -ForegroundColor Gray
    }
    
    Write-Host ""
}
catch {
    Write-Host "✗ Failed to retrieve result: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
    }
}
finally {
    # Cleanup
    Remove-Item $tempFile -ErrorAction SilentlyContinue
}

Write-Host "`n" + ("="*80) -ForegroundColor Cyan
Write-Host "TEST COMPLETED" -ForegroundColor Cyan
Write-Host ("="*80) -ForegroundColor Cyan

Write-Host "`nKey Features Demonstrated:" -ForegroundColor Yellow
Write-Host "  ✓ MCQ answer extraction from uploaded answer sheets" -ForegroundColor Green
Write-Host "  ✓ Automatic evaluation of extracted MCQ answers" -ForegroundColor Green
Write-Host "  ✓ Question-level MCQ results in API response" -ForegroundColor Green
Write-Host "  ✓ Integration with subjective answer evaluation" -ForegroundColor Green
Write-Host "  ✓ Consolidated exam result with both MCQ and subjective scores" -ForegroundColor Green

Write-Host "`nAnswer Formats Supported:" -ForegroundColor Yellow
Write-Host "  • 1) A, 2) B, 3) C" -ForegroundColor Gray
Write-Host "  • 1. A, 2. B, 3. C" -ForegroundColor Gray
Write-Host "  • 1: A, 2: B, 3: C" -ForegroundColor Gray
Write-Host "  • 1-A, 2-B, 3-C" -ForegroundColor Gray
Write-Host "  • Q1 A, Q2 B, Q3 C" -ForegroundColor Gray
