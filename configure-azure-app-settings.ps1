#!/usr/bin/env pwsh
# Configure Azure App Service Settings
# This script sets the required environment variables for the production backend

Write-Host "====================================" -ForegroundColor Cyan
Write-Host " Azure App Service Configuration" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Install Azure CLI if not present
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "Azure CLI not found. Installing..." -ForegroundColor Yellow
    winget install -e --id Microsoft.AzureCLI
    Write-Host "Please restart your terminal and run this script again." -ForegroundColor Yellow
    exit
}

# Variables
$resourceGroup = "rg-school-ai-chatbot"
$webAppName = "app-wlanqwy7vuwmu"
$sqlServer = "school-chatbot-sql-10271900.database.windows.net"
$database = "school-ai-chatbot"
$sqlUser = "schooladmin"
$sqlPassword = "India@12345"
$openAiKey = "YOUR_OPENAI_API_KEY_HERE"
$jwtKey = "super-secure-jwt-secret-key-for-school-ai-chatbot-production-2024"

Write-Host "Logging in to Azure..." -ForegroundColor Cyan
az login

Write-Host "`nConfiguring App Service settings..." -ForegroundColor Cyan

# Set connection string
$connectionString = "Server=$sqlServer;Database=$database;User Id=$sqlUser;Password=$sqlPassword;Trusted_Connection=False;MultipleActiveResultSets=true;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config connection-string set `
    --resource-group $resourceGroup `
    --name $webAppName `
    --settings DefaultConnection="$connectionString" `
    --connection-string-type SQLAzure

# Set application settings
az webapp config appsettings set `
    --resource-group $resourceGroup `
    --name $webAppName `
    --settings `
        "DatabaseProvider=SqlServer" `
        "OpenAI__ApiKey=$openAiKey" `
        "Jwt__Key=$jwtKey" `
        "Jwt__Issuer=SchoolAiChatbotBackend" `
        "Jwt__Audience=SchoolAiChatbotUsers" `
        "ASPNETCORE_ENVIRONMENT=Production"

Write-Host "`n✅ Configuration complete!" -ForegroundColor Green
Write-Host "`nRestarting web app..." -ForegroundColor Cyan

az webapp restart `
    --resource-group $resourceGroup `
    --name $webAppName

Write-Host "`n✅ Web app restarted!" -ForegroundColor Green
Write-Host "`nYour backend should now be running at:" -ForegroundColor White
Write-Host "https://app-wlanqwy7vuwmu.azurewebsites.net" -ForegroundColor Cyan
Write-Host "`nWait 1-2 minutes for the app to fully start, then check:" -ForegroundColor Yellow
Write-Host "https://app-wlanqwy7vuwmu.azurewebsites.net/health" -ForegroundColor Cyan
