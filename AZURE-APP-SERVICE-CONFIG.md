# Fix Azure App Service 503 Error

Your backend is returning 503 because it's missing required configuration. Here's how to fix it:

## Option 1: Using PowerShell Script (Recommended)

1. Wait for Azure CLI to finish installing
2. Restart your terminal
3. Run:
```powershell
cd c:\SmartStudyAI\school-ai-chatbot
.\configure-azure-app-settings.ps1
```

## Option 2: Manual Configuration via Azure Portal

### Step 1: Go to Azure Portal
1. Open https://portal.azure.com
2. Navigate to **App Services**
3. Click on **app-wlanqwy7vuwmu**

### Step 2: Configure Connection Strings
1. In the left menu, click **Configuration**
2. Click on **Connection strings** tab
3. Click **+ New connection string**
4. Add this:
   - **Name:** `DefaultConnection`
   - **Value:** `Server=school-chatbot-sql-10271900.database.windows.net;Database=school-ai-chatbot;User Id=schooladmin;Password=India@12345;Trusted_Connection=False;MultipleActiveResultSets=true;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;`
   - **Type:** `SQLAzure`
5. Click **OK**

### Step 3: Configure Application Settings
Still in **Configuration** > **Application settings** tab, add these settings:

| Name | Value |
|------|-------|
| `DatabaseProvider` | `SqlServer` |
| `OpenAI__ApiKey` | `YOUR_OPENAI_API_KEY_HERE` |
| `Jwt__Key` | `super-secure-jwt-secret-key-for-school-ai-chatbot-production-2024` |
| `Jwt__Issuer` | `SchoolAiChatbotBackend` |
| `Jwt__Audience` | `SchoolAiChatbotUsers` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

### Step 4: Save and Restart
1. Click **Save** at the top
2. Click **Continue** on the warning
3. Go to **Overview** in the left menu
4. Click **Restart** at the top
5. Click **Yes** to confirm

### Step 5: Verify
Wait 2-3 minutes, then check:
- Health endpoint: https://app-wlanqwy7vuwmu.azurewebsites.net/health
- Your frontend should now work!

## Common Issues

### Still getting 503?
1. Check **Log stream** in Azure Portal to see error messages
2. Verify all settings are entered correctly (watch for typos)
3. Make sure there are no extra spaces in values

### Database connection errors?
Verify the SQL Server firewall allows Azure services:
1. Go to **SQL Server** resource in Azure Portal
2. Click **Networking** (or **Firewalls and virtual networks**)
3. Ensure **Allow Azure services and resources to access this server** is ON

## Quick Test After Configuration

Run this in PowerShell:
```powershell
Invoke-WebRequest -Uri "https://app-wlanqwy7vuwmu.azurewebsites.net/health"
```

If you get a 200 response, it's working! 🎉
