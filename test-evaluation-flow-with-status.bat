@echo off
REM ============================================================================
REM END-TO-END ANSWER SHEET EVALUATION TEST WITH DATABASE STATUS TRACKING
REM ============================================================================

echo.
echo ╔════════════════════════════════════════════════════════════════════════╗
echo ║  ANSWER SHEET EVALUATION FLOW - DATABASE STATUS TRACKING TEST          ║
echo ║  Flow: Upload → OCR → AI Evaluation → Results                         ║
echo ║  Status: PendingEvaluation → OcrProcessing → Evaluating → Completed   ║
echo ╚════════════════════════════════════════════════════════════════════════╝
echo.

set BASE_URL=http://localhost:8080
set EXAM_ID=Karnataka_2nd_PUC_Math_2024_25
set STUDENT_ID=DEMO-STUDENT-%RANDOM%

echo [INFO] Base URL: %BASE_URL%
echo [INFO] Student ID: %STUDENT_ID%
echo.

REM ============================================================================
REM STEP 1: CREATE TEST ANSWER SHEET
REM ============================================================================
echo ╔════════════════════════════════════════════
echo ║ STEP 1: Create Student Answer Sheet
echo ╚════════════════════════════════════════════
echo.

echo Creating answer sheet with detailed solutions...
(
echo STUDENT ANSWER SHEET
echo ================================================================================
echo Student ID: %STUDENT_ID%
echo Exam: Karnataka 2nd PUC Mathematics
echo Date: %DATE% %TIME%
echo ================================================================================
echo.
echo QUESTION 1:
echo -----------
echo Given matrix A = [2  3]
echo                  [4  5]
echo.
echo Solution:
echo Step 1: Identify the matrix elements
echo         a=2, b=3, c=4, d=5
echo.
echo Step 2: Apply determinant formula for 2x2 matrix
echo         det^(A^) = ad - bc
echo.
echo Step 3: Substitute values
echo         det^(A^) = ^(2^)^(5^) - ^(3^)^(4^)
echo         det^(A^) = 10 - 12
echo.
echo Step 4: Calculate final answer
echo         det^(A^) = -2
echo.
echo Answer: The determinant is -2
echo.
echo.
echo QUESTION 2:
echo -----------
echo Find derivative of x^2 using first principles
echo.
echo Solution:
echo Step 1: Write the first principles formula
echo         f'^(x^) = lim[h→0] [f^(x+h^) - f^(x^)] / h
echo.
echo Step 2: Substitute f^(x^) = x^2
echo         f'^(x^) = lim[h→0] [^(x+h^)^2 - x^2] / h
echo.
echo Step 3: Expand and simplify
echo         f'^(x^) = lim[h→0] [x^2 + 2xh + h^2 - x^2] / h
echo         f'^(x^) = lim[h→0] [2xh + h^2] / h
echo         f'^(x^) = lim[h→0] [h^(2x + h^)] / h
echo         f'^(x^) = lim[h→0] ^(2x + h^)
echo.
echo Step 4: Apply the limit
echo         f'^(x^) = 2x + 0 = 2x
echo.
echo Answer: The derivative of x^2 is 2x
echo.
echo ================================================================================
echo END OF ANSWER SHEET
echo ================================================================================
) > answers.txt

echo [SUCCESS] Answer sheet created: answers.txt
echo.

REM ============================================================================
REM STEP 2: UPLOAD ANSWER SHEET
REM ============================================================================
echo ╔════════════════════════════════════════════
echo ║ STEP 2: Upload Answer Sheet for Evaluation
echo ╚════════════════════════════════════════════
echo.

echo [INFO] Uploading answer sheet...
curl -X POST "%BASE_URL%/api/exam/upload-written" ^
  -F "examId=%EXAM_ID%" ^
  -F "studentId=%STUDENT_ID%" ^
  -F "files=@answers.txt" ^
  -H "Accept: application/json" > upload_response.json

echo.
echo [SUCCESS] Answer sheet uploaded!
echo.

REM Extract submission ID (basic parsing)
for /f "tokens=2 delims=:" %%a in ('findstr /C:"writtenSubmissionId" upload_response.json') do (
    set SUBMISSION_ID=%%a
)
set SUBMISSION_ID=%SUBMISSION_ID:"=%
set SUBMISSION_ID=%SUBMISSION_ID:,=%
set SUBMISSION_ID=%SUBMISSION_ID: =%

echo [INFO] Submission ID: %SUBMISSION_ID%
echo [STATUS] PendingEvaluation - Answer sheet uploaded, awaiting processing
echo.

REM ============================================================================
REM STEP 3: MONITOR STATUS UPDATES
REM ============================================================================
echo ╔════════════════════════════════════════════
echo ║ STEP 3: Monitor Status Updates in Database
echo ╚════════════════════════════════════════════
echo.

echo [INFO] Polling submission status every 3 seconds...
echo [INFO] This demonstrates real-time database status updates
echo.

set MAX_ATTEMPTS=40
set ATTEMPT=0

:poll_loop
set /a ATTEMPT+=1
if %ATTEMPT% GTR %MAX_ATTEMPTS% goto poll_timeout

echo [Attempt %ATTEMPT%/%MAX_ATTEMPTS%] Checking status...

REM Check submission status
curl -s -X GET "%BASE_URL%/api/exam/submission-status/%SUBMISSION_ID%" > status_response.json

REM Check if completed
findstr /C:"Completed" status_response.json >nul
if %ERRORLEVEL% EQU 0 (
    echo.
    echo [SUCCESS] ✓ Evaluation completed successfully!
    echo [STATUS] Completed - All processing finished
    echo.
    goto fetch_results
)

REM Check if processing
findstr /C:"OcrProcessing" status_response.json >nul
if %ERRORLEVEL% EQU 0 (
    echo [STATUS] OcrProcessing - Extracting text from answer sheet...
    echo [INFO] DB Field: OcrStartedAt = %DATE% %TIME%
)

findstr /C:"Evaluating" status_response.json >nul
if %ERRORLEVEL% EQU 0 (
    echo [STATUS] Evaluating - AI is scoring your answers...
    echo [INFO] DB Field: EvaluationStartedAt = %DATE% %TIME%
)

REM Check if failed
findstr /C:"Failed" status_response.json >nul
if %ERRORLEVEL% EQU 0 (
    echo.
    echo [ERROR] ✗ Evaluation failed!
    goto cleanup
)

timeout /t 3 /nobreak >nul
goto poll_loop

:poll_timeout
echo.
echo [WARNING] Maximum polling time exceeded
echo [INFO] You can check results manually at:
echo [INFO] GET %BASE_URL%/api/exam/result/%EXAM_ID%/%STUDENT_ID%
echo.
goto cleanup

REM ============================================================================
REM STEP 4: FETCH COMPLETE RESULTS
REM ============================================================================
:fetch_results
echo ╔════════════════════════════════════════════
echo ║ STEP 4: Fetch Complete Evaluation Results
echo ╚════════════════════════════════════════════
echo.

echo [INFO] Fetching results with step-wise marks...
curl -X GET "%BASE_URL%/api/exam/result/%EXAM_ID%/%STUDENT_ID%" ^
  -H "Accept: application/json" > final_results.json

echo.
echo ╔════════════════════════════════════════════════════════════════════════╗
echo ║  EVALUATION RESULTS
echo ╚════════════════════════════════════════════════════════════════════════╝
echo.

REM Display results using Python JSON pretty print if available
where python >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    python -m json.tool final_results.json
) else (
    type final_results.json
)

echo.
echo.
echo ════════════════════════════════════════════════════════════════════════
echo WHAT YOU SEE IN THE RESULTS:
echo ════════════════════════════════════════════════════════════════════════
echo.
echo [✓] Score for each question
echo [✓] Total score and percentage
echo [✓] Expected/correct answers
echo [✓] Student's answers echoed back
echo [✓] Step-by-step analysis with marks per step
echo [✓] Detailed feedback for each step
echo [✓] Overall feedback and suggestions
echo [✓] Final grade (A+, A, B+, B, C, D, F)
echo.
echo ════════════════════════════════════════════════════════════════════════
echo DATABASE STATUS FLOW:
echo ════════════════════════════════════════════════════════════════════════
echo.
echo 1. PendingEvaluation  → Answer sheet uploaded (SubmittedAt timestamp)
echo 2. OcrProcessing      → Text extraction started (OcrStartedAt timestamp)
echo 3. Evaluating         → AI evaluation in progress (EvaluationStartedAt)
echo 4. Completed          → Evaluation finished (EvaluatedAt timestamp)
echo.
echo All timestamps are stored in WrittenSubmissions table in database
echo.

REM ============================================================================
REM CLEANUP
REM ============================================================================
:cleanup
echo ╔════════════════════════════════════════════
echo ║ CLEANUP
echo ╚════════════════════════════════════════════
echo.

if exist answers.txt del answers.txt
if exist upload_response.json del upload_response.json
if exist status_response.json del status_response.json
echo [SUCCESS] Temporary files cleaned up
echo.

echo ╔════════════════════════════════════════════════════════════════════════╗
echo ║  TEST COMPLETED!
echo ╚════════════════════════════════════════════════════════════════════════╝
echo.

pause
