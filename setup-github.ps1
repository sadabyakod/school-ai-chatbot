#!/usr/bin/env pwsh
# Simple GitHub Repository Setup Script
# This script provides you with the exact commands to run after creating the repository manually

param(
    [Parameter(Mandatory=$false)]
    [string]$GitHubUsername = "",
    
    [Parameter(Mandatory=$false)]
    [string]$RepositoryName = "image-api-winforms"
)

Write-Host "🚀 Image API - GitHub Repository Setup" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

# Step 1: Check current git status
Write-Host "📊 Checking current repository status..." -ForegroundColor Yellow
$gitStatus = git status --porcelain
$gitLog = git log --oneline -3

Write-Host "Current Git Status:" -ForegroundColor Cyan
if ($gitStatus) {
    Write-Host "Uncommitted changes found:" -ForegroundColor Red
    $gitStatus
    Write-Host "Committing changes..." -ForegroundColor Yellow
    git add .
    git commit -m "Auto-commit before GitHub setup - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
} else {
    Write-Host "✅ Working tree is clean" -ForegroundColor Green
}

Write-Host "`nRecent commits:" -ForegroundColor Cyan
$gitLog

# Step 2: Display repository information
Write-Host "`n📋 Repository Information:" -ForegroundColor Yellow
Write-Host "Current directory: $(Get-Location)" -ForegroundColor White
Write-Host "Repository name: $RepositoryName" -ForegroundColor White
Write-Host "Total files: $((Get-ChildItem -Recurse -File | Where-Object { $_.FullName -notlike "*\.git\*" -and $_.FullName -notlike "*\bin\*" -and $_.FullName -notlike "*\obj\*" }).Count)" -ForegroundColor White

# Step 3: Display the files that will be uploaded
Write-Host "`n📁 Files to be uploaded:" -ForegroundColor Yellow
$files = git ls-files | Sort-Object
$files | ForEach-Object { Write-Host "  ✓ $_" -ForegroundColor Green }

Write-Host "`n🌐 STEP-BY-STEP GITHUB SETUP:" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

Write-Host "`n1️⃣ CREATE REPOSITORY ON GITHUB:" -ForegroundColor Yellow
Write-Host "   → Go to: https://github.com/new" -ForegroundColor White
Write-Host "   → Repository name: $RepositoryName" -ForegroundColor White
Write-Host "   → Description: .NET Core Image API with WinForms Base64 integration" -ForegroundColor White
Write-Host "   → Choose Public or Private" -ForegroundColor White
Write-Host "   → ❌ DO NOT initialize with README, .gitignore, or license" -ForegroundColor Red
Write-Host "   → Click 'Create repository'" -ForegroundColor White

Write-Host "`n2️⃣ CONNECT AND PUSH (Run these commands):" -ForegroundColor Yellow

if ($GitHubUsername) {
    $repoUrl = "https://github.com/$GitHubUsername/$RepositoryName.git"
    Write-Host "git remote add origin $repoUrl" -ForegroundColor Green
} else {
    Write-Host "git remote add origin https://github.com/YOUR_USERNAME/$RepositoryName.git" -ForegroundColor Green
    Write-Host "   (Replace YOUR_USERNAME with your GitHub username)" -ForegroundColor Gray
}

Write-Host "git branch -M main" -ForegroundColor Green
Write-Host "git push -u origin main" -ForegroundColor Green

Write-Host "`n3️⃣ VERIFY UPLOAD:" -ForegroundColor Yellow
Write-Host "   Your repository will contain:" -ForegroundColor White
Write-Host "   ✅ Complete .NET 9.0 Web API source code" -ForegroundColor Green
Write-Host "   ✅ Controllers, Models, Data layers" -ForegroundColor Green
Write-Host "   ✅ Entity Framework migrations" -ForegroundColor Green
Write-Host "   ✅ Comprehensive documentation" -ForegroundColor Green
Write-Host "   ✅ WinForms integration examples" -ForegroundColor Green
Write-Host "   ✅ Test images and sample data" -ForegroundColor Green

Write-Host "`n📊 REPOSITORY STATISTICS:" -ForegroundColor Cyan
Write-Host "Files: $($files.Count)" -ForegroundColor White
Write-Host "Commits: $(git rev-list --count HEAD)" -ForegroundColor White
Write-Host "Size: ~$('{0:N0}' -f ((Get-ChildItem -Recurse -File | Where-Object { $_.FullName -notlike "*\.git\*" } | Measure-Object -Property Length -Sum).Sum / 1KB)) KB" -ForegroundColor White

Write-Host "`n🎯 WHAT YOU'RE GETTING:" -ForegroundColor Cyan
Write-Host "✅ Production-ready Image API" -ForegroundColor Green
Write-Host "✅ Base64 encoding for WinForms" -ForegroundColor Green
Write-Host "✅ SQL Server integration" -ForegroundColor Green
Write-Host "✅ Swagger documentation" -ForegroundColor Green
Write-Host "✅ Professional project structure" -ForegroundColor Green
Write-Host "✅ Comprehensive documentation" -ForegroundColor Green

Write-Host "`n🚀 Ready to push to GitHub!" -ForegroundColor Green
Write-Host "Your repository is fully prepared with all necessary files." -ForegroundColor White

# Create a quick reference file
$quickRef = @"
# Quick Reference - GitHub Setup Commands

## After creating repository on GitHub:

``````bash
git remote add origin https://github.com/YOUR_USERNAME/$RepositoryName.git
git branch -M main
git push -u origin main
``````

## Repository Details:
- Name: $RepositoryName
- Type: .NET 9.0 Web API
- Features: Base64 Image serving, WinForms integration
- Files: $($files.Count) source files
- Documentation: Complete guides and examples

## Access after upload:
- Repository: https://github.com/YOUR_USERNAME/$RepositoryName
- API Documentation: Run ``dotnet run`` then visit /swagger
- Clone: ``git clone https://github.com/YOUR_USERNAME/$RepositoryName.git``
"@

$quickRef | Out-File -FilePath "GITHUB_COMMANDS.md" -Encoding UTF8
Write-Host "`n💾 Quick reference saved to: GITHUB_COMMANDS.md" -ForegroundColor Yellow