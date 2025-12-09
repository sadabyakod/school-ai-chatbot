@echo off
echo.
echo ========================================
echo SUBJECTIVE EVALUATION DEMONSTRATION
echo ========================================
echo.

set BASE_URL=http://localhost:8080
set EXAM_ID=Karnataka_2nd_PUC_Math_2024_25
set STUDENT_ID=DEMO-STUDENT-%RANDOM%

echo Step 1: Creating student answer file...
echo Question 1: Derivative of x^2 is 2x using power rule > answers.txt
echo Question 2: Integration of sin(x) gives -cos(x) + C >> answers.txt  
echo Question 3: Limit of sin(x)/x as x approaches 0 equals 1 >> answers.txt
echo [OK] Answer file created
echo.

echo Step 2: Uploading answer sheet for AI evaluation...
curl -X POST "%BASE_URL%/api/exam/upload-written" ^
  -F "examId=%EXAM_ID%" ^
  -F "studentId=%STUDENT_ID%" ^
  -F "files=@answers.txt" ^
  -H "Accept: application/json"
echo.
echo.

echo [OK] Upload complete!
echo.
echo AI is now evaluating the answers...
echo This includes:
echo   - OCR text extraction
echo   - Answer analysis  
echo   - Step-by-step scoring
echo   - Feedback generation
echo.
echo Waiting 30 seconds for evaluation to complete...
timeout /t 30 /nobreak
echo.

echo Step 3: Fetching evaluation results...
echo.
curl -X GET "%BASE_URL%/api/exam/result/%EXAM_ID%/%STUDENT_ID%" ^
  -H "Accept: application/json" ^
  | python -m json.tool
echo.

echo.
echo ========================================
echo WHAT STUDENTS RECEIVE:
echo ========================================
echo [OK] Score for each question
echo [OK] Total score and percentage
echo [OK] Expected/correct answers
echo [OK] Their answers echoed back
echo [OK] Step-by-step analysis
echo [OK] Detailed feedback per step
echo [OK] Overall feedback
echo [OK] Improvement suggestions
echo [OK] Final grade
echo ========================================
echo.

del answers.txt
pause
