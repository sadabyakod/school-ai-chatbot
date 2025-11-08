#!/usr/bin/env pwsh
# GitHub Repository Creation Script
# This script will create a new GitHub repository and push your code automatically

param(
    [Parameter(Mandatory=$true)]
    [string]$GitHubUsername,
    
    [Parameter(Mandatory=$true)]
    [string]$GitHubToken,
    
    [Parameter(Mandatory=$false)]
    [string]$RepositoryName = "image-api-winforms",
    
    [Parameter(Mandatory=$false)]
    [string]$Description = ".NET Core Image API with WinForms Base64 integration - Professional solution for serving images from SQL Server",
    
    [Parameter(Mandatory=$false)]
    [bool]$IsPrivate = $false
)

Write-Host "🚀 Starting GitHub Repository Creation Process..." -ForegroundColor Green
Write-Host "Repository Name: $RepositoryName" -ForegroundColor Cyan
Write-Host "Username: $GitHubUsername" -ForegroundColor Cyan
Write-Host "Private: $IsPrivate" -ForegroundColor Cyan

# Step 1: Verify we're in the correct directory
$currentDir = Get-Location
Write-Host "📁 Current Directory: $currentDir" -ForegroundColor Yellow

if (!(Test-Path ".git")) {
    Write-Error "❌ Not a git repository! Please run this script from the winform_project directory."
    exit 1
}

# Step 2: Check git status
Write-Host "📊 Checking git status..." -ForegroundColor Yellow
$gitStatus = git status --porcelain
if ($gitStatus) {
    Write-Warning "⚠️ Uncommitted changes found. Committing them first..."
    git add .
    git commit -m "Final commit before GitHub push - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
}

# Step 3: Create GitHub repository using GitHub CLI or API
Write-Host "🌐 Creating GitHub repository..." -ForegroundColor Yellow

# Try using GitHub CLI first (gh command)
$ghExists = Get-Command gh -ErrorAction SilentlyContinue
if ($ghExists) {
    Write-Host "Using GitHub CLI (gh)..." -ForegroundColor Green
    
    $visibility = if ($IsPrivate) { "--private" } else { "--public" }
    
    try {
        gh repo create "$GitHubUsername/$RepositoryName" $visibility --description "$Description" --source=. --push
        Write-Host "✅ Repository created successfully using GitHub CLI!" -ForegroundColor Green
        
        # Open the repository in browser
        gh repo view "$GitHubUsername/$RepositoryName" --web
        
        Write-Host "🎉 Complete! Your repository is now live at:" -ForegroundColor Green
        Write-Host "https://github.com/$GitHubUsername/$RepositoryName" -ForegroundColor Cyan
        exit 0
    }
    catch {
        Write-Warning "GitHub CLI failed, trying manual approach..."
    }
}

# Alternative: Manual API approach
Write-Host "Using GitHub REST API..." -ForegroundColor Yellow

# Prepare the API request
$headers = @{
    "Authorization" = "Bearer $GitHubToken"
    "Accept" = "application/vnd.github.v3+json"
    "Content-Type" = "application/json"
}

$body = @{
    name = $RepositoryName
    description = $Description
    private = $IsPrivate
    auto_init = $false
} | ConvertTo-Json

try {
    # Create the repository
    Write-Host "📡 Sending API request to GitHub..." -ForegroundColor Yellow
    $response = Invoke-RestMethod -Uri "https://api.github.com/user/repos" -Method Post -Headers $headers -Body $body
    
    Write-Host "✅ Repository created successfully!" -ForegroundColor Green
    Write-Host "Repository URL: $($response.html_url)" -ForegroundColor Cyan
    
    # Step 4: Add remote origin
    Write-Host "🔗 Adding remote origin..." -ForegroundColor Yellow
    git remote remove origin -ErrorAction SilentlyContinue  # Remove if exists
    git remote add origin $response.clone_url
    
    # Step 5: Push to GitHub
    Write-Host "📤 Pushing code to GitHub..." -ForegroundColor Yellow
    git branch -M main
    git push -u origin main
    
    Write-Host "🎉 SUCCESS! Your repository is now live!" -ForegroundColor Green
    Write-Host "Repository URL: $($response.html_url)" -ForegroundColor Cyan
    Write-Host "Clone URL: $($response.clone_url)" -ForegroundColor Cyan
    
    # Try to open in browser
    try {
        Start-Process $response.html_url
    }
    catch {
        Write-Host "💡 You can view your repository at: $($response.html_url)" -ForegroundColor Yellow
    }
    
} catch {
    Write-Error "❌ Failed to create repository: $($_.Exception.Message)"
    
    if ($_.Exception.Message -like "*401*") {
        Write-Host "🔑 Token authentication failed. Please check your GitHub token." -ForegroundColor Red
        Write-Host "💡 Make sure your token has 'repo' permissions." -ForegroundColor Yellow
    } elseif ($_.Exception.Message -like "*422*") {
        Write-Host "📛 Repository might already exist or name is invalid." -ForegroundColor Red
        Write-Host "💡 Try a different repository name." -ForegroundColor Yellow
    }
    
    Write-Host "🔧 Manual Setup Alternative:" -ForegroundColor Yellow
    Write-Host "1. Go to https://github.com/new" -ForegroundColor White
    Write-Host "2. Create repository named: $RepositoryName" -ForegroundColor White
    Write-Host "3. Run these commands:" -ForegroundColor White
    Write-Host "   git remote add origin https://github.com/$GitHubUsername/$RepositoryName.git" -ForegroundColor Gray
    Write-Host "   git branch -M main" -ForegroundColor Gray
    Write-Host "   git push -u origin main" -ForegroundColor Gray
    
    exit 1
}

Write-Host "✨ Repository creation completed successfully!" -ForegroundColor Green