#!/usr/bin/env pwsh
# Quick Start Script - Runs Backend and Frontend Together

Write-Host "====================================" -ForegroundColor Cyan
Write-Host " School AI Chatbot - Quick Start" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Continue"

# Check if backend is already running
Write-Host "Checking backend status..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:8080/health" -Method GET -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 200) {
        Write-Host "Backend is already running on http://localhost:8080" -ForegroundColor Green
        $backendRunning = $true
    }
} catch {
    Write-Host "⚠️  Backend is not running. Starting it now..." -ForegroundColor Yellow
    $backendRunning = $false
    
    # Start backend in background
    Write-Host "`nStarting ASP.NET Core backend..." -ForegroundColor Cyan
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot\SchoolAiChatbotBackend'; dotnet run" -WindowStyle Normal
    
    Write-Host "Waiting for backend to start (15 seconds)..." -ForegroundColor Yellow
    Start-Sleep -Seconds 15
    
    # Verify backend started
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:8080/health" -Method GET -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Host "Backend started successfully!" -ForegroundColor Green
        }
    } catch {
        Write-Host "❌ Backend failed to start. Check the backend window for errors." -ForegroundColor Red
        Write-Host "   You may need to configure Azure SQL connection string." -ForegroundColor Yellow
    }
}

# Check if frontend is already running
Write-Host "`nChecking frontend status..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5173" -Method GET -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 200) {
        Write-Host "Frontend is already running on http://localhost:5173" -ForegroundColor Green
        $frontendRunning = $true
    }
} catch {
    Write-Host "⚠️  Frontend is not running. Starting it now..." -ForegroundColor Yellow
    $frontendRunning = $false
    
    # Start frontend in background
    Write-Host "`nStarting React frontend..." -ForegroundColor Cyan
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot\school-ai-frontend'; npm run dev" -WindowStyle Normal
    
    Write-Host "Waiting for frontend to start (10 seconds)..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10
    
    # Verify frontend started
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5173" -Method GET -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Host "Frontend started successfully!" -ForegroundColor Green
        }
    } catch {
        Write-Host "⚠️  Frontend may still be starting. Check the frontend window." -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "Services Status" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Backend:  http://localhost:8080" -ForegroundColor White
Write-Host "    Health:  http://localhost:8080/health" -ForegroundColor Gray
Write-Host "    API:     http://localhost:8080/api" -ForegroundColor Gray
Write-Host ""
Write-Host "Frontend: http://localhost:5173" -ForegroundColor White
Write-Host "    Chat:    http://localhost:5173/" -ForegroundColor Gray
Write-Host ""
Write-Host "Configuration:" -ForegroundColor White
Write-Host "    Using: .env.development" -ForegroundColor Gray
Write-Host "    API URL: http://localhost:8080" -ForegroundColor Gray
Write-Host ""
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Ready! Open http://localhost:5173 in your browser" -ForegroundColor Green
Write-Host ""
Write-Host "Press Ctrl+C in each terminal window to stop the servers." -ForegroundColor Yellow
Write-Host ""
