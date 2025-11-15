# Quick Deployment Checklist ✅

## Pre-Deployment (Local Testing)

- [ ] **Install Dependencies**
  ```powershell
  cd SchoolAiChatbotBackend
  dotnet restore
  dotnet build
  ```

- [ ] **Update appsettings.Development.json**
  ```json
  {
    "ConnectionStrings": {
      "SqlDb": "YOUR_LOCAL_OR_AZURE_SQL_CONNECTION"
    },
    "AzureOpenAI": {
      "Endpoint": "https://YOUR_RESOURCE.openai.azure.com/",
      "ApiKey": "YOUR_KEY",
      "ChatDeployment": "gpt-4",
      "EmbeddingDeployment": "text-embedding-3-small"
    },
    "AzureWebJobsStorage": "YOUR_STORAGE_CONNECTION",
    "USE_REAL_EMBEDDINGS": "true"
  }
  ```

- [ ] **Run Database Migration**
  ```powershell
  dotnet ef migrations add AddAzureFunctionsTables
  dotnet ef database update
  ```

- [ ] **Test Locally**
  ```powershell
  dotnet run
  # Open browser: http://localhost:8080/health
  ```

- [ ] **Test Endpoints**
  ```powershell
  # Chat
  Invoke-RestMethod -Uri "http://localhost:8080/api/chat" -Method POST -ContentType "application/json" -Body '{"question":"test","sessionId":"123"}'
  
  # Health
  Invoke-RestMethod -Uri "http://localhost:8080/health"
  ```

---

## Azure App Service Deployment

### Option 1: Azure CLI (Recommended)

- [ ] **Login to Azure**
  ```bash
  az login
  ```

- [ ] **Build Release Package**
  ```powershell
  cd SchoolAiChatbotBackend
  dotnet publish -c Release -o ./publish
  Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force
  ```

- [ ] **Deploy to App Service**
  ```bash
  az webapp deployment source config-zip \
    --resource-group YOUR_RESOURCE_GROUP \
    --name app-wlanqwy7vuwmu \
    --src ./publish.zip
  ```

### Option 2: VS Code Extension

- [ ] Install "Azure App Service" extension
- [ ] Right-click `SchoolAiChatbotBackend` folder
- [ ] Select "Deploy to Web App"
- [ ] Choose `app-wlanqwy7vuwmu`

### Option 3: GitHub Actions (CI/CD)

- [ ] Create `.github/workflows/deploy-backend.yml`
- [ ] Add `AZURE_WEBAPP_PUBLISH_PROFILE` secret
- [ ] Push to `main` branch → auto-deploys

---

## Azure App Service Configuration

- [ ] **Navigate to App Service**
  - Portal: https://portal.azure.com
  - Resource: `app-wlanqwy7vuwmu`

- [ ] **Set Environment Variables**
  
  Configuration → Application Settings → New:
  
  ```
  Name: ConnectionStrings__SqlDb
  Value: Server=tcp:YOUR_SERVER.database.windows.net,1433;Initial Catalog=YOUR_DB;User ID=YOUR_USER;Password=YOUR_PASS;Encrypt=True;
  
  Name: AzureWebJobsStorage
  Value: DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net
  
  Name: AzureOpenAI__Endpoint
  Value: https://YOUR_RESOURCE.openai.azure.com/
  
  Name: AzureOpenAI__ApiKey
  Value: YOUR_AZURE_OPENAI_KEY
  
  Name: AzureOpenAI__ChatDeployment
  Value: gpt-4
  
  Name: AzureOpenAI__EmbeddingDeployment
  Value: text-embedding-3-small
  
  Name: USE_REAL_EMBEDDINGS
  Value: true
  
  Name: Jwt__Key
  Value: YOUR_JWT_SECRET_KEY_MIN_32_CHARS
  
  Name: Jwt__Issuer
  Value: SchoolAiChatbotBackend
  
  Name: Jwt__Audience
  Value: SchoolAiChatbotUsers
  ```

- [ ] **Click Save** and **Restart** the app service

---

## Database Setup

- [ ] **Azure SQL Database**
  - Server: `YOUR_SERVER.database.windows.net`
  - Database: `YOUR_DATABASE`
  - Pricing: Basic/Standard (depends on usage)

- [ ] **Configure Firewall**
  - Add client IP for local access
  - Enable "Allow Azure services"

- [ ] **Run Migration SQL** (from `MIGRATION_EF_CORE.md`)
  ```sql
  -- Copy and execute the SQL script from MIGRATION_EF_CORE.md
  ```

- [ ] **Verify Tables Created**
  ```sql
  SELECT TABLE_NAME 
  FROM INFORMATION_SCHEMA.TABLES 
  WHERE TABLE_NAME IN ('FileChunks', 'ChunkEmbeddings', 'UploadedFiles', 'ChatHistories', 'StudyNotes');
  ```

---

## Azure Storage (Blob)

- [ ] **Create Storage Account** (if not exists)
  ```bash
  az storage account create \
    --name YOUR_STORAGE_ACCOUNT \
    --resource-group YOUR_RESOURCE_GROUP \
    --location eastus \
    --sku Standard_LRS
  ```

- [ ] **Create Blob Container**
  ```bash
  az storage container create \
    --name textbooks \
    --account-name YOUR_STORAGE_ACCOUNT \
    --public-access blob
  ```

- [ ] **Get Connection String**
  ```bash
  az storage account show-connection-string \
    --name YOUR_STORAGE_ACCOUNT \
    --resource-group YOUR_RESOURCE_GROUP
  ```

---

## Azure Functions (Ingestion Pipeline)

- [ ] **Create Function App** (if not exists)
  ```bash
  az functionapp create \
    --resource-group YOUR_RESOURCE_GROUP \
    --consumption-plan-location eastus \
    --runtime dotnet-isolated \
    --functions-version 4 \
    --name YOUR_FUNCTION_APP \
    --storage-account YOUR_STORAGE_ACCOUNT
  ```

- [ ] **Deploy Azure Functions**
  ```powershell
  cd api
  func azure functionapp publish YOUR_FUNCTION_APP
  ```

- [ ] **Configure Function App Settings**
  
  Same as App Service, add:
  - `SqlDb`
  - `AzureWebJobsStorage`
  - `AzureOpenAI:Endpoint`
  - `AzureOpenAI:ApiKey`
  - `AzureOpenAI:EmbeddingDeployment`
  - `USE_REAL_EMBEDDINGS`

---

## Post-Deployment Testing

- [ ] **Health Check**
  ```bash
  curl https://app-wlanqwy7vuwmu.azurewebsites.net/health
  ```
  
  Expected: `{"status":"healthy","timestamp":"...","database":"configured"}`

- [ ] **Chat Endpoint**
  ```bash
  curl -X POST https://app-wlanqwy7vuwmu.azurewebsites.net/api/chat \
    -H "Content-Type: application/json" \
    -d '{"question":"What is 2+2?","sessionId":"test-123"}'
  ```

- [ ] **File Upload**
  ```bash
  curl -X POST https://app-wlanqwy7vuwmu.azurewebsites.net/api/file/upload \
    -F "file=@test.txt" \
    -F "subject=Test" \
    -F "grade=Grade 1"
  ```

- [ ] **Check Logs**
  - Azure Portal → App Service → Log Stream
  - Look for: "RAG answer generated", "File uploaded successfully"

- [ ] **Verify Database**
  ```sql
  SELECT COUNT(*) FROM FileChunks;
  SELECT COUNT(*) FROM ChunkEmbeddings;
  SELECT COUNT(*) FROM ChatHistories;
  ```

---

## Frontend Update

- [ ] **Update API Base URL** in frontend
  
  `school-ai-frontend/src/config.js`:
  ```javascript
  export const API_BASE_URL = 'https://app-wlanqwy7vuwmu.azurewebsites.net';
  ```

- [ ] **Test Frontend → Backend Connection**
  - Open frontend
  - Send a chat message
  - Check browser console for errors
  - Verify response received

- [ ] **Deploy Frontend** (if using Static Web Apps)
  ```bash
  cd school-ai-frontend
  npm run build
  az staticwebapp upload \
    --name YOUR_SWA_NAME \
    --resource-group YOUR_RESOURCE_GROUP \
    --source ./dist
  ```

---

## Monitoring & Observability

- [ ] **Enable Application Insights**
  ```bash
  az monitor app-insights component create \
    --app YOUR_APP_INSIGHTS \
    --location eastus \
    --resource-group YOUR_RESOURCE_GROUP
  ```

- [ ] **Link to App Service**
  - Portal → App Service → Application Insights → Enable
  - Copy Instrumentation Key
  - Add to App Settings: `APPLICATIONINSIGHTS_CONNECTION_STRING`

- [ ] **Check Metrics**
  - Response times
  - Request counts
  - Failed requests
  - Database query performance

---

## Security Checklist

- [ ] **Enable HTTPS Only**
  - Portal → App Service → TLS/SSL settings → HTTPS Only: ON

- [ ] **Configure CORS**
  - Already configured in `Program.cs`
  - Update if needed for production domain

- [ ] **Rotate Secrets**
  - Use Azure Key Vault for sensitive keys
  - Rotate JWT secret periodically
  - Rotate OpenAI API keys

- [ ] **Enable Managed Identity**
  ```bash
  az webapp identity assign \
    --name app-wlanqwy7vuwmu \
    --resource-group YOUR_RESOURCE_GROUP
  ```

---

## Performance Optimization

- [ ] **Add Connection Pooling** (already enabled in EF Core)

- [ ] **Enable Caching** for frequent queries
  ```csharp
  services.AddMemoryCache();
  ```

- [ ] **Index Database Tables** (already done in migration SQL)

- [ ] **Enable Azure CDN** for static assets

- [ ] **Configure Auto-Scaling**
  - Portal → App Service → Scale out (App Service plan)
  - Set rules based on CPU/Memory usage

---

## Backup & Disaster Recovery

- [ ] **Enable Database Backups**
  - Portal → SQL Database → Automated backups
  - Set retention period (7-35 days)

- [ ] **Backup Blob Storage**
  - Enable soft delete
  - Configure geo-redundancy

- [ ] **Document Restore Procedure**
  ```bash
  # Restore database to point in time
  az sql db restore \
    --resource-group YOUR_RESOURCE_GROUP \
    --server YOUR_SERVER \
    --name YOUR_DB_RESTORED \
    --source-database YOUR_DB \
    --time "2024-01-15T10:00:00Z"
  ```

---

## Final Verification

- [ ] ✅ Backend deployed and running
- [ ] ✅ Database migrated with new tables
- [ ] ✅ Configuration variables set
- [ ] ✅ All endpoints tested successfully
- [ ] ✅ Azure Functions processing files
- [ ] ✅ Frontend connected to backend
- [ ] ✅ Monitoring enabled
- [ ] ✅ Backups configured

---

## Rollback Plan (If Something Goes Wrong)

1. **Redeploy Previous Version**
   ```bash
   az webapp deployment source config-zip \
     --name app-wlanqwy7vuwmu \
     --resource-group YOUR_RESOURCE_GROUP \
     --src ./previous-version.zip
   ```

2. **Restore Database**
   ```bash
   az sql db restore --time "BEFORE_MIGRATION_TIME"
   ```

3. **Revert Configuration**
   - Save current config before deployment
   - Restore from backup if needed

---

## Support Resources

- **Azure Documentation**: https://docs.microsoft.com/azure
- **EF Core Docs**: https://docs.microsoft.com/ef/core
- **OpenAI API Docs**: https://platform.openai.com/docs
- **Azure OpenAI Docs**: https://learn.microsoft.com/azure/ai-services/openai/

---

**Deployment Complete! Your School AI Chatbot is now running on Azure with SQL-based RAG.** 🚀
