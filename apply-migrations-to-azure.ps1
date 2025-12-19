# Apply EF Core Migrations to Azure SQL Database
# This script updates the Azure SQL database schema to match the latest code

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Applying Migrations to Azure SQL Database" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get connection string from Azure App Service
Write-Host "Retrieving Azure SQL connection string..." -ForegroundColor Yellow

$appName = "app-wlanqwy7vuwmu"
$resourceGroup = "DefaultResourceGroup-EUS"

# Get connection string from Azure App Service configuration
$connectionString = az webapp config appsettings list --name $appName --resource-group $resourceGroup --query "[?name=='ConnectionStrings__DefaultConnection'].value" --output tsv

if (-not $connectionString) {
    Write-Host "❌ Failed to retrieve connection string from Azure" -ForegroundColor Red
    Write-Host "Manually set the connection string or check Azure CLI login" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Connection string retrieved" -ForegroundColor Green
Write-Host ""

# Navigate to backend project
Set-Location -Path "$PSScriptRoot\SchoolAiChatbotBackend"

Write-Host "Applying migrations to Azure SQL..." -ForegroundColor Yellow

# Apply migrations using connection string
$env:ConnectionStrings__DefaultConnection = $connectionString

dotnet ef database update --verbose

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "✓ Migrations Applied Successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Your Azure SQL database is now up to date." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "❌ Migration Failed" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting steps:" -ForegroundColor Yellow
    Write-Host "1. Ensure you're logged into Azure CLI: az login" -ForegroundColor White
    Write-Host "2. Check firewall rules allow your IP in Azure SQL" -ForegroundColor White
    Write-Host "3. Verify connection string is correct in Azure App Service" -ForegroundColor White
    exit 1
}
