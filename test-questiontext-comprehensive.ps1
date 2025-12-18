# Comprehensive test to verify QuestionText in evaluation results
# This test generates a NEW exam and immediately evaluates it

param(
    [string]$BaseUrl = "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net"
)

Write-Host "`n======================================" -ForegroundColor Cyan
Write-Host "  QuestionText Verification Test" -ForegroundColor Cyan
Write-Host "======================================`n" -ForegroundColor Cyan

# Generate unique student ID
$studentId = "test-$(Get-Date -Format 'yyyyMMddHHmmss')"
Write-Host "Student ID: $studentId`n" -ForegroundColor White

# Step 1: Generate NEW exam (force fresh generation, not cached)
Write-Host "[1/5] Generating NEW exam (AI-powered)..." -ForegroundColor Yellow
$generateBody = @{
    subject = "Mathematics"
    grade = "2nd PUC"
    fastMode = $true
    useCache = $false  # Force new generation
} | ConvertTo-Json

try {
    $exam = Invoke-RestMethod -Uri "$BaseUrl/api/exam/generate" `
        -Method POST `
        -Body $generateBody `
        -ContentType "application/json" `
        -TimeoutSec 120
    
    Write-Host "✓ Exam generated: $($exam.examId)" -ForegroundColor Green
    Write-Host "  Subject: $($exam.subject)" -ForegroundColor Gray
    Write-Host "  Total Marks: $($exam.totalMarks)" -ForegroundColor Gray
    Write-Host "  Parts: $($exam.parts.Count)" -ForegroundColor Gray
    
    # Verify first few questions have questionText
    $sampleQuestions = 0
    foreach ($part in $exam.parts) {
        foreach ($q in $part.questions | Select-Object -First 2) {
            $sampleQuestions++
            $textPreview = if ($q.questionText.Length -gt 60) { 
                $q.questionText.Substring(0, 60) + "..." 
            } else { 
                $q.questionText 
            }
            Write-Host "  Sample Q$sampleQuestions [$($q.questionId)]: $textPreview" -ForegroundColor Gray
        }
        if ($sampleQuestions -ge 3) { break }
    }
}
catch {
    Write-Host "✗ Failed to generate exam" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Submit MCQ answers
Write-Host "`n[2/5] Submitting MCQ answers..." -ForegroundColor Yellow
$mcqAnswers = @()
foreach ($part in $exam.parts) {
    if ($part.questionType -match "MCQ|Multiple Choice") {
        foreach ($q in $part.questions) {
            # Pick first option as answer
            $answer = if ($q.options -and $q.options.Count -gt 0) { $q.options[0] } else { "A" }
            $mcqAnswers += @{
                questionId = $q.questionId
                selectedAnswer = $answer
            }
        }
    }
}

if ($mcqAnswers.Count -gt 0) {
    $mcqBody = @{
        examId = $exam.examId
        studentId = $studentId
        answers = $mcqAnswers
    } | ConvertTo-Json -Depth 10

    try {
        $mcqResult = Invoke-RestMethod -Uri "$BaseUrl/api/exam/submit-mcq" `
            -Method POST `
            -Body $mcqBody `
            -ContentType "application/json"
        Write-Host "✓ Submitted $($mcqAnswers.Count) MCQ answers" -ForegroundColor Green
    }
    catch {
        Write-Host "✗ MCQ submission failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}
else {
    Write-Host "  No MCQ questions to submit" -ForegroundColor Gray
}

# Step 3: Submit subjective answers (simulated written answers)
Write-Host "`n[3/5] Submitting subjective answers..." -ForegroundColor Yellow
$subjectiveAnswers = @()
foreach ($part in $exam.parts) {
    if ($part.questionType -notmatch "MCQ|Multiple Choice") {
        foreach ($q in $part.questions | Select-Object -First 5) {  # Limit to 5 to speed up test
            $subjectiveAnswers += @{
                questionId = $q.questionId
                questionNumber = $q.questionNumber
                writtenAnswer = "This is a test answer for question $($q.questionNumber). Not a real solution."
            }
        }
    }
}

if ($subjectiveAnswers.Count -gt 0) {
    $subjectiveBody = @{
        examId = $exam.examId
        studentId = $studentId
        answers = $subjectiveAnswers
    } | ConvertTo-Json -Depth 10

    try {
        $subjectiveResult = Invoke-RestMethod -Uri "$BaseUrl/api/exam/evaluate-subjective" `
            -Method POST `
            -Body $subjectiveBody `
            -ContentType "application/json" `
            -TimeoutSec 180
        Write-Host "✓ Evaluated $($subjectiveAnswers.Count) subjective answers" -ForegroundColor Green
        Write-Host "  WrittenSubmissionId: $($subjectiveResult.writtenSubmissionId)" -ForegroundColor Gray
    }
    catch {
        Write-Host "✗ Subjective evaluation failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "  This is expected if evaluate-subjective endpoint requires image upload" -ForegroundColor Yellow
    }
}
else {
    Write-Host "  No subjective questions to submit" -ForegroundColor Gray
}

# Step 4: Wait a bit for async processing
Write-Host "`n[4/5] Waiting for processing..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

# Step 5: Fetch consolidated result
Write-Host "`n[5/5] Fetching evaluation result..." -ForegroundColor Yellow
try {
    $result = Invoke-RestMethod -Uri "$BaseUrl/api/exam/result/$($exam.examId)/$studentId" `
        -Method GET
    
    Write-Host "✓ Result fetched successfully" -ForegroundColor Green
    Write-Host "  Total Score: $($result.grandScore)/$($result.grandTotalMarks)" -ForegroundColor Gray
    Write-Host "  Percentage: $($result.percentage)%" -ForegroundColor Gray
    
    # Analyze questionText presence
    Write-Host "`n======================================" -ForegroundColor Cyan
    Write-Host "  QuestionText Analysis" -ForegroundColor Cyan
    Write-Host "======================================`n" -ForegroundColor Cyan
    
    if ($result.subjectiveResults -and $result.subjectiveResults.Count -gt 0) {
        $totalSubjective = $result.subjectiveResults.Count
        $withQuestionText = ($result.subjectiveResults | Where-Object { 
            $_.questionText -and $_.questionText -ne "" -and $_.questionText -ne "Error loading question" 
        }).Count
        $withoutQuestionText = $totalSubjective - $withQuestionText
        
        Write-Host "Total Subjective Questions: $totalSubjective" -ForegroundColor White
        Write-Host "With QuestionText: $withQuestionText " -NoNewline
        Write-Host "$(if ($withQuestionText -eq $totalSubjective) { '✓' } else { '✗' })" -ForegroundColor $(if ($withQuestionText -eq $totalSubjective) { "Green" } else { "Red" })
        Write-Host "Without QuestionText: $withoutQuestionText" -ForegroundColor $(if ($withoutQuestionText -gt 0) { "Red" } else { "Green" })
        
        # Show sample results
        Write-Host "`nSample Results (first 3):" -ForegroundColor Yellow
        foreach ($subResult in $result.subjectiveResults | Select-Object -First 3) {
            Write-Host "`nQ$($subResult.questionNumber) [$($subResult.questionId)]:" -ForegroundColor Cyan
            
            $qtStatus = if ($subResult.questionText -and $subResult.questionText -ne "" -and $subResult.questionText -ne "Error loading question") {
                "✓ Present"
            } else {
                "✗ MISSING"
            }
            Write-Host "  QuestionText: $qtStatus" -ForegroundColor $(if ($qtStatus -match "✓") { "Green" } else { "Red" })
            
            if ($subResult.questionText -and $subResult.questionText.Length -gt 0) {
                $preview = if ($subResult.questionText.Length -gt 80) {
                    $subResult.questionText.Substring(0, 80) + "..."
                } else {
                    $subResult.questionText
                }
                Write-Host "    '$preview'" -ForegroundColor Gray
            }
            
            Write-Host "  ExpectedAnswer: $(if ($subResult.expectedAnswer) { '✓ Present' } else { '✗ Missing' })" -ForegroundColor $(if ($subResult.expectedAnswer) { "Green" } else { "Red" })
            Write-Host "  StudentAnswer: $($subResult.studentAnswerEcho.Substring(0, [Math]::Min(60, $subResult.studentAnswerEcho.Length)))..." -ForegroundColor Gray
            Write-Host "  Marks: $($subResult.earnedMarks)/$($subResult.maxMarks)" -ForegroundColor Gray
            Write-Host "  Feedback: $($subResult.overallFeedback.Substring(0, [Math]::Min(80, $subResult.overallFeedback.Length)))..." -ForegroundColor Gray
        }
        
        # Final verdict
        Write-Host "`n======================================" -ForegroundColor Cyan
        if ($withQuestionText -eq $totalSubjective) {
            Write-Host "  ✓ SUCCESS: All questions have QuestionText!" -ForegroundColor Green
            Write-Host "  The backend is working correctly." -ForegroundColor Green
        }
        else {
            Write-Host "  ✗ ISSUE: $withoutQuestionText questions missing QuestionText" -ForegroundColor Red
            Write-Host "  This may indicate old cached exams or DB issues." -ForegroundColor Yellow
        }
        Write-Host "======================================`n" -ForegroundColor Cyan
    }
    else {
        Write-Host "No subjective results found in response" -ForegroundColor Yellow
    }
    
    # Save full response for debugging
    $outputFile = "evaluation-result-$(Get-Date -Format 'yyyyMMddHHmmss').json"
    $result | ConvertTo-Json -Depth 15 | Out-File $outputFile
    Write-Host "Full result saved to: $outputFile" -ForegroundColor Cyan
}
catch {
    Write-Host "✗ Failed to fetch result" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    
    # Try to get more error details
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "  Response: $responseBody" -ForegroundColor Gray
        $reader.Close()
    }
}

Write-Host "`nTest complete!" -ForegroundColor Cyan
Write-Host "ExamId: $($exam.examId)" -ForegroundColor White
Write-Host "StudentId: $studentId`n" -ForegroundColor White
