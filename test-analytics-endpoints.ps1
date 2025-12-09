# Test script for Exam Analytics API endpoints
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
    $exam = Invoke-RestMethod -Uri "$baseUrl/api/exam/generate" -Method POST -Body $examRequest -ContentType "application/json" -TimeoutSec 120
    $examId = $exam.examId
    Write-Host "SUCCESS: Exam generated - $examId" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "FAILED: Could not generate exam - $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Submit MCQ answers for multiple students
Write-Host "Step 2: Submitting MCQ answers for 3 students..." -ForegroundColor Yellow

$students = @("STUDENT-001", "STUDENT-002", "STUDENT-003")

foreach ($studentId in $students) {
    # Find MCQ questions in the exam
    $mcqAnswers = @()
    
    foreach ($part in $exam.parts) {
        if ($part.questionType -like "*MCQ*" -or $part.questionType -eq "Multiple Choice") {
            foreach ($question in $part.questions) {
                if ($question.questionId -and $question.correctOption) {
                    $mcqAnswers += @{
                        questionId = $question.questionId
                        selectedOption = $question.correctOption
                    }
                    
                    # Limit to 5 questions for testing
                    if ($mcqAnswers.Count -ge 5) { break }
                }
            }
        }
        if ($mcqAnswers.Count -ge 5) { break }
    }
    
    if ($mcqAnswers.Count -gt 0) {
        $mcqRequest = @{
            examId = $examId
            studentId = $studentId
            answers = $mcqAnswers
        } | ConvertTo-Json -Depth 10

        try {
            $mcqResult = Invoke-RestMethod -Uri "$baseUrl/api/exam/submit-mcq" -Method POST -Body $mcqRequest -ContentType "application/json"
            $percentage = [math]::Round($mcqResult.percentage, 2)
            Write-Host "  SUCCESS: Student $studentId - Score: $($mcqResult.score)/$($mcqResult.totalMarks) ($percentage%)" -ForegroundColor Green
        } catch {
            $errorDetail = ""
            if ($_.ErrorDetails.Message) {
                $errorDetail = $_.ErrorDetails.Message
            }
            Write-Host "  FAILED: Student $studentId - $($_.Exception.Message) - $errorDetail" -ForegroundColor Red
        }
    } else {
        Write-Host "  SKIPPED: Student $studentId - No MCQ questions found in exam" -ForegroundColor Yellow
    }
}

Write-Host ""

# Step 3: Test GET /api/exam/{examId}/submissions
Write-Host "Step 3: Getting all submissions for exam..." -ForegroundColor Yellow

try {
    $submissions = Invoke-RestMethod -Uri "$baseUrl/api/exam/$examId/submissions?page=1&pageSize=10" -Method GET
    
    Write-Host "SUCCESS: Retrieved $($submissions.totalCount) submissions (page $($submissions.page) of $($submissions.totalPages))" -ForegroundColor Green
    
    foreach ($sub in $submissions.items) {
        Write-Host "  - Student: $($sub.studentId), Type: $($sub.submissionType), Score: $($sub.totalScore)/$($sub.totalMaxScore), Status: $($sub.status)" -ForegroundColor Gray
    }
    Write-Host ""
} catch {
    Write-Host "FAILED: Could not get submissions - $($_.Exception.Message)" -ForegroundColor Red
}

# Step 4: Test GET /api/exam/{examId}/submissions/{studentId}
Write-Host "Step 4: Getting detailed submission for first student..." -ForegroundColor Yellow

try {
    $detail = Invoke-RestMethod -Uri "$baseUrl/api/exam/$examId/submissions/STUDENT-001" -Method GET
    
    Write-Host "SUCCESS: Retrieved submission detail for STUDENT-001" -ForegroundColor Green
    Write-Host "  Exam: $($detail.examTitle)" -ForegroundColor Gray
    Write-Host "  MCQ: $($detail.mcqScore)/$($detail.mcqTotalMarks)" -ForegroundColor Gray
    Write-Host "  Status: $($detail.status)" -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "FAILED: Could not get submission detail - $($_.Exception.Message)" -ForegroundColor Red
}

# Step 5: Test GET /api/exam/submissions/by-student/{studentId}
Write-Host "Step 5: Getting exam history for STUDENT-001..." -ForegroundColor Yellow

try {
    $history = Invoke-RestMethod -Uri "$baseUrl/api/exam/submissions/by-student/STUDENT-001?page=1&pageSize=10" -Method GET
    
    Write-Host "SUCCESS: Retrieved $($history.totalCount) exam attempts" -ForegroundColor Green
    
    foreach ($attempt in $history.items) {
        Write-Host "  - Exam: $($attempt.examTitle)" -ForegroundColor Gray
        $attemptPercentage = [math]::Round($attempt.percentage, 2)
        Write-Host "    Score: $($attempt.score)/$($attempt.totalMarks) ($attemptPercentage%)" -ForegroundColor Gray
    }
    Write-Host ""
} catch {
    Write-Host "FAILED: Could not get student history - $($_.Exception.Message)" -ForegroundColor Red
}

# Step 6: Test GET /api/exam/{examId}/summary
Write-Host "Step 6: Getting exam summary statistics..." -ForegroundColor Yellow

try {
    $summary = Invoke-RestMethod -Uri "$baseUrl/api/exam/$examId/summary" -Method GET
    
    Write-Host "SUCCESS: Retrieved exam summary" -ForegroundColor Green
    Write-Host "  Exam: $($summary.examTitle)" -ForegroundColor Gray
    Write-Host "  Total Submissions: $($summary.totalSubmissions)" -ForegroundColor Gray
    Write-Host "  Completed: $($summary.completedSubmissions)" -ForegroundColor Gray
    Write-Host "  Pending Evaluation: $($summary.pendingEvaluations)" -ForegroundColor Gray
    
    if ($summary.averageScore) {
        $avgScore = [math]::Round($summary.averageScore, 2)
        Write-Host "  Average Score: $avgScore" -ForegroundColor Gray
    }
    
    Write-Host ""
} catch {
    Write-Host "FAILED: Could not get exam summary - $($_.Exception.Message)" -ForegroundColor Red
}

# Step 7: Test 404 handling
Write-Host "Step 7: Testing 404 handling..." -ForegroundColor Yellow

try {
    $notFound = Invoke-RestMethod -Uri "$baseUrl/api/exam/NONEXISTENT-EXAM/submissions" -Method GET
    Write-Host "FAILED: Should have returned 404" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "SUCCESS: Correctly returned 404 for non-existent exam" -ForegroundColor Green
    } else {
        Write-Host "FAILED: Unexpected error - $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "All tests completed!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
