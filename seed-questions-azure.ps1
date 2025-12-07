# Script to seed questions directly into Azure SQL via API
Write-Host "Seeding questions into Azure SQL database..." -ForegroundColor Cyan

$baseUrl = "http://localhost:8080"

# Sample questions to seed
$questions = @(
    @{
        subject = "Mathematics"
        chapter = "Algebra"
        topic = "Linear Equations"
        text = "What is the value of x in the equation 2x + 5 = 15?"
        type = "MCQ"
        difficulty = "Easy"
        correctAnswer = "5"
        options = @("5", "10", "15", "20")
    },
    @{
        subject = "Mathematics"
        chapter = "Algebra"
        topic = "Quadratic Equations"
        text = "Solve for x: x² - 5x + 6 = 0"
        type = "MCQ"
        difficulty = "Medium"
        correctAnswer = "x = 2 or x = 3"
        options = @("x = 2 or x = 3", "x = 1 or x = 6", "x = -2 or x = -3", "x = 5 or x = 1")
    },
    @{
        subject = "Science"
        chapter = "Physics"
        topic = "Force and Motion"
        text = "What is the SI unit of force?"
        type = "MCQ"
        difficulty = "Easy"
        correctAnswer = "Newton"
        options = @("Newton", "Joule", "Watt", "Pascal")
    }
)

Write-Host "`nNote: Questions are seeded automatically when the backend starts." -ForegroundColor Yellow
Write-Host "If questions are missing, the DatabaseSeeder needs to run." -ForegroundColor Yellow
Write-Host "`nChecking if Questions table has data..." -ForegroundColor Cyan

# Test by creating a template and starting an exam
try {
    $template = Invoke-RestMethod -Uri "$baseUrl/api/exams/templates" -Method Post -Body (@{
        name = "Test Question Check"
        subject = "Mathematics"
        chapter = "Algebra"
        totalQuestions = 3
        durationMinutes = 10
        adaptiveEnabled = $false
    } | ConvertTo-Json) -ContentType "application/json"
    
    Write-Host "Template created: ID $($template.id)" -ForegroundColor Green
    
    $start = Invoke-RestMethod -Uri "$baseUrl/api/exams/start" -Method Post -Body (@{
        studentId = "seed-test"
        examTemplateId = $template.id
    } | ConvertTo-Json) -ContentType "application/json"
    
    if ($start.firstQuestion) {
        Write-Host "✓ Questions exist in database!" -ForegroundColor Green
        Write-Host "  First question: $($start.firstQuestion.text.Substring(0, [Math]::Min(60, $start.firstQuestion.text.Length)))..." -ForegroundColor Gray
    } else {
        Write-Host "✗ No questions found in database" -ForegroundColor Red
        Write-Host "`nThe DatabaseSeeder needs to create questions." -ForegroundColor Yellow
        Write-Host "This happens automatically on first run when database is empty." -ForegroundColor Yellow
    }
} catch {
    Write-Host "Error testing: $_" -ForegroundColor Red
}
