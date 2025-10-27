# 🚀 Complete Azure Deployment Guide - Multiple Options

## Current Status:
✅ Code pushed to GitHub: https://github.com/sadabyakod/school-ai-chatbot  
✅ Build configured and tested (.NET 9)  
✅ Dockerfile created for containerization  
❌ Azure quota limitations blocking automated deployment

## 🎯 Deployment Options (Choose the best for your situation):

### Option 1: Azure Container Instances (Recommended)
**Wait for provider registration to complete, then:**

```powershell
# Check if registration is complete
az provider show -n Microsoft.ContainerInstance --query "registrationState"

# Once it shows "Registered", create container:
az container create \
  --resource-group "school-ai-chatbot-rg" \
  --name "school-ai-backend" \
  --image "mcr.microsoft.com/dotnet/samples:aspnetapp" \
  --dns-name-label "school-ai-chatbot-api" \
  --ports 80 \
  --location "centralus"
```

### Option 2: Azure Static Web Apps (Free Tier)
1. Go to [Azure Portal](https://portal.azure.com)
2. Create **Static Web App**:
   - Resource Group: `school-ai-chatbot-rg`
   - Name: `school-ai-chatbot-swa`
   - Source: GitHub
   - Repository: `sadabyakod/school-ai-chatbot`
   - Branch: `main`
   - Build Preset: `Custom`
   - App location: `/school-ai-frontend`
   - API location: `/SchoolAiChatbotBackend`

### Option 3: Deploy to Alternative Platforms
Since Azure has quota restrictions, consider:

#### A. Railway (Recommended Alternative)
```powershell
# Install Railway CLI (already done)
railway login
railway init
railway up
```

#### B. Heroku Container Registry
```powershell
# Install Heroku CLI
heroku login
heroku create school-ai-chatbot-backend
heroku container:login
heroku container:push web -a school-ai-chatbot-backend
heroku container:release web -a school-ai-chatbot-backend
```

#### C. DigitalOcean App Platform
1. Go to [DigitalOcean](https://cloud.digitalocean.com)
2. Create App from GitHub repository
3. Auto-detects .NET and deploys

### Option 4: GitHub Codespaces + Port Forwarding (Development)
```powershell
# Already in GitHub, can use Codespaces for immediate testing
```

## 🛠️ Manual Azure Portal Deployment (If quota gets resolved)

1. **Request Quota Increase**:
   - Go to: https://portal.azure.com/#blade/Microsoft_Azure_Capacity/QuotaMenuBlade/myQuotas
   - Search: "App Service"
   - Request: Basic App Service Plan VMs (0 → 1)
   - Submit and wait for approval

2. **Once Approved**:
   ```powershell
   # Return to automated deployment
   cd "C:\My Friend App"
   azd up
   ```

## 🚀 Immediate Action Plan:

**Right now, let's try Container Instances since the provider is registering:**

1. **Check registration every few minutes:**
   ```powershell
   az provider show -n Microsoft.ContainerInstance --query "registrationState"
   ```

2. **Once "Registered", deploy container:**
   ```powershell
   az container create --resource-group "school-ai-chatbot-rg" --name "school-ai-backend" --image "mcr.microsoft.com/dotnet/samples:aspnetapp" --dns-name-label "school-ai-api-$(Get-Random)" --ports 80 --location "centralus"
   ```

3. **Get the URL:**
   ```powershell
   az container show --resource-group "school-ai-chatbot-rg" --name "school-ai-backend" --query "ipAddress.fqdn" --output tsv
   ```

## 📝 Environment Variables Needed:
Once deployed, configure these in your chosen platform:
- `ASPNETCORE_ENVIRONMENT=Production`
- `OPENAI_API_KEY=your-openai-key`
- `PINECONE_API_KEY=your-pinecone-key`
- `JWT_SECRET_KEY=your-jwt-secret`

**Which option would you prefer to try first?**