param(
    [string]$BaseUrl = "https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net"
)

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Testing QuestionText Fix - Deployed API" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Step 1: Generate Exam
Write-Host "Step 1: Generating exam..." -ForegroundColor Yellow
$examRequest = @{
    subject = "Math"
    topics = @("Limits", "Derivatives")
    numQuestions = 3
    mcqCount = 1
    includeSubjective = $true
    subjectiveMarks = @(2)
} | ConvertTo-Json -Depth 10

try {
    $exam = Invoke-RestMethod -Uri "$BaseUrl/api/exam/generate" -Method Post -Body $examRequest -ContentType "application/json"
    Write-Host "✓ Exam generated: $($exam.examId)" -ForegroundColor Green
    $examId = $exam.examId
    
    # Save for reference
    $examId | Out-File "test-exam-id.txt" -NoNewline
    
    # Show a sample question
    if ($exam.parts -and $exam.parts.Count -gt 0) {
        $firstQ = $exam.parts[0].questions[0]
        Write-Host "  Sample Question: $($firstQ.questionText.Substring(0, [Math]::Min(50, $firstQ.questionText.Length)))..." -ForegroundColor Gray
    }
}
catch {
    Write-Host "✗ Failed to generate exam: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Submit Answer Sheet
Write-Host "`nStep 2: Submitting answer sheet..." -ForegroundColor Yellow

# Create a simple multipart form request
$boundary = [System.Guid]::NewGuid().ToString()
$LF = "`r`n"

# Read or create sample PDF
$pdfPath = "C:\school-ai-chatbot\sample_answer_sheet.pdf"
if (-not (Test-Path $pdfPath)) {
    Write-Host "  Creating sample answer sheet PDF..." -ForegroundColor Gray
    # Create a minimal valid PDF
    $pdfContent = @"
%PDF-1.4
1 0 obj
<< /Type /Catalog /Pages 2 0 R >>
endobj
2 0 obj
<< /Type /Pages /Kids [3 0 R] /Count 1 >>
endobj
3 0 obj
<< /Type /Page /Parent 2 0 R /Resources 4 0 R /MediaBox [0 0 612 792] /Contents 5 0 R >>
endobj
4 0 obj
<< /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >> >>
endobj
5 0 obj
<< /Length 100 >>
stream
BT
/F1 12 Tf
50 700 Td
(Answer Sheet) Tj
0 -50 Td
(Q1: lim x->0 sin(x)/x = 1) Tj
ET
endstream
endobj
xref
0 6
0000000000 65535 f 
0000000009 00000 n 
0000000058 00000 n 
0000000115 00000 n 
0000000214 00000 n 
0000000293 00000 n 
trailer
<< /Size 6 /Root 1 0 R >>
startxref
445
%%EOF
"@
    [System.IO.File]::WriteAllText($pdfPath, $pdfContent)
}

$fileBytes = [System.IO.File]::ReadAllBytes($pdfPath)
$fileEnc = [System.Text.Encoding]::GetEncoding("iso-8859-1").GetString($fileBytes)

$bodyLines = @(
    "--$boundary",
    'Content-Disposition: form-data; name="examId"',
    "",
    $examId,
    "--$boundary",
    'Content-Disposition: form-data; name="studentId"',
    "",
    "student123",
    "--$boundary",
    'Content-Disposition: form-data; name="file"; filename="answer_sheet.pdf"',
    "Content-Type: application/pdf",
    "",
    $fileEnc,
    "--$boundary--"
)

$body = $bodyLines -join $LF

try {
    $submission = Invoke-RestMethod -Uri "$BaseUrl/api/written-submission/submit-with-extraction" `
        -Method Post `
        -ContentType "multipart/form-data; boundary=$boundary" `
        -Body $body
    
    Write-Host "✓ Submission created: $($submission.submissionId)" -ForegroundColor Green
    Write-Host "  Status: $($submission.status)" -ForegroundColor Gray
}
catch {
    Write-Host "✗ Failed to submit answer sheet: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 3: Wait for evaluation
Write-Host "`nStep 3: Waiting for evaluation..." -ForegroundColor Yellow
Write-Host "  (Waiting 15 seconds...)" -ForegroundColor Gray
Start-Sleep -Seconds 15

# Step 4: Get Results
Write-Host "`nStep 4: Fetching results..." -ForegroundColor Yellow

try {
    $result = Invoke-RestMethod -Uri "$BaseUrl/api/exam/result/$examId/student123" -Method Get
    
    Write-Host "`n--- Results Summary ---" -ForegroundColor Cyan
    Write-Host "Total Score: $($result.totalScore)/$($result.maxScore)" -ForegroundColor White
    Write-Host "Grade: $($result.grade) ($($result.percentage)%)" -ForegroundColor White
    Write-Host "Pass: $($result.passed)" -ForegroundColor White
    
    # Check MCQ Results
    if ($result.mcqResults -and $result.mcqResults.Count -gt 0) {
        Write-Host "`n--- MCQ Results ---" -ForegroundColor Green
        $result.mcqResults | ForEach-Object {
            $qText = if ($_.questionText) { 
                $_.questionText.Substring(0, [Math]::Min(60, $_.questionText.Length)) 
            } else { 
                "[EMPTY]" 
            }
            Write-Host "  Q$($_.questionId): $qText..." -ForegroundColor White
            Write-Host "    Correct: $($_.isCorrect) | Marks: $($_.earnedMarks)/$($_.maxMarks)" -ForegroundColor Gray
        }
    }
    
    # Check Subjective Results
    if ($result.subjectiveResults -and $result.subjectiveResults.Count -gt 0) {
        Write-Host "`n--- Subjective Results ---" -ForegroundColor Green
        $result.subjectiveResults | ForEach-Object {
            $qText = if ($_.questionText) { 
                $_.questionText.Substring(0, [Math]::Min(60, $_.questionText.Length)) 
            } else { 
                "[EMPTY]" 
            }
            Write-Host "  Q$($_.questionId): $qText..." -ForegroundColor White
            Write-Host "    Marks: $($_.earnedMarks)/$($_.maxMarks)" -ForegroundColor Gray
        }
    }
    
    # Final Check
    Write-Host "`n--- QuestionText Validation ---" -ForegroundColor Cyan
    $emptyMcq = $result.mcqResults | Where-Object { [string]::IsNullOrWhiteSpace($_.questionText) }
    $emptySubj = $result.subjectiveResults | Where-Object { [string]::IsNullOrWhiteSpace($_.questionText) }
    
    if ($emptyMcq -or $emptySubj) {
        Write-Host "❌ FAILED: Some questionText fields are still empty!" -ForegroundColor Red
        if ($emptyMcq) {
            Write-Host "   Empty MCQ Questions: $($emptyMcq.Count)" -ForegroundColor Red
        }
        if ($emptySubj) {
            Write-Host "   Empty Subjective Questions: $($emptySubj.Count)" -ForegroundColor Red
        }
        exit 1
    }
    else {
        Write-Host "✅ SUCCESS: All questionText fields are populated!" -ForegroundColor Green
        Write-Host "   Total MCQ questions checked: $($result.mcqResults.Count)" -ForegroundColor Gray
        Write-Host "   Total Subjective questions checked: $($result.subjectiveResults.Count)" -ForegroundColor Gray
    }
}
catch {
    Write-Host "✗ Failed to get results: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Response: $($_.ErrorDetails.Message)" -ForegroundColor Gray
    exit 1
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Test Complete!" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan
