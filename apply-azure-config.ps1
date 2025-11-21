# Script to apply Azure environment variables to local appsettings.Development.json
# This helps synchronize your local development settings with Azure production settings

Write-Host "`n=== Azure Configuration Helper ===" -ForegroundColor Cyan
Write-Host "This script will help you update appsettings.Development.json with Azure values" -ForegroundColor Yellow

# Path to appsettings file
$settingsFile = "SchoolAiChatbotBackend\appsettings.Development.json"

# Read current settings
$settings = Get-Content $settingsFile | ConvertFrom-Json

Write-Host "`nPlease provide the following values from your Azure Portal:" -ForegroundColor Green
Write-Host "(You can find these in: App Service > Configuration > Application settings)" -ForegroundColor Gray

# Prompt for Azure OpenAI settings
Write-Host "`n--- Azure OpenAI Settings ---" -ForegroundColor Cyan
$endpoint = Read-Host "Enter AzureOpenAI__Endpoint (from Azure Portal)"
$apiKey = Read-Host "Enter AzureOpenAI__ApiKey (from Azure Portal)" -AsSecureString
$apiKeyPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($apiKey))
$chatDeployment = Read-Host "Enter AzureOpenAI__ChatDeployment (default: gpt-4)"
$embeddingDeployment = Read-Host "Enter AzureOpenAI__EmbeddingDeployment (default: text-embedding-3-small)"

# Update settings
$settings.AzureOpenAI.Endpoint = if ($endpoint) { $endpoint } else { $settings.AzureOpenAI.Endpoint }
$settings.AzureOpenAI.ApiKey = if ($apiKeyPlain) { $apiKeyPlain } else { $settings.AzureOpenAI.ApiKey }
$settings.AzureOpenAI.ChatDeployment = if ($chatDeployment) { $chatDeployment } else { "gpt-4" }
$settings.AzureOpenAI.EmbeddingDeployment = if ($embeddingDeployment) { $embeddingDeployment } else { "text-embedding-3-small" }

# Save settings
$settings | ConvertTo-Json -Depth 10 | Set-Content $settingsFile

Write-Host "`n=== Configuration Updated Successfully ===" -ForegroundColor Green
Write-Host "File updated: $settingsFile" -ForegroundColor White
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Restart the backend: .\start-backend.ps1" -ForegroundColor Yellow
Write-Host "2. Test the chat functionality" -ForegroundColor Yellow
