# Azure Database Schema Fix

## Problem
The application deployed successfully but is experiencing database schema mismatch errors:
```
Invalid column name 'FileId'
Invalid column name 'Chapter'
Invalid column name 'ChunkIndex'
Invalid column name 'CreatedAt'
Invalid column name 'Grade'
Invalid column name 'Subject'
```

## Root Cause
The Azure SQL database schema is outdated and doesn't have the latest Entity Framework migrations applied.

## Solution Options

### Option 1: Apply Entity Framework Migrations (Recommended)

#### Prerequisites
- Azure CLI installed and logged in (`az login`)
- .NET 8 SDK installed
- Your IP address allowed in Azure SQL firewall

#### Steps
1. Run the migration script:
   ```powershell
   cd c:\school-ai-chatbot
   .\apply-migrations-to-azure.ps1
   ```

This script will:
- Retrieve the connection string from Azure App Service
- Apply all pending migrations to Azure SQL
- Update the database schema

---

### Option 2: Manual SQL Script (Quick Fix)

If the automated migration fails, you can manually fix the schema:

#### Steps
1. Open Azure Portal → SQL Databases
2. Find your database and open Query Editor
3. Login with SQL authentication
4. Run the script from `fix-filechunks-schema.sql`

Or use Azure Data Studio / SQL Server Management Studio:
```sql
-- Copy and paste contents of fix-filechunks-schema.sql
```

---

### Option 3: Use Azure CLI with SQL Commands

```powershell
# Get database details
$resourceGroup = "DefaultResourceGroup-EUS"
$serverName = "your-sql-server-name"
$databaseName = "your-database-name"

# Run migration script
az sql db execute `
    --server $serverName `
    --name $databaseName `
    --resource-group $resourceGroup `
    --admin-user "your-admin" `
    --admin-password "your-password" `
    --file "fix-filechunks-schema.sql"
```

---

## Verification

After applying the fix, verify the deployment:

1. **Check Application Logs:**
   ```powershell
   az webapp log tail --name app-wlanqwy7vuwmu --resource-group DefaultResourceGroup-EUS
   ```

2. **Test API Endpoint:**
   ```powershell
   curl https://app-wlanqwy7vuwmu.azurewebsites.net/api/chat
   ```

3. **Check Database Schema:**
   ```sql
   SELECT COLUMN_NAME, DATA_TYPE 
   FROM INFORMATION_SCHEMA.COLUMNS 
   WHERE TABLE_NAME = 'FileChunks'
   ```

Expected columns:
- Id (int)
- FileId (int)
- ChunkText (nvarchar)
- ChunkIndex (int)
- Chapter (nvarchar)
- Grade (nvarchar)
- Subject (nvarchar)
- CreatedAt (datetime2)

---

## Migration Details

The following migrations need to be applied:
1. `20251028191833_InitialCreateForAzureSQL`
2. `20251115000000_RemovePineconeVectorId`
3. `20251118102207_UpdateForNet9`
4. `20251121015813_AddChatSessionFeatures`
5. `20251121081930_AddExamSystemEntities`
6. `20251121103000_UpdateUploadedFilesSchema` ← This one adds the missing columns

---

## Firewall Configuration

If you get connection errors, add your IP to Azure SQL firewall:

```powershell
# Get your public IP
$myIp = (Invoke-WebRequest -Uri "https://api.ipify.org").Content

# Add firewall rule
az sql server firewall-rule create `
    --resource-group DefaultResourceGroup-EUS `
    --server your-sql-server `
    --name "DevMachine" `
    --start-ip-address $myIp `
    --end-ip-address $myIp
```

Or in Azure Portal:
1. Go to SQL Server resource
2. Click "Networking" in left menu
3. Add your client IP address
4. Click "Save"

---

## Troubleshooting

### Error: "Cannot connect to database"
- Check firewall rules in Azure SQL
- Verify connection string in App Service configuration
- Ensure Azure SQL server allows Azure services

### Error: "Login failed for user"
- Verify SQL authentication credentials
- Check if user has proper permissions
- Try using Azure AD authentication

### Error: "Timeout expired"
- Database might be paused (DTU tier)
- Check Azure SQL performance tier
- Verify network connectivity

---

## Prevention

To prevent this in the future, add migration step to CI/CD:

### Update `.github/workflows/main_app-wlanqwy7vuwmu.yml`:

Add after deploy step:
```yaml
- name: Apply Database Migrations
  run: |
    dotnet tool install --global dotnet-ef
    cd SchoolAiChatbotBackend
    dotnet ef database update --connection "${{ secrets.AZURE_SQL_CONNECTION_STRING }}"
```

Then add the connection string as a GitHub secret:
1. Go to GitHub repo → Settings → Secrets
2. Add `AZURE_SQL_CONNECTION_STRING`
3. Paste your Azure SQL connection string

---

## Status

✅ Application deployed and running  
❌ Database schema outdated  
⏳ Waiting for migration to be applied  

Once migrations are applied, the API will work correctly.
