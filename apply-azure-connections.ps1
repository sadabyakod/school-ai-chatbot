# Script to apply Azure SQL and Blob Storage connection strings
Write-Host "`n=== Azure Connections Configuration ===" -ForegroundColor Cyan
Write-Host "This script will update SQL and Blob Storage connections" -ForegroundColor Yellow

$settingsFile = "SchoolAiChatbotBackend\appsettings.Development.json"
$settings = Get-Content $settingsFile | ConvertFrom-Json

Write-Host "`nFrom your Azure Portal, get the following:" -ForegroundColor Green
Write-Host "(App Service > Configuration > Connection strings or Application settings)" -ForegroundColor Gray

# SQL Connection String
Write-Host "`n--- SQL Database Connection ---" -ForegroundColor Cyan
Write-Host "You can find this in:" -ForegroundColor Gray
Write-Host "  - Azure SQL Database > Connection strings" -ForegroundColor Gray
Write-Host "  - Or App Service > Configuration > Connection strings" -ForegroundColor Gray
$sqlConnection = Read-Host "`nEnter SQL Connection String"

# Blob Storage Connection String
Write-Host "`n--- Blob Storage Connection ---" -ForegroundColor Cyan
Write-Host "You can find this in:" -ForegroundColor Gray
Write-Host "  - Storage Account > Access keys" -ForegroundColor Gray
Write-Host "  - Or App Service > Configuration > AzureStorage__ConnectionString" -ForegroundColor Gray
$blobConnection = Read-Host "`nEnter Blob Storage Connection String"

# Container Name (optional)
Write-Host "`n--- Container Name ---" -ForegroundColor Cyan
$containerName = Read-Host "Enter Container Name (press Enter to keep: $($settings.BlobStorage.ContainerName))"

# Update settings
if ($sqlConnection) {
    $settings.ConnectionStrings.DefaultConnection = $sqlConnection
}

if ($blobConnection) {
    $settings.BlobStorage.ConnectionString = $blobConnection
}

if ($containerName) {
    $settings.BlobStorage.ContainerName = $containerName
}

# Save settings
$settings | ConvertTo-Json -Depth 10 | Set-Content $settingsFile

Write-Host "`n=== Configuration Updated Successfully ===" -ForegroundColor Green
Write-Host "Updated: $settingsFile" -ForegroundColor White
Write-Host "`nNext step: Start the backend" -ForegroundColor Cyan
Write-Host "cd SchoolAiChatbotBackend; dotnet run --urls `"http://localhost:8080`"" -ForegroundColor Yellow
