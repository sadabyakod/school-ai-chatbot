# Test QuestionText Fix End-to-End
$baseUrl = "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "QUESTION TEXT E2E TEST" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Step 1: Generate Exam
Write-Host "[Step 1] Generating exam..." -ForegroundColor Yellow
$examRequest = @{
    subject = "Mathematics"
    grade = "2nd PUC"
} | ConvertTo-Json

try {
    $exam = Invoke-RestMethod -Uri "$baseUrl/api/exam/generate" -Method POST -Body $examRequest -ContentType "application/json"
    $examId = $exam.examId
    Write-Host "✓ Exam generated: $examId" -ForegroundColor Green
    Write-Host "  Total Questions: $($exam.questionCount)" -ForegroundColor Gray
    Write-Host "  Total Marks: $($exam.totalMarks)" -ForegroundColor Gray
} catch {
    Write-Host "✗ Failed to generate exam: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Create mock answer PDF (just a simple text file for testing)
Write-Host "`n[Step 2] Creating mock answer sheet..." -ForegroundColor Yellow
$answersText = @"
Question 3: Pythagorean Theorem
The Pythagorean theorem states that in a right triangle the square of the hypotenuse equals the sum of squares of the other two sides.
Given sides 3 and 4, we calculate c = 5

Question 4: Quadratic Equation
x squared minus 5x plus 6 equals 0
Factoring gives x equals 2 or x equals 3

Question 5: Circle Area
Area equals pi times radius squared
Area equals 78.5 square cm
"@

$tempFile = [System.IO.Path]::GetTempFileName()
$pdfFile = $tempFile -replace '\.tmp$', '.pdf'
[System.IO.File]::WriteAllText($pdfFile, $answersText)
Write-Host "[OK] Mock answer sheet created" -ForegroundColor Green

# Step 3: Submit answer sheet
Write-Host "`n[Step 3] Submitting answer sheet..." -ForegroundColor Yellow
$studentId = "TEST_STUDENT_" + (Get-Date -Format "yyyyMMddHHmmss")

try {
    # Read file as bytes
    $fileBytes = [System.IO.File]::ReadAllBytes($pdfFile)
    $fileContent = [System.Convert]::ToBase64String($fileBytes)
    
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"
    
    $bodyLines = @(
        "--$boundary",
        "Content-Disposition: form-data; name=`"examId`"$LF",
        $examId,
        "--$boundary",
        "Content-Disposition: form-data; name=`"studentId`"$LF",
        $studentId,
        "--$boundary",
        "Content-Disposition: form-data; name=`"answerSheetFile`"; filename=`"answers.pdf`"",
        "Content-Type: application/pdf$LF",
        $answersText,
        "--$boundary--$LF"
    )
    
    $body = $bodyLines -join $LF
    
    $response = Invoke-RestMethod -Uri "$baseUrl/api/exam/submit-written" `
        -Method POST `
        -ContentType "multipart/form-data; boundary=$boundary" `
        -Body $body
    
    $submissionId = $response.writtenSubmissionId
    Write-Host "[OK] Answer sheet submitted: $submissionId" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Failed to submit: $($_.Exception.Message)" -ForegroundColor Red
    Remove-Item $pdfFile -ErrorAction SilentlyContinue
    exit 1
}

# Cleanup temp file
Remove-Item $pdfFile -ErrorAction SilentlyContinue

# Step 4: Wait for evaluation
Write-Host "`n[Step 4] Waiting for evaluation..." -ForegroundColor Yellow
$maxAttempts = 20
$attempt = 0
$status = "pending"

while ($attempt -lt $maxAttempts -and $status -ne "completed") {
    $attempt++
    Start-Sleep -Seconds 3
    
    try {
        $statusResponse = Invoke-RestMethod -Uri "$baseUrl/api/exam/submission-status/$submissionId" -Method GET
        $status = $statusResponse.status
        Write-Host "  Attempt $attempt/$maxAttempts - Status: $status" -ForegroundColor Gray
    } catch {
        Write-Host "  Attempt $attempt/$maxAttempts - Checking..." -ForegroundColor Gray
    }
}

if ($status -ne "completed") {
    Write-Host "[FAIL] Evaluation timed out" -ForegroundColor Red
    exit 1
}

Write-Host "[OK] Evaluation completed" -ForegroundColor Green

# Step 5: Fetch results and check questionText
Write-Host "`n[Step 5] Fetching results to verify questionText..." -ForegroundColor Yellow

try {
    $result = Invoke-RestMethod -Uri "$baseUrl/api/exam/result/$examId/$studentId" -Method GET
    
    Write-Host "`n" -NoNewline
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "RESULTS VERIFICATION" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    
    $allQuestionsHaveText = $true
    
    # Check subjective questions
    Write-Host "`nSubjective Questions:" -ForegroundColor White
    foreach ($question in $result.subjectiveResults) {
        $hasText = -not [string]::IsNullOrWhiteSpace($question.questionText)
        $icon = if ($hasText) { "[OK]" } else { "[FAIL]"; $allQuestionsHaveText = $false }
        $color = if ($hasText) { "Green" } else { "Red" }
        
        Write-Host "  $icon Q$($question.questionNumber) - " -ForegroundColor $color -NoNewline
        if ($hasText) {
            $preview = $question.questionText.Substring(0, [Math]::Min(60, $question.questionText.Length))
            Write-Host "$preview..." -ForegroundColor Gray
        } else {
            Write-Host "[EMPTY - BUG!]" -ForegroundColor Red
        }
        
        Write-Host "      Marks: $($question.earnedMarks)/$($question.maxMarks)" -ForegroundColor Gray
    }
    
    # Summary
    Write-Host "`n" -NoNewline
    Write-Host "========================================" -ForegroundColor Cyan
    if ($allQuestionsHaveText) {
        Write-Host "[SUCCESS] All questions have text!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Cyan
        exit 0
    } else {
        Write-Host "[FAILED] Some questions missing text!" -ForegroundColor Red
        Write-Host "========================================" -ForegroundColor Cyan
        
        # Show debug info
        Write-Host "`nDebug Info:" -ForegroundColor Yellow
        Write-Host "Exam ID: $examId" -ForegroundColor Gray
        Write-Host "Student ID: $studentId" -ForegroundColor Gray
        Write-Host "Submission ID: $submissionId" -ForegroundColor Gray
        exit 1
    }
    
} catch {
    Write-Host "✗ Failed to fetch results: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
