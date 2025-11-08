# Simple GitHub Repository Setup Script
# This script provides you with the exact commands to run after creating the repository manually

param(
    [Parameter(Mandatory=$false)]
    [string]$GitHubUsername = "",
    
    [Parameter(Mandatory=$false)]
    [string]$RepositoryName = "image-api-winforms"
)

Write-Host "GitHub Repository Setup" -ForegroundColor Green
Write-Host "========================" -ForegroundColor Green

# Step 1: Check current git status
Write-Host "Checking current repository status..." -ForegroundColor Yellow
$gitStatus = git status --porcelain
$gitLog = git log --oneline -3

Write-Host "Current Git Status:" -ForegroundColor Cyan
if ($gitStatus) {
    Write-Host "Uncommitted changes found:" -ForegroundColor Red
    $gitStatus
    Write-Host "Committing changes..." -ForegroundColor Yellow
    git add .
    git commit -m "Auto-commit before GitHub setup"
} else {
    Write-Host "Working tree is clean" -ForegroundColor Green
}

Write-Host ""
Write-Host "Recent commits:" -ForegroundColor Cyan
$gitLog

# Step 2: Display repository information
Write-Host ""
Write-Host "Repository Information:" -ForegroundColor Yellow
Write-Host "Current directory: $(Get-Location)" -ForegroundColor White
Write-Host "Repository name: $RepositoryName" -ForegroundColor White

# Step 3: Display the files that will be uploaded
Write-Host ""
Write-Host "Files to be uploaded:" -ForegroundColor Yellow
$files = git ls-files | Sort-Object
$files | ForEach-Object { Write-Host "  * $_" -ForegroundColor Green }

Write-Host ""
Write-Host "STEP-BY-STEP GITHUB SETUP:" -ForegroundColor Cyan
Write-Host "===========================" -ForegroundColor Cyan

Write-Host ""
Write-Host "1. CREATE REPOSITORY ON GITHUB:" -ForegroundColor Yellow
Write-Host "   -> Go to: https://github.com/new" -ForegroundColor White
Write-Host "   -> Repository name: $RepositoryName" -ForegroundColor White
Write-Host "   -> Description: .NET Core Image API with WinForms Base64 integration" -ForegroundColor White
Write-Host "   -> Choose Public or Private" -ForegroundColor White
Write-Host "   -> DO NOT initialize with README, .gitignore, or license" -ForegroundColor Red
Write-Host "   -> Click 'Create repository'" -ForegroundColor White

Write-Host ""
Write-Host "2. CONNECT AND PUSH (Run these commands):" -ForegroundColor Yellow

if ($GitHubUsername) {
    $repoUrl = "https://github.com/$GitHubUsername/$RepositoryName.git"
    Write-Host "git remote add origin $repoUrl" -ForegroundColor Green
} else {
    Write-Host "git remote add origin https://github.com/YOUR_USERNAME/$RepositoryName.git" -ForegroundColor Green
    Write-Host "   (Replace YOUR_USERNAME with your GitHub username)" -ForegroundColor Gray
}

Write-Host "git branch -M main" -ForegroundColor Green
Write-Host "git push -u origin main" -ForegroundColor Green

Write-Host ""
Write-Host "REPOSITORY STATISTICS:" -ForegroundColor Cyan
Write-Host "Files: $($files.Count)" -ForegroundColor White
Write-Host "Commits: $(git rev-list --count HEAD)" -ForegroundColor White

Write-Host ""
Write-Host "Ready to push to GitHub!" -ForegroundColor Green