# Test MCQ Response - Verify Correct Answers are Returned
$baseUrl = "http://192.168.1.77:8080"
$examId = "test-exam-" + (Get-Date -Format "yyyyMMddHHmmss")
$studentId = "test-student-001"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "MCQ Response Verification Test" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Step 1: Generate exam
Write-Host "Step 1: Generating exam..." -ForegroundColor Yellow
$examRequest = @{
    subject = "Mathematics"
    chapter = "Algebra"
    examType = "Chapter Test"
    grade = "2nd PUC"
    difficulty = "Easy"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/exam/generate" -Method Post -Body $examRequest -ContentType "application/json" -TimeoutSec 120
    $examId = $response.examId
    Write-Host "✓ Exam generated: $examId" -ForegroundColor Green
    
    # Get MCQ questions
    $mcqQuestions = @()
    foreach ($part in $response.parts) {
        if ($part.questionType -like "*MCQ*") {
            $mcqQuestions += $part.questions
        }
    }
    
    if ($mcqQuestions.Count -eq 0) {
        Write-Host "✗ No MCQ questions found" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Found $($mcqQuestions.Count) MCQ questions`n" -ForegroundColor Cyan
} catch {
    Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Submit MCQ answers
Write-Host "Step 2: Submitting MCQ answers..." -ForegroundColor Yellow

$answers = @()
for ($i = 0; $i -lt [Math]::Min(3, $mcqQuestions.Count); $i++) {
    $q = $mcqQuestions[$i]
    if ($i -lt 2) {
        $selectedOption = $q.correctAnswer
    } else {
        $options = @("A", "B", "C", "D") | Where-Object { $_ -ne $q.correctAnswer }
        $selectedOption = $options[0]
    }
    
    $answers += @{
        questionId = $q.questionId
        selectedOption = $selectedOption
    }
}

$submissionRequest = @{
    examId = $examId
    studentId = $studentId
    answers = $answers
} | ConvertTo-Json -Depth 10

try {
    $submitResponse = Invoke-RestMethod -Uri "$baseUrl/api/exam-submission/submit-mcq" -Method Post -Body $submissionRequest -ContentType "application/json"
    Write-Host "✓ Submitted: $($submitResponse.score)/$($submitResponse.totalMarks)" -ForegroundColor Green
    
    Write-Host "`n--- Submit MCQ Response ---" -ForegroundColor Magenta
    $correctAnswerCount = 0
    foreach ($result in $submitResponse.results) {
        if ($null -ne $result.correctAnswer -and $result.correctAnswer -ne "") {
            $correctAnswerCount++
        }
    }
    
    if ($correctAnswerCount -eq $submitResponse.results.Count) {
        Write-Host "✓ ALL $($submitResponse.results.Count) results have correctAnswer field" -ForegroundColor Green
    } else {
        Write-Host "✗ Only $correctAnswerCount/$($submitResponse.results.Count) have correctAnswer" -ForegroundColor Red
    }
    
} catch {
    Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 3: Get result
Write-Host "`nStep 3: Retrieving exam result..." -ForegroundColor Yellow

try {
    $resultResponse = Invoke-RestMethod -Uri "$baseUrl/api/exam-submission/result/$examId/$studentId" -Method Get
    Write-Host "✓ Retrieved: $($resultResponse.mcqScore)/$($resultResponse.mcqTotalMarks)" -ForegroundColor Green
    
    Write-Host "`n--- Get Result Response ---" -ForegroundColor Magenta
    $correctAnswerCount = 0
    foreach ($result in $resultResponse.mcqResults) {
        if ($null -ne $result.correctAnswer -and $result.correctAnswer -ne "") {
            $correctAnswerCount++
        }
    }
    
    if ($correctAnswerCount -eq $resultResponse.mcqResults.Count) {
        Write-Host "✓ ALL $($resultResponse.mcqResults.Count) results have correctAnswer field" -ForegroundColor Green
    } else {
        Write-Host "✗ Only $correctAnswerCount/$($resultResponse.mcqResults.Count) have correctAnswer" -ForegroundColor Red
    }
    
} catch {
    Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "CONCLUSION" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$submitHasAll = ($submitResponse.results | Where-Object { $null -ne $_.correctAnswer -and $_.correctAnswer -ne "" }).Count -eq $submitResponse.results.Count
$resultHasAll = ($resultResponse.mcqResults | Where-Object { $null -ne $_.correctAnswer -and $_.correctAnswer -ne "" }).Count -eq $resultResponse.mcqResults.Count

if ($submitHasAll -and $resultHasAll) {
    Write-Host "`n✓ Backend IS returning correct answers" -ForegroundColor Green
    Write-Host "→ If mobile app doesn't show them, check frontend code" -ForegroundColor Yellow
} else {
    Write-Host "`n✗ Backend NOT returning correct answers properly" -ForegroundColor Red
}

Write-Host "`n========================================`n" -ForegroundColor Cyan
