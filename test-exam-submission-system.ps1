# Test Script for Exam Submission System
# Run this after starting the server with: dotnet run --urls="http://0.0.0.0:8080"

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Exam Submission System - Test" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:8080"

# Step 1: Generate an exam
Write-Host "Step 1: Generating exam..." -ForegroundColor Yellow
$examRequest = @{
    subject = "Mathematics"
    grade = "2nd PUC"
    chapter = "Matrices"
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
    Write-Host "  Total marks: $($exam.totalMarks)" -ForegroundColor Gray
    Write-Host "  Duration: $($exam.duration) minutes" -ForegroundColor Gray
    Write-Host ""

    # Step 2: Submit MCQ answers
    Write-Host "Step 2: Submitting MCQ answers..." -ForegroundColor Yellow
    
    # Get MCQ questions from Part A
    $mcqPart = $exam.parts | Where-Object { $_.questionType -like "*MCQ*" } | Select-Object -First 1
    
    if ($mcqPart) {
        $mcqAnswers = @()
        foreach ($question in $mcqPart.questions | Select-Object -First 5) {
            $mcqAnswers += @{
                questionId = $question.questionId
                selectedOption = $question.correctAnswer
            }
        }

        $mcqRequest = @{
            examId = $examId
            studentId = "TEST-STUDENT-001"
            answers = $mcqAnswers
        } | ConvertTo-Json -Depth 5

        $mcqResult = Invoke-RestMethod -Uri "$baseUrl/api/exam/submit-mcq" `
            -Method POST `
            -Body $mcqRequest `
            -ContentType "application/json"

        Write-Host "✓ MCQ submitted" -ForegroundColor Green
        Write-Host "  Score: $($mcqResult.score)/$($mcqResult.totalMarks)" -ForegroundColor Gray
        Write-Host "  Percentage: $($mcqResult.percentage)%" -ForegroundColor Gray
        Write-Host ""
    }

    # Step 3: Get result
    Write-Host "Step 3: Fetching consolidated result..." -ForegroundColor Yellow
    
    $result = Invoke-RestMethod -Uri "$baseUrl/api/exam/result/$examId/TEST-STUDENT-001" `
        -Method GET

    Write-Host "✓ Result retrieved" -ForegroundColor Green
    Write-Host ""
    Write-Host "=== CONSOLIDATED RESULT ===" -ForegroundColor Cyan
    Write-Host "  Exam: $($result.examTitle)" -ForegroundColor White
    Write-Host "  Student: $($result.studentId)" -ForegroundColor White
    Write-Host ""
    Write-Host "  MCQ Score: $($result.mcqScore)/$($result.mcqTotalMarks)" -ForegroundColor White
    Write-Host "  Subjective Score: $($result.subjectiveScore)/$($result.subjectiveTotalMarks)" -ForegroundColor White
    Write-Host ""
    Write-Host "  GRAND TOTAL: $($result.grandScore)/$($result.grandTotalMarks)" -ForegroundColor Yellow
    Write-Host "  Percentage: $($result.percentage)%" -ForegroundColor Yellow
    Write-Host "  Grade: $($result.grade)" -ForegroundColor Yellow
    Write-Host "  Status: $(if ($result.passed) { 'PASSED ✓' } else { 'FAILED ✗' })" -ForegroundColor $(if ($result.passed) { 'Green' } else { 'Red' })
    Write-Host ""

    Write-Host "================================" -ForegroundColor Cyan
    Write-Host "All tests completed successfully!" -ForegroundColor Green
    Write-Host "================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Note: To test written answer upload, use Swagger UI at:" -ForegroundColor Yellow
    Write-Host "  http://localhost:8080/swagger" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Or use a tool like Postman to upload image files." -ForegroundColor Yellow

} catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Make sure the server is running:" -ForegroundColor Yellow
    Write-Host "  cd SchoolAiChatbotBackend" -ForegroundColor Cyan
    Write-Host "  dotnet run --urls=`"http://0.0.0.0:8080`"" -ForegroundColor Cyan
}
