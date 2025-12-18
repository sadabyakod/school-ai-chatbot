# Test script to verify questionText is included in evaluation results
param(
    [string]$BaseUrl = "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net"
)

Write-Host "`n=== Testing QuestionText in Evaluation Results ===" -ForegroundColor Cyan

# Step 1: Generate a simple test exam
Write-Host "`n1. Generating test exam..." -ForegroundColor Yellow
$generateRequest = @{
    subject = "Mathematics"
    grade = "2nd PUC"
    useCache = $false
} | ConvertTo-Json

try {
    $generateResponse = Invoke-RestMethod -Uri "$BaseUrl/api/exam/generate" -Method POST -Body $generateRequest -ContentType "application/json"
    $examId = $generateResponse.examId
    Write-Host "   Generated Exam ID: $examId" -ForegroundColor Green
    
    # Show first question to verify it has questionText
    if ($generateResponse.parts -and $generateResponse.parts.Count -gt 0) {
        $firstPart = $generateResponse.parts[0]
        if ($firstPart.questions -and $firstPart.questions.Count -gt 0) {
            $firstQuestion = $firstPart.questions[0]
            Write-Host "`n   First Question Details:" -ForegroundColor Cyan
            Write-Host "   - QuestionId: $($firstQuestion.questionId)" -ForegroundColor White
            Write-Host "   - QuestionNumber: $($firstQuestion.questionNumber)" -ForegroundColor White
            Write-Host "   - QuestionText: $($firstQuestion.questionText.Substring(0, [Math]::Min(100, $firstQuestion.questionText.Length)))..." -ForegroundColor White
            Write-Host "   - CorrectAnswer: $($firstQuestion.correctAnswer)" -ForegroundColor White
        }
    }
}
catch {
    Write-Host "   Error generating exam: $_" -ForegroundColor Red
    Write-Host "   Response: $($_.Exception.Response)" -ForegroundColor Red
    exit 1
}

# Step 2: Submit MCQ answers
Write-Host "`n2. Submitting MCQ answers..." -ForegroundColor Yellow
$mcqAnswers = @()
if ($generateResponse.parts) {
    foreach ($part in $generateResponse.parts) {
        if ($part.questionType -match "MCQ") {
            foreach ($question in $part.questions) {
                $mcqAnswers += @{
                    questionId = $question.questionId
                    selectedAnswer = "Not answered"  # Intentionally wrong to test evaluation
                }
            }
        }
    }
}

if ($mcqAnswers.Count -gt 0) {
    $mcqRequest = @{
        examId = $examId
        studentId = "test-student-$(Get-Random -Minimum 1000 -Maximum 9999)"
        answers = $mcqAnswers
    } | ConvertTo-Json -Depth 10

    try {
        $mcqResponse = Invoke-RestMethod -Uri "$BaseUrl/api/exam/submit-mcq" -Method POST -Body $mcqRequest -ContentType "application/json"
        Write-Host "   MCQ submitted successfully" -ForegroundColor Green
        $studentId = $mcqResponse.studentId
    }
    catch {
        Write-Host "   Error submitting MCQ: $_" -ForegroundColor Red
        $studentId = "test-student-$(Get-Random -Minimum 1000 -Maximum 9999)"
    }
}
else {
    Write-Host "   No MCQ questions found, skipping..." -ForegroundColor Yellow
    $studentId = "test-student-$(Get-Random -Minimum 1000 -Maximum 9999)"
}

# Step 3: Submit written answers (direct evaluation without image upload)
Write-Host "`n3. Submitting subjective answers for evaluation..." -ForegroundColor Yellow
$subjectiveAnswers = @()
if ($generateResponse.parts) {
    foreach ($part in $generateResponse.parts) {
        if ($part.questionType -notmatch "MCQ") {
            foreach ($question in $part.questions) {
                $subjectiveAnswers += @{
                    questionId = $question.questionId
                    questionNumber = $question.questionNumber
                    writtenAnswer = "Not answered"  # Intentionally empty to test feedback
                }
            }
        }
    }
}

if ($subjectiveAnswers.Count -gt 0) {
    $writtenRequest = @{
        examId = $examId
        studentId = $studentId
        answers = $subjectiveAnswers
    } | ConvertTo-Json -Depth 10

    try {
        $writtenResponse = Invoke-RestMethod -Uri "$BaseUrl/api/exam/evaluate-subjective" -Method POST -Body $writtenRequest -ContentType "application/json"
        Write-Host "   Subjective answers evaluated successfully" -ForegroundColor Green
        Write-Host "   WrittenSubmissionId: $($writtenResponse.writtenSubmissionId)" -ForegroundColor White
    }
    catch {
        Write-Host "   Error evaluating subjective answers: $_" -ForegroundColor Red
        Write-Host "   Response: $($_.Exception.Response)" -ForegroundColor Red
    }
}

# Step 4: Get consolidated result and check for questionText
Write-Host "`n4. Fetching consolidated result..." -ForegroundColor Yellow
Start-Sleep -Seconds 2  # Wait for processing

try {
    $resultResponse = Invoke-RestMethod -Uri "$BaseUrl/api/exam/result/$examId/$studentId" -Method GET
    Write-Host "   Result fetched successfully" -ForegroundColor Green
    
    # Check subjective results for questionText
    Write-Host "`n=== Checking QuestionText in Subjective Results ===" -ForegroundColor Cyan
    if ($resultResponse.subjectiveResults -and $resultResponse.subjectiveResults.Count -gt 0) {
        Write-Host "`n   Total Subjective Questions: $($resultResponse.subjectiveResults.Count)" -ForegroundColor White
        
        foreach ($result in $resultResponse.subjectiveResults | Select-Object -First 3) {
            Write-Host "`n   Question $($result.questionNumber) ($($result.questionId)):" -ForegroundColor Yellow
            Write-Host "   - QuestionText: $($result.questionText)" -ForegroundColor $(if ($result.questionText -and $result.questionText -ne "" -and $result.questionText -ne "Error loading question") { "Green" } else { "Red" })
            Write-Host "   - ExpectedAnswer: $($result.expectedAnswer.Substring(0, [Math]::Min(100, $result.expectedAnswer.Length)))..." -ForegroundColor White
            Write-Host "   - StudentAnswer: $($result.studentAnswerEcho)" -ForegroundColor White
            Write-Host "   - Marks: $($result.earnedMarks)/$($result.maxMarks)" -ForegroundColor White
            Write-Host "   - Feedback: $($result.overallFeedback)" -ForegroundColor White
        }
        
        # Check if any questionText is missing
        $missingQuestionText = $resultResponse.subjectiveResults | Where-Object { 
            -not $_.questionText -or $_.questionText -eq "" -or $_.questionText -eq "Error loading question" 
        }
        
        if ($missingQuestionText.Count -gt 0) {
            Write-Host "`n   ⚠️  WARNING: $($missingQuestionText.Count) questions have missing QuestionText!" -ForegroundColor Red
        }
        else {
            Write-Host "`n   ✅ All questions have QuestionText populated!" -ForegroundColor Green
        }
    }
    else {
        Write-Host "   No subjective results found" -ForegroundColor Yellow
    }
    
    # Also export full result to JSON for inspection
    $resultResponse | ConvertTo-Json -Depth 10 | Out-File "test-result-with-questiontext.json"
    Write-Host "`n   Full result saved to: test-result-with-questiontext.json" -ForegroundColor Cyan
}
catch {
    Write-Host "   Error fetching result: $_" -ForegroundColor Red
    Write-Host "   Response: $($_.Exception.Response)" -ForegroundColor Red
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
Write-Host "ExamId: $examId" -ForegroundColor White
Write-Host "StudentId: $studentId" -ForegroundColor White
