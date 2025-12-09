# Test script for Exam Analytics API endpoints
# Run this after starting the backend server

$baseUrl = "http://localhost:8080"
$ErrorActionPreference = "Continue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Exam Analytics API Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Generate an exam
Write-Host "Step 1: Generating exam..." -ForegroundColor Yellow
$examRequest = @{
    subject = "Mathematics"
    grade = "2nd PUC"
    chapter = "Calculus"
    difficulty = "Medium"
    examType = "Full Paper"
} | ConvertTo-Json

try {
    $exam = Invoke-RestMethod -Uri "$baseUrl/api/exam/generate" `
        -Method POST `
        -Body $examRequest `
        -ContentType "application/json" `
        -TimeoutSec 120

    $examId = $exam.examId
    Write-Host "✓ Exam generated: $examId" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "✗ Failed to generate exam: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Submit MCQ answers for multiple students
Write-Host "Step 2: Submitting MCQ answers for 3 students..." -ForegroundColor Yellow

$students = @("STUDENT-001", "STUDENT-002", "STUDENT-003")

foreach ($studentId in $students) {
    # Get MCQ questions from the exam
    $mcqPart = $exam.parts | Where-Object { $_.questionType -like "*MCQ*" } | Select-Object -First 1
    
    if ($mcqPart) {
        $mcqAnswers = @()
        $questionCount = [Math]::Min(5, $mcqPart.questions.Count)
        
        for ($i = 0; $i -lt $questionCount; $i++) {
            $question = $mcqPart.questions[$i]
            # Randomly select right or wrong answer
            $isCorrect = (Get-Random -Minimum 0 -Maximum 2) -eq 1
            $answer = if ($isCorrect) { $question.correctAnswer } else { "A" }
            
            $mcqAnswers += @{
                questionId = $question.questionId
                selectedOption = $answer
            }
        }

        $mcqRequest = @{
            examId = $examId
            studentId = $studentId
            answers = $mcqAnswers
        } | ConvertTo-Json -Depth 5

        try {
            $mcqResult = Invoke-RestMethod -Uri "$baseUrl/api/exam/submit-mcq" `
                -Method POST `
                -Body $mcqRequest `
                -ContentType "application/json"

            Write-Host "  ✓ Student ${studentId}: $($mcqResult.score)/$($mcqResult.totalMarks) ($('{0:N2}' -f $mcqResult.percentage)%)" -ForegroundColor Green
        } catch {
            Write-Host "  ✗ Failed for student ${studentId}" -ForegroundColor Red
        }
    }
}

Write-Host ""

# Step 3: Test /api/exam/{examId}/submissions
Write-Host "Step 3: Getting all submissions for exam..." -ForegroundColor Yellow

try {
    $submissions = Invoke-RestMethod -Uri "$baseUrl/api/exam/$examId/submissions?page=1&pageSize=10" -Method GET
    
    Write-Host "✓ Retrieved submissions: $($submissions.totalCount) total, showing page $($submissions.page)" -ForegroundColor Green
    Write-Host "  Total Pages: $($submissions.totalPages)" -ForegroundColor Gray
    
    foreach ($sub in $submissions.items) {
        Write-Host "  - Student: $($sub.studentId), Type: $($sub.submissionType), Score: $($sub.totalScore)/$($sub.totalMaxScore), Status: $($sub.status)" -ForegroundColor Gray
    }
    Write-Host ""
} catch {
    Write-Host "✗ Failed to get submissions: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 4: Test /api/exam/{examId}/submissions/{studentId}
Write-Host "Step 4: Getting detailed submission for first student..." -ForegroundColor Yellow

try {
    $detail = Invoke-RestMethod -Uri "$baseUrl/api/exam/$examId/submissions/STUDENT-001" -Method GET
    
    Write-Host "✓ Retrieved submission detail for STUDENT-001" -ForegroundColor Green
    Write-Host "  Exam: $($detail.examTitle)" -ForegroundColor Gray
    Write-Host "  MCQ: $($detail.mcqScore)/$($detail.mcqTotalMarks)" -ForegroundColor Gray
    Write-Host "  Overall: $($detail.grandScore)/$($detail.grandTotalMarks) ($($detail.percentage)%)" -ForegroundColor Gray
    Write-Host "  Grade: $($detail.letterGrade), Passed: $($detail.passed)" -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "✗ Failed to get submission detail: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 5: Test /api/exam/submissions/by-student/{studentId}
Write-Host "Step 5: Getting exam history for STUDENT-001..." -ForegroundColor Yellow

try {
    $history = Invoke-RestMethod -Uri "$baseUrl/api/exam/submissions/by-student/STUDENT-001?page=1&pageSize=10" -Method GET
    
    Write-Host "✓ Retrieved exam history: $($history.totalCount) attempts" -ForegroundColor Green
    
    foreach ($attempt in $history.items) {
        Write-Host "  - Exam: $($attempt.examTitle)" -ForegroundColor Gray
        Write-Host "    Score: $($attempt.score)/$($attempt.totalMarks) ($('{0:N2}' -f $attempt.percentage)%)" -ForegroundColor Gray
        Write-Host "    Attempted: $($attempt.attemptedAt)" -ForegroundColor Gray
    }
    Write-Host ""
} catch {
    Write-Host "✗ Failed to get student history: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 6: Test /api/exam/{examId}/summary
Write-Host "Step 6: Getting exam summary statistics..." -ForegroundColor Yellow

try {
    $summary = Invoke-RestMethod -Uri "$baseUrl/api/exam/$examId/summary" -Method GET
    
    Write-Host "✓ Retrieved exam summary" -ForegroundColor Green
    Write-Host "  Exam: $($summary.examTitle)" -ForegroundColor Gray
    Write-Host "  Total Submissions: $($summary.totalSubmissions)" -ForegroundColor Gray
    Write-Host "  Completed: $($summary.completedSubmissions)" -ForegroundColor Gray
    Write-Host "  Pending Evaluation: $($summary.pendingEvaluations)" -ForegroundColor Gray
    Write-Host "  Average Score: $($summary.averageScore)" -ForegroundColor Gray
    Write-Host "  Score Range: $($summary.minScore) - $($summary.maxScore)" -ForegroundColor Gray
    Write-Host "  Average Percentage: $('{0:N2}' -f $summary.averagePercentage)%" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "  Status Breakdown:" -ForegroundColor Gray
    $summary.statusBreakdown.PSObject.Properties | ForEach-Object {
        Write-Host "    $($_.Name): $($_.Value)" -ForegroundColor Gray
    }
    Write-Host ""
} catch {
    Write-Host "✗ Failed to get exam summary: $($_.Exception.Message)" -ForegroundColor Red
}

# Test non-existent exam
Write-Host "Step 7: Testing 404 handling..." -ForegroundColor Yellow

try {
    $notFound = Invoke-RestMethod -Uri "$baseUrl/api/exam/NONEXISTENT-EXAM/submissions" -Method GET
    Write-Host "✗ Should have returned 404" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "✓ Correctly returned 404 for non-existent exam" -ForegroundColor Green
    } else {
        Write-Host "✗ Unexpected error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "All tests completed!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
