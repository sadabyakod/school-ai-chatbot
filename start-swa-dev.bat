@echo off
REM Azure Static Web Apps Local Development Setup

echo 🚀 Setting up Azure Static Web Apps local development...

REM Check if Azure Functions Core Tools is installed
where func >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Azure Functions Core Tools not found. Installing...
    npm install -g azure-functions-core-tools@4 --unsafe-perm true
) else (
    echo ✅ Azure Functions Core Tools found
)

REM Check if Static Web Apps CLI is installed
where swa >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Static Web Apps CLI not found. Installing...
    npm install -g @azure/static-web-apps-cli
) else (
    echo ✅ Static Web Apps CLI found
)

echo 🔧 Setting up environment...

REM Copy environment file for SWA
cd school-ai-frontend
copy .env.swa .env
echo ✅ Environment configured for Static Web Apps

REM Install frontend dependencies
echo 📦 Installing frontend dependencies...
call npm install

echo 🏗️  Starting local development environment...
echo Frontend will run on: http://localhost:4280
echo API will run on: http://localhost:7071
echo.
echo Use Ctrl+C to stop the development server

REM Start SWA emulator (this will start both frontend and API)
cd ..
swa start school-ai-frontend --api-location api --port 4280