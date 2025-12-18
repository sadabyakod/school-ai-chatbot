# ============================================================================
# END-TO-END ANSWER SHEET UPLOAD TEST
# ============================================================================
# Tests complete flow:
# 1. Generate exam with questions
# 2. Upload student answer sheet (simulated)
# 3. Poll submission status
# 4. Fetch detailed feedback with expected answers and marks breakdown
# ============================================================================

param(
    [string]$BaseUrl = "http://localhost:8080",
    [string]$StudentId = "student_$(Get-Date -Format 'HHmmss')",
    [string]$AnswerImagePath = "" # Optional: Path to actual image file
)

$ErrorActionPreference = "Continue"

# Color output functions
function Write-Step {
    param($Message)
    Write-Host "`n$('='*80)" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Yellow
    Write-Host "$('='*80)" -ForegroundColor Cyan
}

function Write-Success {
    param($Message)
    Write-Host " $Message" -ForegroundColor Green
}

function Write-Error-Custom {
    param($Message)
    Write-Host " $Message" -ForegroundColor Red
}

function Write-Info {
    param($Message)
    Write-Host "  $Message" -ForegroundColor Cyan
}

function Write-Detail {
    param($Key, $Value)
    Write-Host "   $Key : " -NoNewline -ForegroundColor Gray
    Write-Host $Value -ForegroundColor White
}

# ============================================================================
# STEP 1: GENERATE EXAM WITH QUESTIONS
# ============================================================================

Write-Step "STEP 1: Generating Exam Paper"

$examRequest = @{
    subject = "Mathematics"
    grade = "2nd PUC"
    chapter = "Integration"
    difficulty = "medium"
    examType = "full"
    useCache = $false
    fastMode = $true
} | ConvertTo-Json

Write-Info "Sending exam generation request..."
Write-Detail "Subject" "Mathematics"
Write-Detail "Grade" "2nd PUC"
Write-Detail "Chapter" "Integration"

try {
    $examResponse = Invoke-RestMethod -Uri "$BaseUrl/api/exam/generate" `
        -Method POST `
        -Body $examRequest `
        -ContentType "application/json" `
        -TimeoutSec 120

    if ($examResponse.exam -and $examResponse.exam.examId) {
        $examId = $examResponse.exam.examId
        $questionCount = $examResponse.exam.questionCount
        $totalMarks = $examResponse.exam.totalMarks
        
        Write-Success "Exam generated successfully!"
        Write-Detail "Exam ID" $examId
        Write-Detail "Total Questions" $questionCount
        Write-Detail "Total Marks" $totalMarks
        
        # Display question breakdown
        if ($examResponse.exam.parts) {
            Write-Host "`n Question Breakdown:" -ForegroundColor Cyan
            foreach ($part in $examResponse.exam.parts) {
                Write-Host "    $($part.partName): $($part.totalQuestions) questions ($($part.marksPerQuestion) marks each)" -ForegroundColor Gray
            }
        }
        
        # Show sample questions
        if ($examResponse.exam.questions -and $examResponse.exam.questions.Count -gt 0) {
            Write-Host "`n Sample Questions:" -ForegroundColor Cyan
            $sampleQuestions = $examResponse.exam.questions | Select-Object -First 3
            foreach ($q in $sampleQuestions) {
                Write-Host "   Q$($q.questionNumber). [$($q.marks) marks] $($q.questionText.Substring(0, [Math]::Min(100, $q.questionText.Length)))..." -ForegroundColor Gray
                if ($q.expectedAnswer) {
                    Write-Host "      Expected: $($q.expectedAnswer.Substring(0, [Math]::Min(80, $q.expectedAnswer.Length)))..." -ForegroundColor DarkGray
                }
            }
        }
    }
    else {
        Write-Error-Custom "Failed to generate exam: Invalid response format"
        exit 1
    }
}
catch {
    Write-Error-Custom "Failed to generate exam: $_"
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 2

# ============================================================================
# STEP 2: UPLOAD STUDENT ANSWER SHEET
# ============================================================================

Write-Step "STEP 2: Uploading Student Answer Sheet"

Write-Info "Preparing answer sheet upload..."
Write-Detail "Student ID" $StudentId
Write-Detail "Exam ID" $examId

# Create sample answer file if no image provided
if ([string]::IsNullOrEmpty($AnswerImagePath) -or !(Test-Path $AnswerImagePath)) {
    Write-Info "No image provided. Creating text-based answer file..."
    
    $answerContent = @'
STUDENT ANSWER SHEET
====================
Exam ID: {0}
Student ID: {1}
Subject: Mathematics (2nd PUC)

PART A - MCQ AND FILL IN THE BLANKS
====================================

Q1. The integral of 2x is: x squared + C

Q2. Integral of cos x dx equals sin x + C

Q3. The value of definite integral from 0 to 1 of x dx equals one half

PART B - SHORT ANSWER QUESTIONS
================================

Q4. Evaluate integral of 3x squared plus 2x plus 1 dx
Answer:
Integral equals x cubed plus x squared plus x plus C

Q5. Find integral of sin 2x dx
Answer:
Using substitution u equals 2x
Result equals negative one half cos 2x plus C

PART C - LONG ANSWER QUESTIONS
================================

Q6. Evaluate definite integral from 0 to pi of sin squared x dx

Answer:
Using the identity sin squared x equals one minus cos 2x divided by 2

Definite integral from 0 to pi of sin squared x dx 
equals one half times integral from 0 to pi of 1 minus cos 2x dx
equals one half times x minus sin 2x divided by 2 evaluated from 0 to pi
equals one half times pi
equals pi divided by 2

Therefore the answer is pi divided by 2

Q7. Find the area bounded by the curve y equals x squared and the line y equals 4

Answer:
The curve intersects the line at x equals plus or minus 2

Area equals definite integral from negative 2 to 2 of 4 minus x squared dx
equals 4x minus x cubed divided by 3 evaluated from negative 2 to 2
equals 8 minus 8 divided by 3 minus negative 8 plus 8 divided by 3
equals 16 minus 16 divided by 3
equals 32 divided by 3 square units

Therefore the area is 32 divided by 3 square units.
'@ -f $examId, $StudentId
    
    $tempAnswerFile = "temp_answer_sheet_$(Get-Date -Format 'HHmmss').txt"
    $answerContent | Out-File -FilePath $tempAnswerFile -Encoding UTF8
    $AnswerImagePath = $tempAnswerFile
    Write-Success "Created sample answer file: $tempAnswerFile"
}

# Upload answer sheet
try {
    Write-Info "Uploading answer sheet..."
    
    # Prepare multipart form data
    $boundary = [System.Guid]::NewGuid().ToString()
    $fileBin = [System.IO.File]::ReadAllBytes($AnswerImagePath)
    $fileName = [System.IO.Path]::GetFileName($AnswerImagePath)
    $fileExtension = [System.IO.Path]::GetExtension($AnswerImagePath)
    
    # Determine content type
    $contentType = switch ($fileExtension.ToLower()) {
        ".jpg"  { "image/jpeg" }
        ".jpeg" { "image/jpeg" }
        ".png"  { "image/png" }
        ".pdf"  { "application/pdf" }
        default { "text/plain" }
    }
    
    $LF = "`r`n"
    $bodyLines = (
        "--$boundary",
        "Content-Disposition: form-data; name=`"examId`"",
        "",
        $examId,
        "--$boundary",
        "Content-Disposition: form-data; name=`"studentId`"",
        "",
        $StudentId,
        "--$boundary",
        "Content-Disposition: form-data; name=`"files`"; filename=`"$fileName`"",
        "Content-Type: $contentType",
        "",
        [System.Text.Encoding]::UTF8.GetString($fileBin),
        "--$boundary--"
    ) -join $LF
    
    $uploadResponse = Invoke-RestMethod -Uri "$BaseUrl/api/exam/upload-written" `
        -Method POST `
        -ContentType "multipart/form-data; boundary=$boundary" `
        -Body $bodyLines `
        -TimeoutSec 60

    if ($uploadResponse.writtenSubmissionId) {
        $submissionId = $uploadResponse.writtenSubmissionId
        Write-Success "Answer sheet uploaded successfully!"
        Write-Detail "Submission ID" $submissionId
        Write-Detail "Status" $uploadResponse.status
        Write-Detail "Message" $uploadResponse.message
    }
    else {
        Write-Error-Custom "Upload failed: Invalid response"
        exit 1
    }
}
catch {
    Write-Error-Custom "Failed to upload answer sheet: $_"
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($tempAnswerFile) { Remove-Item $tempAnswerFile -ErrorAction SilentlyContinue }
    exit 1
}

# Clean up temp file
if ($tempAnswerFile -and (Test-Path $tempAnswerFile)) {
    Remove-Item $tempAnswerFile -ErrorAction SilentlyContinue
}

Start-Sleep -Seconds 2

# ============================================================================
# STEP 3: POLL SUBMISSION STATUS
# ============================================================================

Write-Step "STEP 3: Monitoring Evaluation Status"

$maxPolls = 60
$pollCount = 0
$isComplete = $false
$pollInterval = 5

Write-Info "Polling for evaluation status (max ${maxPolls} attempts)..."

while ($pollCount -lt $maxPolls -and -not $isComplete) {
    $pollCount++
    
    try {
        $statusResponse = Invoke-RestMethod -Uri "$BaseUrl/api/exam/submission-status/$submissionId" `
            -Method GET `
            -ContentType "application/json"
        
        $status = $statusResponse.status
        $statusMessage = $statusResponse.statusMessage
        $isComplete = $statusResponse.isComplete
        $isError = $statusResponse.isError
        
        # Display progress
        $progressBar = "[$pollCount/$maxPolls] "
        $statusIcon = switch ($status) {
            "0" { "[UPLOADED]" }  # Uploaded
            "1" { "[OCR]" }  # OCR Processing
            "2" { "[EVAL]" }  # Evaluating
            "3" { "[READY]" }  # Results Ready
            default { "[PROC]" }
        }
        
        Write-Host "$progressBar $statusIcon $statusMessage" -ForegroundColor $(if ($isComplete) { "Green" } elseif ($isError) { "Red" } else { "Yellow" })
        
        if ($isComplete) {
            Write-Success "Evaluation complete!"
            Write-Detail "Total Score" "$($statusResponse.totalScore)/$($statusResponse.maxPossibleScore)"
            Write-Detail "Percentage" "$($statusResponse.percentage)%"
            Write-Detail "Grade" $statusResponse.grade
            break
        }
        
        if ($isError) {
            Write-Error-Custom "Evaluation failed: $($statusResponse.errorMessage)"
            exit 1
        }
        
        # Wait before next poll (use recommended poll interval from API)
        $waitTime = if ($statusResponse.pollIntervalSeconds -gt 0) { $statusResponse.pollIntervalSeconds } else { $pollInterval }
        Start-Sleep -Seconds $waitTime
    }
    catch {
        Write-Host "  Poll attempt $pollCount failed: $($_.Exception.Message)" -ForegroundColor DarkYellow
        Start-Sleep -Seconds $pollInterval
    }
}

if (-not $isComplete) {
    Write-Error-Custom "Evaluation timed out after $pollCount polls"
    Write-Info "You can check status later using: GET $BaseUrl/api/exam/submission-status/$submissionId"
    exit 1
}

Start-Sleep -Seconds 2

# ============================================================================
# STEP 4: FETCH DETAILED FEEDBACK WITH MARKS BREAKDOWN
# ============================================================================

Write-Step "STEP 4: Fetching Detailed Evaluation Results"

Write-Info "Retrieving complete feedback with expected answers..."

try {
    $evaluationResult = Invoke-RestMethod -Uri "$BaseUrl/api/exam/evaluation-result/$submissionId" `
        -Method GET `
        -ContentType "application/json"
    
    Write-Success "Evaluation results retrieved!"
    
    # Display summary
    Write-Host "`n EVALUATION SUMMARY" -ForegroundColor Cyan
    Write-Host $('='*80) -ForegroundColor Gray
    Write-Detail "Submission ID" $evaluationResult.writtenSubmissionId
    Write-Detail "Exam ID" $evaluationResult.examId
    Write-Detail "Student ID" $evaluationResult.studentId
    Write-Detail "Evaluated At" $evaluationResult.evaluatedAt
    Write-Host ""
    Write-Detail "Total Score" "$($evaluationResult.summary.totalScore)/$($evaluationResult.summary.maxPossibleScore)"
    Write-Detail "Percentage" "$($evaluationResult.summary.percentage)%"
    Write-Detail "Grade" $evaluationResult.summary.grade
    
    # Display detailed question-by-question feedback
    if ($evaluationResult.evaluationResult) {
        $result = $evaluationResult.evaluationResult
        
        # Check if we have question results
        if ($result.questionResults -or $result.questions) {
            $questionResults = if ($result.questionResults) { $result.questionResults } else { $result.questions }
            
            Write-Host "`n QUESTION-BY-QUESTION FEEDBACK" -ForegroundColor Cyan
            Write-Host $('='*80) -ForegroundColor Gray
            
            $questionIndex = 1
            foreach ($qResult in $questionResults) {
                Write-Host "`nQuestion $questionIndex" -ForegroundColor Yellow
                Write-Host $('-'*80) -ForegroundColor DarkGray
                
                # Question details
                if ($qResult.questionText) {
                    Write-Host " Question: " -NoNewline -ForegroundColor Cyan
                    Write-Host $qResult.questionText -ForegroundColor White
                }
                
                # Marks
                $marksAwarded = if ($qResult.marksAwarded -ne $null) { $qResult.marksAwarded } else { $qResult.score }
                $maxMarks = if ($qResult.maxMarks -ne $null) { $qResult.maxMarks } else { $qResult.marks }
                
                Write-Host " Marks Awarded: " -NoNewline -ForegroundColor Cyan
                Write-Host "$marksAwarded / $maxMarks" -ForegroundColor $(if ($marksAwarded -eq $maxMarks) { "Green" } elseif ($marksAwarded -gt 0) { "Yellow" } else { "Red" })
                
                # Student answer
                if ($qResult.studentAnswer) {
                    Write-Host "  Student Answer: " -NoNewline -ForegroundColor Cyan
                    $displayAnswer = if ($qResult.studentAnswer.Length -gt 200) { 
                        $qResult.studentAnswer.Substring(0, 200) + "..." 
                    } else { 
                        $qResult.studentAnswer 
                    }
                    Write-Host $displayAnswer -ForegroundColor White
                }
                
                # Expected answer
                if ($qResult.expectedAnswer -or $qResult.correctAnswer) {
                    $expected = if ($qResult.expectedAnswer) { $qResult.expectedAnswer } else { $qResult.correctAnswer }
                    Write-Host " Expected Answer: " -NoNewline -ForegroundColor Cyan
                    $displayExpected = if ($expected.Length -gt 200) { 
                        $expected.Substring(0, 200) + "..." 
                    } else { 
                        $expected 
                    }
                    Write-Host $displayExpected -ForegroundColor Green
                }
                
                # Feedback
                if ($qResult.feedback) {
                    Write-Host " Feedback: " -NoNewline -ForegroundColor Cyan
                    Write-Host $qResult.feedback -ForegroundColor Magenta
                }
                
                # Step-by-step evaluation (if available)
                if ($qResult.stepByStepEvaluation) {
                    Write-Host " Step-by-Step Evaluation:" -ForegroundColor Cyan
                    foreach ($step in $qResult.stepByStepEvaluation) {
                        Write-Host "    $step" -ForegroundColor Gray
                    }
                }
                
                # Rubric breakdown (if available)
                if ($qResult.rubricBreakdown) {
                    Write-Host " Rubric Breakdown:" -ForegroundColor Cyan
                    foreach ($criterion in $qResult.rubricBreakdown) {
                        $criterionScore = if ($criterion.pointsAwarded -ne $null) { $criterion.pointsAwarded } else { $criterion.score }
                        $criterionMax = if ($criterion.maxPoints -ne $null) { $criterion.maxPoints } else { $criterion.maxScore }
                        Write-Host "    $($criterion.criterion -or $criterion.name): $criterionScore/$criterionMax" -ForegroundColor Gray
                        if ($criterion.feedback -or $criterion.comment) {
                            Write-Host "      $($criterion.feedback -or $criterion.comment)" -ForegroundColor DarkGray
                        }
                    }
                }
                
                $questionIndex++
            }
        }
        else {
            Write-Info "No detailed question results available in response"
        }
        
        # Display overall feedback
        if ($result.overallFeedback) {
            Write-Host "`n OVERALL FEEDBACK" -ForegroundColor Cyan
            Write-Host $('='*80) -ForegroundColor Gray
            Write-Host $result.overallFeedback -ForegroundColor White
        }
        
        # Display strengths and weaknesses
        if ($result.strengths) {
            Write-Host "`n STRENGTHS" -ForegroundColor Green
            foreach ($strength in $result.strengths) {
                Write-Host "    $strength" -ForegroundColor Green
            }
        }
        
        if ($result.areasForImprovement -or $result.weaknesses) {
            $improvements = if ($result.areasForImprovement) { $result.areasForImprovement } else { $result.weaknesses }
            Write-Host "`n AREAS FOR IMPROVEMENT" -ForegroundColor Yellow
            foreach ($area in $improvements) {
                Write-Host "    $area" -ForegroundColor Yellow
            }
        }
    }
    
    # Save detailed report to file
    $reportFile = "evaluation_report_${submissionId}_$(Get-Date -Format 'yyyyMMdd_HHmmss').json"
    $evaluationResult | ConvertTo-Json -Depth 10 | Out-File -FilePath $reportFile -Encoding UTF8
    Write-Host "`n Full report saved to: $reportFile" -ForegroundColor Cyan
}
catch {
    Write-Error-Custom "Failed to fetch evaluation results: $_"
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# ============================================================================
# TEST COMPLETE
# ============================================================================

Write-Host "`n" + $('='*80) -ForegroundColor Green
Write-Host "   END-TO-END TEST COMPLETED SUCCESSFULLY!" -ForegroundColor Green
Write-Host $('='*80) -ForegroundColor Green

Write-Host "`n Test Summary:" -ForegroundColor Cyan
Write-Detail "1. Exam Generated" " $examId"
Write-Detail "2. Answer Uploaded" " $submissionId"
Write-Detail "3. Evaluation Status" " Completed after $pollCount polls"
Write-Detail "4. Results Retrieved" "Done - Score: $($evaluationResult.summary.totalScore)/$($evaluationResult.summary.maxPossibleScore) - $($evaluationResult.summary.percentage)%"

Write-Host "`n API Endpoints Used:" -ForegroundColor Cyan
Write-Host "   1. POST $BaseUrl/api/exam/generate" -ForegroundColor Gray
Write-Host "   2. POST $BaseUrl/api/exam/upload-written" -ForegroundColor Gray
Write-Host "   3. GET  $BaseUrl/api/exam/submission-status/$submissionId" -ForegroundColor Gray
Write-Host "   4. GET  $BaseUrl/api/exam/evaluation-result/$submissionId" -ForegroundColor Gray

Write-Host "`nAll steps executed successfully!`n" -ForegroundColor Green

