@echo off
echo ===============================================
echo School AI Chatbot Database Setup
echo ===============================================
echo.

REM Check if mysql command is available
mysql --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: MySQL client not found!
    echo Please install MySQL client from: https://dev.mysql.com/downloads/mysql/
    pause
    exit /b 1
)

REM Check if SQL file exists
if not exist "database-setup.sql" (
    echo ERROR: database-setup.sql not found!
    echo Make sure you're running this from the correct directory.
    pause
    exit /b 1
)

REM Get connection details
set /p password="Enter MySQL password: "
set server=school-ai-mysql-server.mysql.database.azure.com
set username=adminuser
set database=flexibleserverdb

echo.
echo Connecting to: %server%
echo Database: %database%
echo Username: %username%
echo.

REM Test connection
echo Testing connection...
mysql -h %server% -u %username% -p%password% -D %database% -e "SELECT 'Connection successful!' as Status;" 2>nul
if %errorlevel% neq 0 (
    echo ERROR: Could not connect to database!
    echo Please check your password and network connection.
    pause
    exit /b 1
)

echo Connection successful!
echo.

REM Execute setup script
echo Running database setup...
mysql -h %server% -u %username% -p%password% -D %database% < database-setup.sql
if %errorlevel% neq 0 (
    echo ERROR: Database setup failed!
    pause
    exit /b 1
)

echo.
echo SUCCESS: Database setup completed!
echo.
echo Your School AI Chatbot database is ready.
echo.
echo Next steps:
echo 1. Add MYSQL_PASSWORD to GitHub Secrets
echo 2. Push your updated deployment workflow
echo 3. Deploy your API to use the MySQL database
echo.
pause