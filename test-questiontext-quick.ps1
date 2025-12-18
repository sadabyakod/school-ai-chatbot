# Quick test to verify questionText is populated in exam generation
$baseUrl = "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "QUICK QUESTIONTEXT TEST" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Step 1: Generate exam
Write-Host "[STEP 1] Generating exam..." -ForegroundColor Yellow
$examBody = @{
    subject = "Mathematics"
    grade = "2nd PUC"
} | ConvertTo-Json

try {
    $exam = Invoke-RestMethod -Uri "$baseUrl/api/exam/generate" -Method POST -Body $examBody -ContentType "application/json"
    Write-Host "✅ Exam generated: $($exam.examId)" -ForegroundColor Green
    
    # Check questionText in all questions
    $allQuestionsHaveText = $true
    $questionsChecked = 0
    
    foreach ($part in $exam.parts) {
        foreach ($question in $part.questions) {
            $questionsChecked++
            if ([string]::IsNullOrEmpty($question.questionText)) {
                Write-Host "❌ Question $($question.questionId) has NO questionText!" -ForegroundColor Red
                $allQuestionsHaveText = $false
            } else {
                $preview = $question.questionText.Substring(0, [Math]::Min(50, $question.questionText.Length))
                Write-Host "✅ Question $($question.questionId): '$preview...'" -ForegroundColor Green
            }
        }
    }
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "RESULT: Checked $questionsChecked questions" -ForegroundColor Cyan
    if ($allQuestionsHaveText) {
        Write-Host "✅ ALL QUESTIONS HAVE TEXT!" -ForegroundColor Green
    } else {
        Write-Host "❌ SOME QUESTIONS MISSING TEXT!" -ForegroundColor Red
    }
    Write-Host "========================================`n" -ForegroundColor Cyan
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
