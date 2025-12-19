# Quick Deploy to Azure App Service
# Run: .\deploy-to-azure.ps1

$ErrorActionPreference = "Stop"

Write-Host "Building release..." -ForegroundColor Cyan
Set-Location C:\school-ai-chatbot\SchoolAiChatbotBackend
dotnet publish -c Release -o ./publish

Write-Host "Creating deployment package..." -ForegroundColor Cyan
Set-Location publish
if (Test-Path ../deploy.zip) { Remove-Item ../deploy.zip -Force }
Compress-Archive -Path * -DestinationPath ../deploy.zip -Force
Set-Location ..

Write-Host "Deploying to Azure..." -ForegroundColor Cyan
az webapp deploy --resource-group rg-smartstudy-dev --name smartstudy-api --src-path deploy.zip --type zip

Write-Host "Deployment complete!" -ForegroundColor Green
Write-Host "App URL: https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net" -ForegroundColor Yellow
