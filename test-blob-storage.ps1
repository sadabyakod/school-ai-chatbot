# Test Azure Blob Storage Connection
$connectionString = "DefaultEndpointsProtocol=https;AccountName=studyaistorage345;AccountKey=KNfDL6woZuGczjODsxfYEYENNVKxtQiEt3/7N8lgf/opb5ytHGhb69iWtts0Efe1n6QNgsIIp7mF+ASt80rpOg==;EndpointSuffix=core.windows.net"
$containerName = "textbooks"

Write-Host "Testing Azure Blob Storage connection..." -ForegroundColor Cyan

# Install Azure Storage module if not present
if (-not (Get-Module -ListAvailable -Name Az.Storage)) {
    Write-Host "Installing Az.Storage module..." -ForegroundColor Yellow
    Install-Module -Name Az.Storage -Force -AllowClobber -Scope CurrentUser
}

try {
    # Create context
    $context = New-AzStorageContext -ConnectionString $connectionString
    
    # Check if container exists
    $container = Get-AzStorageContainer -Name $containerName -Context $context -ErrorAction SilentlyContinue
    
    if ($null -eq $container) {
        Write-Host "Container '$containerName' does not exist. Creating..." -ForegroundColor Yellow
        New-AzStorageContainer -Name $containerName -Context $context -Permission Blob
        Write-Host "✅ Container created successfully!" -ForegroundColor Green
    } else {
        Write-Host "✅ Container '$containerName' already exists" -ForegroundColor Green
    }
    
    # Test upload
    $testFile = "$env:TEMP\blob-test.txt"
    "Test content" | Out-File -FilePath $testFile
    
    $blobName = "test-$(Get-Date -Format 'yyyyMMddHHmmss').txt"
    Set-AzStorageBlobContent -File $testFile -Container $containerName -Blob $blobName -Context $context -Force | Out-Null
    
    Write-Host "✅ Test file uploaded successfully!" -ForegroundColor Green
    Write-Host "Blob URL: https://studyaistorage345.blob.core.windows.net/$containerName/$blobName" -ForegroundColor Cyan
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack: $($_.Exception.StackTrace)" -ForegroundColor Yellow
}
