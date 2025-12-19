# Apply MCQ migrations to Azure SQL Database
$server = "smartstudysqlsrv.database.windows.net"
$database = "smartstudydb"
$username = "schooladmin"
$password = "India@12345"

$connectionString = "Server=tcp:$server,1433;Initial Catalog=$database;Persist Security Info=False;User ID=$username;Password=$password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

Write-Host "Connecting to Azure SQL Database..." -ForegroundColor Cyan
Write-Host "Server: $server" -ForegroundColor Gray
Write-Host "Database: $database" -ForegroundColor Gray
Write-Host ""

try {
    # Load SQL Client assembly
    Add-Type -AssemblyName System.Data

    # Create connection
    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = $connectionString
    $connection.Open()
    
    Write-Host "✅ Connected successfully!" -ForegroundColor Green
    Write-Host ""

    # Migration 1: Add McqAnswers column
    Write-Host "Running Migration 1: Add McqAnswers column..." -ForegroundColor Yellow
    
    $sql1 = @"
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'WrittenSubmissions' 
    AND COLUMN_NAME = 'McqAnswers'
)
BEGIN
    ALTER TABLE WrittenSubmissions
    ADD McqAnswers NVARCHAR(MAX) NULL;
    SELECT 'McqAnswers column added successfully.' AS Result;
END
ELSE
BEGIN
    SELECT 'McqAnswers column already exists.' AS Result;
END
"@

    $command1 = $connection.CreateCommand()
    $command1.CommandText = $sql1
    $result1 = $command1.ExecuteScalar()
    Write-Host "  $result1" -ForegroundColor Gray

    # Migration 2: Add McqScore column
    Write-Host "Running Migration 2: Add McqScore column..." -ForegroundColor Yellow
    
    $sql2 = @"
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('WrittenSubmissions') 
    AND name = 'McqScore'
)
BEGIN
    ALTER TABLE WrittenSubmissions
    ADD McqScore DECIMAL(10,2) NULL;
    SELECT 'McqScore column added successfully.' AS Result;
END
ELSE
BEGIN
    SELECT 'McqScore column already exists.' AS Result;
END
"@

    $command2 = $connection.CreateCommand()
    $command2.CommandText = $sql2
    $result2 = $command2.ExecuteScalar()
    Write-Host "  $result2" -ForegroundColor Gray

    # Migration 3: Add McqTotalMarks column
    Write-Host "Running Migration 3: Add McqTotalMarks column..." -ForegroundColor Yellow
    
    $sql3 = @"
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('WrittenSubmissions') 
    AND name = 'McqTotalMarks'
)
BEGIN
    ALTER TABLE WrittenSubmissions
    ADD McqTotalMarks DECIMAL(10,2) NULL;
    SELECT 'McqTotalMarks column added successfully.' AS Result;
END
ELSE
BEGIN
    SELECT 'McqTotalMarks column already exists.' AS Result;
END
"@

    $command3 = $connection.CreateCommand()
    $command3.CommandText = $sql3
    $result3 = $command3.ExecuteScalar()
    Write-Host "  $result3" -ForegroundColor Gray
    Write-Host ""

    # Verify columns
    Write-Host "Verifying columns..." -ForegroundColor Cyan
    $verifySQL = @"
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'WrittenSubmissions'
AND COLUMN_NAME IN ('McqAnswers', 'McqScore', 'McqTotalMarks')
ORDER BY COLUMN_NAME
"@

    $command4 = $connection.CreateCommand()
    $command4.CommandText = $verifySQL
    $reader = $command4.ExecuteReader()
    
    Write-Host ""
    Write-Host "Column Details:" -ForegroundColor Green
    Write-Host ("-" * 80) -ForegroundColor Gray
    Write-Host ("{0,-20} {1,-20} {2,-15} {3}" -f "COLUMN_NAME", "DATA_TYPE", "IS_NULLABLE", "MAX_LENGTH") -ForegroundColor White
    Write-Host ("-" * 80) -ForegroundColor Gray
    
    while ($reader.Read()) {
        $colName = $reader["COLUMN_NAME"]
        $dataType = $reader["DATA_TYPE"]
        $isNullable = $reader["IS_NULLABLE"]
        $maxLength = if ($reader["CHARACTER_MAXIMUM_LENGTH"] -is [DBNull]) { "N/A" } else { $reader["CHARACTER_MAXIMUM_LENGTH"] }
        Write-Host ("{0,-20} {1,-20} {2,-15} {3}" -f $colName, $dataType, $isNullable, $maxLength) -ForegroundColor Gray
    }
    $reader.Close()
    
    Write-Host ("-" * 80) -ForegroundColor Gray
    Write-Host ""
    Write-Host "✅ Migration completed successfully!" -ForegroundColor Green
    
    $connection.Close()
}
catch {
    Write-Host ""
    Write-Host "❌ Error occurred:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Stack Trace:" -ForegroundColor Yellow
    Write-Host $_.Exception.StackTrace -ForegroundColor Gray
    exit 1
}
