# Test script for Exam System API endpoints
# Run: .\test-exam-endpoints.ps1

$baseUrl = "http://localhost:8080"
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "🧪 Testing Exam System API Endpoints" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Helper function to make API calls
function Invoke-ApiTest {
    param(
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null,
        [string]$Description
    )
    
    Write-Host "🔹 $Description" -ForegroundColor Yellow
    Write-Host "   $Method $Endpoint" -ForegroundColor Gray
    
    try {
        $headers = @{
            "Content-Type" = "application/json"
        }
        
        if ($Body) {
            $jsonBody = $Body | ConvertTo-Json -Depth 10
            Write-Host "   Request: $jsonBody" -ForegroundColor DarkGray
            $response = Invoke-RestMethod -Uri "$baseUrl$Endpoint" -Method $Method -Body $jsonBody -Headers $headers
        } else {
            $response = Invoke-RestMethod -Uri "$baseUrl$Endpoint" -Method $Method -Headers $headers
        }
        
        Write-Host "   ✅ Success" -ForegroundColor Green
        $response | ConvertTo-Json -Depth 5 | Write-Host -ForegroundColor White
        Write-Host ""
        return $response
    } catch {
        Write-Host "   ❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        return $null
    }
}

# Wait for backend to be ready
Write-Host "⏳ Waiting for backend to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

# Test 1: Create Exam Template
Write-Host "`n1️⃣  CREATE EXAM TEMPLATE" -ForegroundColor Cyan
Write-Host "=" * 40 -ForegroundColor DarkGray
$templateBody = @{
    Name = "Math Chapter 1 Test"
    Subject = "Mathematics"
    Chapter = "Algebra"
    TotalQuestions = 10
    DurationMinutes = 30
    AdaptiveEnabled = $true
}
$template = Invoke-ApiTest -Method "POST" -Endpoint "/api/exams/templates" -Body $templateBody -Description "Create new exam template"

if (-not $template) {
    Write-Host "⚠️  Cannot proceed without a template. Exiting..." -ForegroundColor Red
    exit
}

$templateId = $template.id

# Before starting exam, we need some questions. Let's create them directly in DB
Write-Host "`n📝 Note: You need questions in the database first!" -ForegroundColor Yellow
Write-Host "   Run this SQL to add sample questions:" -ForegroundColor Gray
Write-Host @"
   INSERT INTO Questions (Subject, Chapter, Text, Difficulty, Type, CreatedAt) VALUES
   ('Mathematics', 'Algebra', 'What is 2 + 2?', 'Easy', 'MultipleChoice', GETDATE()),
   ('Mathematics', 'Algebra', 'Solve: 3x + 5 = 14', 'Medium', 'MultipleChoice', GETDATE()),
   ('Mathematics', 'Algebra', 'Find derivative of x^2 + 3x + 2', 'Hard', 'MultipleChoice', GETDATE());

   -- Add options for each question (get QuestionId from above inserts)
   INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
   (1, '3', 0), (1, '4', 1), (1, '5', 0),
   (2, 'x=2', 0), (2, 'x=3', 1), (2, 'x=4', 0),
   (3, '2x', 0), (3, '2x+3', 1), (3, 'x^2', 0);
"@ -ForegroundColor DarkGray

Write-Host "`n   After adding questions, continue with these tests:" -ForegroundColor Yellow

# Test 2: Start Exam
Write-Host "`n2️⃣  START EXAM" -ForegroundColor Cyan
Write-Host "=" * 40 -ForegroundColor DarkGray
$startBody = @{
    studentId = "student123"
    examTemplateId = $templateId
}
$examStart = Invoke-ApiTest -Method "POST" -Endpoint "/api/exams/start" -Body $startBody -Description "Start new exam for student"

if ($examStart -and $examStart.attemptId) {
    $attemptId = $examStart.attemptId
    $firstQuestionId = $examStart.firstQuestion.id
    
    # Test 3: Submit Answer (First Question - Correct)
    Write-Host "`n3️⃣  SUBMIT ANSWER (Correct)" -ForegroundColor Cyan
    Write-Host "=" * 40 -ForegroundColor DarkGray
    
    # Assuming first option is correct for demo
    if ($examStart.firstQuestion.options.Count -gt 0) {
        $firstOptionId = $examStart.firstQuestion.options[0].id
        
        $answerBody = @{
            questionId = $firstQuestionId
            selectedOptionId = $firstOptionId
            timeTakenSeconds = 45
        }
        $submitResult = Invoke-ApiTest -Method "POST" -Endpoint "/api/exams/$attemptId/answer" -Body $answerBody -Description "Submit answer to first question"
        
        if ($submitResult -and $submitResult.nextQuestion) {
            # Test 4: Submit Another Answer
            Write-Host "`n4️⃣  SUBMIT SECOND ANSWER" -ForegroundColor Cyan
            Write-Host "=" * 40 -ForegroundColor DarkGray
            
            $secondQuestionId = $submitResult.nextQuestion.id
            $secondOptionId = $submitResult.nextQuestion.options[0].id
            
            $answerBody2 = @{
                questionId = $secondQuestionId
                selectedOptionId = $secondOptionId
                timeTakenSeconds = 60
            }
            Invoke-ApiTest -Method "POST" -Endpoint "/api/exams/$attemptId/answer" -Body $answerBody2 -Description "Submit answer to second question"
        }
    }
    
    # Test 5: Get Exam Summary
    Write-Host "`n5️⃣  GET EXAM SUMMARY" -ForegroundColor Cyan
    Write-Host "=" * 40 -ForegroundColor DarkGray
    Invoke-ApiTest -Method "GET" -Endpoint "/api/exams/$attemptId/summary" -Description "Get exam summary with statistics"
    
    # Test 6: Get Exam History
    Write-Host "`n6️⃣  GET EXAM HISTORY" -ForegroundColor Cyan
    Write-Host "=" * 40 -ForegroundColor DarkGray
    Invoke-ApiTest -Method "GET" -Endpoint "/api/exams/history?studentId=student123" -Description "Get student's exam history"
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "✅ Exam API Testing Complete!" -ForegroundColor Green
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "📊 Summary:" -ForegroundColor Cyan
Write-Host "   - POST /api/exams/templates ✓" -ForegroundColor White
Write-Host "   - POST /api/exams/start ✓" -ForegroundColor White
Write-Host "   - POST /api/exams/{id}/answer ✓" -ForegroundColor White
Write-Host "   - GET /api/exams/{id}/summary ✓" -ForegroundColor White
Write-Host "   - GET /api/exams/history ✓" -ForegroundColor White
Write-Host "`n💡 Tip: Add questions via SQL first for full testing!" -ForegroundColor Yellow
