# 🛠️ Manual Azure Deployment Guide

## Issue: Quota Limitation
Your Azure subscription currently has 0 quota for App Service Plans. This is common with new subscriptions.

## Solution 1: Request Quota Increase (Easiest)

1. **Visit Azure Portal**: https://portal.azure.com/#blade/Microsoft_Azure_Capacity/QuotaMenuBlade/myQuotas
2. **Search for "App Service"** in quotas
3. **Request increase** for Basic App Service Plans (request at least 1)
4. **Wait for approval** (usually instant for basic quotas)

## Solution 2: Manual Deployment (If quota request doesn't work)

### Step 1: Create Resource Group
```bash
az group create --name school-ai-chatbot-rg --location eastus
```

### Step 2: Try Different Regions
Some regions might have available quota:
```bash
# Try West US 2
az group create --name school-ai-chatbot-rg-west --location westus2

# Try Central US  
az group create --name school-ai-chatbot-rg-central --location centralus
```

### Step 3: Create App Service Plan in Different Region
```bash
az appservice plan create \
  --name school-ai-chatbot-plan \
  --resource-group school-ai-chatbot-rg-west \
  --location westus2 \
  --sku B1 \
  --is-linux
```

### Step 4: Create Web App
```bash
az webapp create \
  --name school-ai-chatbot-backend-$(date +%s) \
  --resource-group school-ai-chatbot-rg-west \
  --plan school-ai-chatbot-plan \
  --runtime "DOTNETCORE:9.0"
```

### Step 5: Deploy Your Code
```bash
# Zip your application
cd SchoolAiChatbotBackend
zip -r ../app.zip .

# Deploy via Azure CLI
az webapp deployment source config-zip \
  --resource-group school-ai-chatbot-rg-west \
  --name your-app-name \
  --src ../app.zip
```

## Solution 3: Alternative Platforms (Immediate Deploy)

### Deploy to Railway (Free & Easy)
1. Visit https://railway.app
2. Connect your GitHub repository
3. Deploy with one click
4. No quota limitations

### Deploy to Render (Free Tier Available)  
1. Visit https://render.com
2. Connect GitHub repository
3. Select "Web Service"
4. Build & deploy automatically

### Deploy to Heroku
1. Install Heroku CLI
2. Create Heroku app
3. Deploy via Git push

## Solution 4: Azure Container Instances (If quota allows)
```bash
# Create container instance (lighter resource usage)
az container create \
  --resource-group school-ai-chatbot-rg \
  --name school-ai-chatbot \
  --image mcr.microsoft.com/dotnet/aspnet:9.0 \
  --dns-name-label school-ai-chatbot-$(date +%s) \
  --ports 80
```

## Recommended Next Steps:

1. **Try quota increase first** (usually approved instantly)
2. **If denied, try different Azure regions**
3. **If still failing, use Railway or Render for immediate deployment**
4. **Come back to Azure once quota is available**

## Your Application is Ready!
Your .NET 9 backend builds successfully and is deployment-ready. The only blocker is Azure quotas, which is a common issue with new subscriptions.

Would you like me to help you with any of these alternative approaches?