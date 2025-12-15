$sid = "TEST-" + (Get-Random -Max 9999)
"Q1: det(A) = -2`nQ2: f'(x) = 2x" | Out-File answer.txt

Write-Host "`n=== Answer Sheet Evaluation Test ===" -ForegroundColor Cyan
Write-Host "Student ID: $sid`n"

Write-Host "[Step 1] Uploading answer sheet..." -ForegroundColor Yellow
curl -s -X POST http://localhost:8080/api/exam/upload-written -F "examId=Karnataka_2nd_PUC_Math_2024_25" -F "studentId=$sid" -F "files=@answer.txt" -o upload.json
$upload = Get-Content upload.json | ConvertFrom-Json
Write-Host "  Submission ID: $($upload.writtenSubmissionId)" -ForegroundColor White
Write-Host "  Initial Status: $($upload.status)`n" -ForegroundColor Cyan

$subId = $upload.writtenSubmissionId

Write-Host "[Step 2] Monitoring evaluation status..." -ForegroundColor Yellow
$completed = $false
for($i = 1; $i -le 20; $i++) {
    Start-Sleep -Seconds 3
    curl -s "http://localhost:8080/api/exam/submission-status/$subId" -o status.json
    $status = Get-Content status.json | ConvertFrom-Json
    Write-Host "  [$i] Status: $($status.status)" -ForegroundColor Cyan
    
    if($status.isComplete -eq $true) {
        Write-Host "  Evaluation completed!`n" -ForegroundColor Green
        $completed = $true
        break
    }
}

if($completed) {
    Write-Host "[Step 3] Fetching results with step-wise marks...`n" -ForegroundColor Yellow
    curl -s "http://localhost:8080/api/exam/result/Karnataka_2nd_PUC_Math_2024_25/$sid" -o result.json
    $result = Get-Content result.json | ConvertFrom-Json
    
    Write-Host "=== EVALUATION RESULTS ===" -ForegroundColor Green
    Write-Host "Exam: $($result.examTitle)" -ForegroundColor White
    Write-Host "Total Score: $($result.grandScore) / $($result.grandTotalMarks)" -ForegroundColor White
    Write-Host "Percentage: $($result.percentage)%" -ForegroundColor White
    Write-Host "Grade: $($result.grade)" -ForegroundColor White
    
    if($result.subjectiveResults) {
        Write-Host "`nSubjective Results:" -ForegroundColor Yellow
        Write-Host "Score: $($result.subjectiveScore)/$($result.subjectiveTotalMarks)`n" -ForegroundColor White
        
        $qNum = 1
        foreach($q in $result.subjectiveResults) {
            Write-Host "Question $qNum" -ForegroundColor Cyan
            Write-Host "  Marks Awarded: $($q.earnedMarks) / $($q.maxMarks)" -ForegroundColor White
            
            if($q.stepAnalysis) {
                Write-Host "  Step-wise Evaluation:" -ForegroundColor Cyan
                foreach($step in $q.stepAnalysis) {
                    $icon = if($step.isCorrect) { "[OK]" } else { "[X]" }
                    Write-Host "    $icon Step $($step.step): $($step.marksAwarded)/$($step.maxMarksForStep)" -ForegroundColor White
                    Write-Host "       $($step.feedback)" -ForegroundColor Gray
                }
            }
            
            Write-Host "  Overall Feedback: $($q.overallFeedback)`n" -ForegroundColor Magenta
            $qNum++
        }
    }
    
    Write-Host "`n=== TEST COMPLETED ===" -ForegroundColor Green
} else {
    Write-Host "`nEvaluation did not complete within timeout" -ForegroundColor Yellow
}

Remove-Item answer.txt, upload.json, status.json, result.json -ErrorAction SilentlyContinue
