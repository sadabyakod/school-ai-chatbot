# Apply EF Core Migrations to Azure SQL Database
Write-Host "=== Applying Database Migrations to Azure SQL ===" -ForegroundColor Cyan

$connectionString = "Server=tcp:schoolchatbotsqlindia.database.windows.net,1433;Initial Catalog=school-ai-chatbot;Persist Security Info=False;User ID=schooladmin;Password=India@12345;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

Write-Host "`n1. Checking EF Core tools..." -ForegroundColor Yellow
if (-not (dotnet tool list -g | Select-String "dotnet-ef")) {
    dotnet tool install --global dotnet-ef
}

Write-Host "`n2. Navigating to backend..." -ForegroundColor Yellow
Set-Location c:\school-ai-chatbot\SchoolAiChatbotBackend

Write-Host "`n3. Creating migration..." -ForegroundColor Yellow
if (-not (Test-Path "Migrations")) {
    dotnet ef migrations add InitialCreate
}

Write-Host "`n4. Applying migrations to Azure SQL..." -ForegroundColor Yellow
dotnet ef database update --connection $connectionString --verbose

Write-Host "`n=== Complete ===" -ForegroundColor Green
