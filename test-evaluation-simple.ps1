# ============================================================================
# SIMPLE END-TO-END ANSWER SHEET EVALUATION TEST
# ============================================================================

param(
    [string]$BaseUrl = "http://localhost:8080",
    [int]$MaxPollSeconds = 120,
    [int]$PollIntervalSeconds = 3
)

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " ANSWER SHEET EVALUATION TEST" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "Base URL: $BaseUrl" -ForegroundColor Gray
Write-Host "Max Wait: $MaxPollSeconds seconds`n" -ForegroundColor Gray

# ============================================================================
# STEP 1: GENERATE EXAM
# ============================================================================
Write-Host "[1/5] Generating Exam..." -ForegroundColor Cyan

$examRequest = @{
    subject = "Mathematics"
    grade = "2nd PUC"
} | ConvertTo-Json

try {
    $examResponse = Invoke-RestMethod -Uri "$BaseUrl/api/exam/generate" `
        -Method POST `
        -Body $examRequest `
        -ContentType "application/json"
    
    $examId = $examResponse.examId
    Write-Host "  SUCCESS: Exam ID = $examId" -ForegroundColor Green
    Write-Host "  Subject: $($examResponse.subject)" -ForegroundColor White
    Write-Host "  Grade: $($examResponse.grade)" -ForegroundColor White
    Write-Host "  Total Marks: $($examResponse.totalMarks)" -ForegroundColor White
    Write-Host "  (This may take 30-60 seconds...)" -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    exit 1
}

# ============================================================================
# STEP 2: CREATE ANSWER SHEET
# ============================================================================
Write-Host "[2/5] Creating Answer Sheet..." -ForegroundColor Cyan

$studentId = "TEST-$(Get-Date -Format 'HHmmss')"
$testAnswers = @"
ANSWER SHEET - Student: $studentId

QUESTION 1:
Matrix A = |2  3|
           |4  5|

Solution:
det(A) = (2)(5) - (3)(4) = 10 - 12 = -2

QUESTION 2:
Calculate determinant of 3x3 matrix.
Using cofactor expansion along first row.
Result: det(B) = 0

QUESTION 3:
Properties of determinants:
1. det(AB) = det(A) * det(B)
2. det(A^T) = det(A)
3. If row is zero, determinant is zero
"@

# Create a simple PNG file (API only accepts image/PDF files)
$pngBytes = [byte[]](
    0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,  # PNG signature
    0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,  # IHDR chunk
    0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,  # Width=1, Height=1
    0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,  # RGBA
    0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,  # IDAT chunk
    0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,  # Compressed data
    0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00,  # CRC
    0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,  # IEND chunk
    0x42, 0x60, 0x82
)
$tempFile = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "answer-$(Get-Date -Format 'HHmmss').png")
[System.IO.File]::WriteAllBytes($tempFile, $pngBytes)
Write-Host "  Answer sheet created: $(Split-Path $tempFile -Leaf)" -ForegroundColor Green
Write-Host ""

# ============================================================================
# STEP 3: UPLOAD ANSWER SHEET
# ============================================================================
Write-Host "[3/5] Uploading Answer Sheet..." -ForegroundColor Cyan

try {
    # Use curl for multipart form-data upload (hide progress output)
    $uploadResult = & curl.exe -s -X POST "$BaseUrl/api/exam/upload-written" `
                   -F "examId=$examId" `
                   -F "studentId=$studentId" `
                   -F "files=@$tempFile" `
                   -H "Accept: application/json" | ConvertFrom-Json
    
    $submissionId = $uploadResult.writtenSubmissionId
    Write-Host "  SUCCESS: Submission ID = $submissionId" -ForegroundColor Green
    Write-Host "  Initial Status: $($uploadResult.status)" -ForegroundColor Yellow
    Write-Host ""
} catch {
    Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Remove-Item $tempFile -ErrorAction SilentlyContinue
    exit 1
}

Remove-Item $tempFile -ErrorAction SilentlyContinue

# ============================================================================
# STEP 4: POLL STATUS UNTIL COMPLETE
# ============================================================================
Write-Host "[4/5] Monitoring Evaluation Progress..." -ForegroundColor Cyan

$maxAttempts = [Math]::Floor($MaxPollSeconds / $PollIntervalSeconds)
$attempt = 0
$completed = $false

while ($attempt -lt $maxAttempts -and -not $completed) {
    Start-Sleep -Seconds $PollIntervalSeconds
    $attempt++
    
    try {
        $statusResponse = Invoke-RestMethod -Uri "$BaseUrl/api/exam/submission-status/$submissionId" `
            -Method GET
        
        $status = $statusResponse.status
        $statusMessage = $statusResponse.statusMessage
        
        Write-Host "  [$attempt/$maxAttempts] Status: $status - $statusMessage" -ForegroundColor $(
            switch ($status) {
                "0" { "Yellow" }
                "1" { "Cyan" }
                "2" { "Green" }
                "3" { "Red" }
                "4" { "Red" }
                default { "White" }
            }
        )
        
        if ($status -eq "2") {
            Write-Host "  EVALUATION COMPLETE!" -ForegroundColor Green
            $completed = $true
            
            if ($statusResponse.evaluationResultBlobPath) {
                Write-Host "  Blob Path: $($statusResponse.evaluationResultBlobPath)" -ForegroundColor Gray
            }
            
            if ($statusResponse.result) {
                Write-Host "  Score Preview: $($statusResponse.result.grandScore)/$($statusResponse.result.grandTotalMarks)" -ForegroundColor Green
            }
        }
        elseif ($status -eq "3" -or $status -eq "4") {
            Write-Host "  EVALUATION FAILED: $statusMessage" -ForegroundColor Red
            exit 1
        }
        
    } catch {
        Write-Host "  ERROR checking status: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

if (-not $completed) {
    Write-Host "  TIMEOUT: Evaluation did not complete within $MaxPollSeconds seconds" -ForegroundColor Red
    exit 1
}

Write-Host ""

# ============================================================================
# STEP 5: FETCH DETAILED RESULTS
# ============================================================================
Write-Host "[5/5] Fetching Detailed Results..." -ForegroundColor Cyan

try {
    $results = Invoke-RestMethod -Uri "$BaseUrl/api/exam/result/$examId/$studentId" `
        -Method GET
    
    Write-Host "  SUCCESS: Results retrieved" -ForegroundColor Green
    Write-Host ""
    
    # ========================================
    # DISPLAY RESULTS
    # ========================================
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " EVALUATION RESULTS" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "Exam: $($results.examTitle)" -ForegroundColor White
    Write-Host "Student: $($results.studentId)" -ForegroundColor White
    Write-Host ""
    
    # Overall Score
    Write-Host "OVERALL SCORE" -ForegroundColor Yellow
    Write-Host "  MCQ Score: $($results.mcqScore)/$($results.mcqTotalMarks)" -ForegroundColor White
    Write-Host "  Subjective Score: $($results.subjectiveScore)/$($results.subjectiveTotalMarks)" -ForegroundColor White
    Write-Host "  Total Score: $($results.grandScore)/$($results.grandTotalMarks)" -ForegroundColor $(
        if ($results.passed) { "Green" } else { "Red" }
    )
    Write-Host "  Percentage: $($results.percentage)%" -ForegroundColor White
    Write-Host "  Grade: $($results.grade)" -ForegroundColor White
    Write-Host "  Result: $(if ($results.passed) { 'PASSED' } else { 'FAILED' })" -ForegroundColor $(
        if ($results.passed) { "Green" } else { "Red" }
    )
    Write-Host ""
    
    # Subjective Questions Details
    if ($results.subjectiveResults -and $results.subjectiveResults.Count -gt 0) {
        Write-Host "SUBJECTIVE QUESTIONS BREAKDOWN" -ForegroundColor Yellow
        Write-Host ""
        
        $qNum = 1
        foreach ($result in $results.subjectiveResults) {
            $percentCorrect = [Math]::Round(($result.earnedMarks / $result.maxMarks) * 100, 1)
            
            Write-Host "Question $qNum" -ForegroundColor Cyan
            Write-Host "  Marks: $($result.earnedMarks)/$($result.maxMarks) ($percentCorrect%)" -ForegroundColor $(
                if ($percentCorrect -ge 80) { "Green" } elseif ($percentCorrect -ge 50) { "Yellow" } else { "Red" }
            )
            
            if ($result.isFullyCorrect) {
                Write-Host "  Status: Fully Correct" -ForegroundColor Green
            } else {
                Write-Host "  Status: Partially Correct" -ForegroundColor Yellow
            }
            
            # Expected Answer
            if ($result.expectedAnswer) {
                Write-Host ""
                Write-Host "  EXPECTED ANSWER:" -ForegroundColor Green
                $expectedLines = ($result.expectedAnswer -split "`n") | Select-Object -First 5
                foreach ($line in $expectedLines) {
                    if ($line.Trim()) {
                        Write-Host "    $($line.Trim())" -ForegroundColor White
                    }
                }
            }
            
            # Student Answer (first few lines)
            if ($result.studentAnswerEcho) {
                Write-Host ""
                Write-Host "  STUDENT'S ANSWER:" -ForegroundColor Yellow
                $studentLines = ($result.studentAnswerEcho -split "`n") | Select-Object -First 5
                foreach ($line in $studentLines) {
                    if ($line.Trim()) {
                        Write-Host "    $($line.Trim())" -ForegroundColor White
                    }
                }
            }
            
            # Step Analysis
            if ($result.stepAnalysis -and $result.stepAnalysis.Count -gt 0) {
                Write-Host ""
                Write-Host "  STEP-WISE EVALUATION:" -ForegroundColor Cyan
                foreach ($step in $result.stepAnalysis) {
                    $stepIcon = if ($step.isCorrect) { "[OK]" } else { "[--]" }
                    $stepColor = if ($step.isCorrect) { "Green" } else { "Yellow" }
                    Write-Host "    $stepIcon Step $($step.stepNumber): $($step.marksAwarded)/$($step.maxMarks) marks" -ForegroundColor $stepColor
                    if ($step.feedback) {
                        Write-Host "         $($step.feedback)" -ForegroundColor Gray
                    }
                }
            }
            
            # Overall Feedback
            if ($result.overallFeedback) {
                Write-Host ""
                Write-Host "  FEEDBACK:" -ForegroundColor Magenta
                Write-Host "    $($result.overallFeedback)" -ForegroundColor Gray
            }
            
            Write-Host ""
            Write-Host "  " + ("-" * 70) -ForegroundColor DarkGray
            Write-Host ""
            
            $qNum++
        }
    }
    
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " TEST COMPLETED SUCCESSFULLY" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    
} catch {
    Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
    exit 1
}
