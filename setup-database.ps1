# PowerShell script to set up MySQL database for School AI Chatbot
# Run this script after ensuring MySQL client is installed

param(
    [Parameter(Mandatory=$true)]
    [string]$Password,
    
    [Parameter(Mandatory=$false)]
    [string]$Server = "school-ai-mysql-server.mysql.database.azure.com",
    
    [Parameter(Mandatory=$false)]
    [string]$Username = "adminuser",
    
    [Parameter(Mandatory=$false)]
    [string]$Database = "flexibleserverdb",
    
    [Parameter(Mandatory=$false)]
    [string]$SqlFile = "database-setup.sql"
)

Write-Host "Setting up School AI Chatbot Database..." -ForegroundColor Green
Write-Host "Server: $Server" -ForegroundColor Yellow
Write-Host "Database: $Database" -ForegroundColor Yellow
Write-Host "Username: $Username" -ForegroundColor Yellow

# Check if MySQL client is available
try {
    $mysqlVersion = mysql --version
    Write-Host "MySQL client found: $mysqlVersion" -ForegroundColor Green
} catch {
    Write-Host "ERROR: MySQL client not found. Please install MySQL client first." -ForegroundColor Red
    Write-Host "You can download it from: https://dev.mysql.com/downloads/mysql/" -ForegroundColor Yellow
    exit 1
}

# Check if SQL file exists
if (-not (Test-Path $SqlFile)) {
    Write-Host "ERROR: SQL file '$SqlFile' not found in current directory." -ForegroundColor Red
    Write-Host "Make sure you're running this script from the directory containing $SqlFile" -ForegroundColor Yellow
    exit 1
}

# Test connection first
Write-Host "Testing connection to MySQL server..." -ForegroundColor Yellow
try {
    $testQuery = "SELECT VERSION();"
    $testResult = mysql -h $Server -u $Username -p$Password -D $Database -e $testQuery 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Connection successful!" -ForegroundColor Green
        Write-Host "MySQL Server Version: $($testResult | Select-String 'VERSION')" -ForegroundColor Cyan
    } else {
        Write-Host "✗ Connection failed!" -ForegroundColor Red
        Write-Host "Error: $testResult" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "✗ Connection test failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Execute the SQL setup script
Write-Host "Executing database setup script..." -ForegroundColor Yellow
try {
    $result = mysql -h $Server -u $Username -p$Password -D $Database < $SqlFile 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Database setup completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "✗ Database setup failed!" -ForegroundColor Red
        Write-Host "Error: $result" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "✗ Error executing SQL script: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Verify tables were created
Write-Host "Verifying database setup..." -ForegroundColor Yellow
try {
    $tablesQuery = "SHOW TABLES;"
    $tables = mysql -h $Server -u $Username -p$Password -D $Database -e $tablesQuery 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Database tables created:" -ForegroundColor Green
        Write-Host $tables -ForegroundColor Cyan
    } else {
        Write-Host "✗ Failed to verify tables!" -ForegroundColor Red
        Write-Host "Error: $tables" -ForegroundColor Red
    }
} catch {
    Write-Host "Warning: Could not verify table creation: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Show record counts
Write-Host "Checking sample data..." -ForegroundColor Yellow
try {
    $countQuery = @"
SELECT 'Schools' as TableName, COUNT(*) as RecordCount FROM Schools
UNION ALL
SELECT 'Users' as TableName, COUNT(*) as RecordCount FROM Users
UNION ALL
SELECT 'UploadedFiles' as TableName, COUNT(*) as RecordCount FROM UploadedFiles
UNION ALL
SELECT 'SyllabusChunks' as TableName, COUNT(*) as RecordCount FROM SyllabusChunks
UNION ALL
SELECT 'Faqs' as TableName, COUNT(*) as RecordCount FROM Faqs
UNION ALL
SELECT 'Embeddings' as TableName, COUNT(*) as RecordCount FROM Embeddings
UNION ALL
SELECT 'ChatLogs' as TableName, COUNT(*) as RecordCount FROM ChatLogs;
"@
    
    $counts = mysql -h $Server -u $Username -p$Password -D $Database -e $countQuery 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Sample data verification:" -ForegroundColor Green
        Write-Host $counts -ForegroundColor Cyan
    } else {
        Write-Host "Warning: Could not verify sample data" -ForegroundColor Yellow
    }
} catch {
    Write-Host "Warning: Could not check sample data: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== DATABASE SETUP COMPLETE ===" -ForegroundColor Green
Write-Host "Your School AI Chatbot database is now ready!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Add MYSQL_PASSWORD to GitHub Secrets" -ForegroundColor White
Write-Host "2. Push the updated deployment workflow" -ForegroundColor White
Write-Host "3. Deploy your API to connect to this database" -ForegroundColor White
Write-Host ""
Write-Host "Connection details for your app:" -ForegroundColor Cyan
Write-Host "Server: $Server" -ForegroundColor White
Write-Host "Database: $Database" -ForegroundColor White
Write-Host "Username: $Username" -ForegroundColor White