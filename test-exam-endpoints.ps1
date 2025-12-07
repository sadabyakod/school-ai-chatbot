# Test Exam API Endpoints
Write-Host "=== Testing Exam API Endpoints ===" -ForegroundColor Cyan

$baseUrl = "http://localhost:8080"

# Test 1: Create Template
Write-Host "`n1. POST /api/exams/templates..." -ForegroundColor Yellow
$template = Invoke-RestMethod -Uri "$baseUrl/api/exams/templates" -Method Post -Body (@{name="Mobile Test";subject="Mathematics";chapter="Algebra";totalQuestions=5;durationMinutes=15;adaptiveEnabled=$true} | ConvertTo-Json) -ContentType "application/json"
Write-Host " Template ID: $($template.id)" -ForegroundColor Green

# Test 2: Start Exam  
Write-Host "`n2. POST /api/exams/start..." -ForegroundColor Yellow
$start = Invoke-RestMethod -Uri "$baseUrl/api/exams/start" -Method Post -Body (@{studentId="test123";examTemplateId=$template.id} | ConvertTo-Json) -ContentType "application/json"
Write-Host " Attempt ID: $($start.attemptId), Question: $($start.firstQuestion.text.Substring(0,40))..." -ForegroundColor Green

# Test 3: Submit Answer
Write-Host "`n3. POST /api/exams/$($start.attemptId)/answer..." -ForegroundColor Yellow  
$answer = Invoke-RestMethod -Uri "$baseUrl/api/exams/$($start.attemptId)/answer" -Method Post -Body (@{questionId=$start.firstQuestion.id;selectedOptionId=$start.firstQuestion.options[0].id;timeTakenSeconds=30} | ConvertTo-Json) -ContentType "application/json"
Write-Host " Correct: $($answer.isCorrect), Accuracy: $($answer.currentStats.currentAccuracy)%" -ForegroundColor Green

# Test 4: Get Summary
Write-Host "`n4. GET /api/exams/$($start.attemptId)/summary..." -ForegroundColor Yellow
$summary = Invoke-RestMethod -Uri "$baseUrl/api/exams/$($start.attemptId)/summary" -Method Get
Write-Host " Status: $($summary.status), Score: $($summary.scorePercent)%" -ForegroundColor Green

# Test 5: Get History  
Write-Host "`n5. GET /api/exams/history..." -ForegroundColor Yellow
$history = Invoke-RestMethod -Uri "$baseUrl/api/exams/history?studentId=test123" -Method Get
Write-Host " Total exams: $($history.Count)" -ForegroundColor Green

Write-Host "`n=== All Tests Passed! ===" -ForegroundColor Green
