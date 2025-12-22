# Quick Deploy to Azure App Service
# Run: .\deploy-to-azure.ps1

$ErrorActionPreference = "Stop"

Write-Host "Building release..." -ForegroundColor Cyan
Set-Location C:\school-ai-chatbot\SchoolAiChatbotBackend
dotnet publish -c Release -o ./publish

# Run DB migration for RubricBlobPath (if migration runner present)
$migrationRunner = Join-Path $PSScriptRoot 'DatabaseSetup\migrations\apply-add-rubric-blobpath.ps1'
if (Test-Path $migrationRunner) {
	Write-Host "Found DB migration runner: $migrationRunner" -ForegroundColor Cyan
	if ($env:DB_CONN) {
		Write-Host "Applying DB migration using DB_CONN environment variable..." -ForegroundColor Cyan
		& $migrationRunner -ConnectionString $env:DB_CONN -Force
	}
	else {
		Write-Host "DB_CONN environment variable not set. Skipping automated DB migration." -ForegroundColor Yellow
		Write-Host "To run migration during deploy, set DB_CONN or run DatabaseSetup\migrations\apply-add-rubric-blobpath.ps1 manually." -ForegroundColor Yellow
	}
}
else {
	Write-Host "Migration runner not found at $migrationRunner; skipping DB migration step." -ForegroundColor Yellow
}

Write-Host "Creating deployment package..." -ForegroundColor Cyan
Set-Location publish
if (Test-Path ../deploy.zip) { Remove-Item ../deploy.zip -Force }
Compress-Archive -Path * -DestinationPath ../deploy.zip -Force
Set-Location ..

Write-Host "Deploying to Azure..." -ForegroundColor Cyan
az webapp deploy --resource-group rg-smartstudy-dev --name smartstudy-api --src-path deploy.zip --type zip

Write-Host "Deployment complete!" -ForegroundColor Green
Write-Host "App URL: https://smartstudy-api-athtbtapcvdjesbe.centralindia-01.azurewebsites.net" -ForegroundColor Yellow
