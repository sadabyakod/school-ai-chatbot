# Quick Deploy to Azure App Service
# Run: .\deploy-to-azure.ps1

Write-Host "🚀 Building release..." -ForegroundColor Cyan
cd C:\school-ai-chatbot\SchoolAiChatbotBackend
dotnet publish -c Release -o ./publish 2>&1 | Out-Null

Write-Host "📦 Creating deployment package..." -ForegroundColor Cyan
cd publish
Compress-Archive -Path * -DestinationPath ../deploy.zip -Force
cd ..

Write-Host "☁️ Deploying to Azure..." -ForegroundColor Cyan
az webapp deploy --resource-group rg-smartstudy-dev --name smartstudy-api --src-path deploy.zip --type zip

Write-Host "✅ Deployment complete!" -ForegroundColor Green
Write-Host "🌐 App URL: https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net" -ForegroundColor Yellow
