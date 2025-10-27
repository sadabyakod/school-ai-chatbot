# Manual Azure Deployment Guide

## Step 1: Create Resources Manually in Azure Portal

1. **Go to Azure Portal**: https://portal.azure.com
2. **Navigate to your Resource Group**: `school-ai-chatbot-rg`

### Create App Service Plan:
3. **Add Resource** → **App Service Plan**
   - Name: `school-ai-plan`
   - OS: `Linux`
   - Region: `Central US`
   - Pricing Tier: `Standard S1` (try this first)
   - Click **Create**

### Create Web App:
4. **Add Resource** → **Web App**
   - Name: `school-ai-backend` (or any unique name)
   - Runtime Stack: `.NET 9`
   - OS: `Linux`
   - Region: `Central US`
   - App Service Plan: Select the one created above
   - Click **Create**

## Step 2: Deploy Your Code

### Option A: Deploy from VS Code
1. Install **Azure App Service** extension in VS Code
2. Right-click on `SchoolAiChatbotBackend` folder
3. Select **Deploy to Web App**
4. Choose your newly created web app

### Option B: Deploy via ZIP
1. Build your project:
```powershell
cd SchoolAiChatbotBackend
dotnet publish -c Release -o ./publish
```

2. Zip the `publish` folder
3. Go to your Web App in Azure Portal
4. Go to **Advanced Tools** → **Kudu**
5. Navigate to `/site/wwwroot`
6. Upload and extract your ZIP file

## Step 3: Configure App Settings
In Azure Portal, go to your Web App → **Configuration** → **Application Settings**:

Add these settings:
- `ASPNETCORE_ENVIRONMENT` = `Production`
- `OPENAI_API_KEY` = `your-openai-key`
- `PINECONE_API_KEY` = `your-pinecone-key`
- `JWT_SECRET_KEY` = `your-jwt-secret`

## Step 4: Test
Your API will be available at: `https://your-app-name.azurewebsites.net`

## If Manual Creation Also Fails:
Your subscription truly has no quota. You'll need to:
1. Contact Azure Support
2. Or use a different Azure subscription
3. Or deploy to alternative platforms (Railway, Heroku, etc.)