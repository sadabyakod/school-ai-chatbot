# Azure SQL Server Setup for School AI Chatbot

## Database Details
- **Server Name**: `school-chatbot-sql-10271900.database.windows.net`
- **Resource Group**: `school-ai-chatbot-rg`
- **Location**: Central US
- **Pricing Tier**: Basic
- **Database Name**: `school-ai-chatbot` (recommended)

## Connection String Configuration

### For Azure App Service Environment Variables
Add the following environment variable in your Azure App Service Configuration:

**Name**: `ConnectionStrings__DefaultConnection`
**Value**: 
```
Server=school-chatbot-sql-10271900.database.windows.net;Database=school-ai-chatbot;User Id=YOUR_SQL_USERNAME;Password=YOUR_SQL_PASSWORD;Trusted_Connection=False;MultipleActiveResultSets=true;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

Replace:
- `YOUR_SQL_USERNAME` with your Azure SQL admin username
- `YOUR_SQL_PASSWORD` with your Azure SQL admin password
- `school-ai-chatbot` with your actual database name if different

### For Local Development (appsettings.Development.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=school-chatbot-sql-10271900.database.windows.net;Database=school-ai-chatbot;User Id=YOUR_SQL_USERNAME;Password=YOUR_SQL_PASSWORD;Trusted_Connection=False;MultipleActiveResultSets=true;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

## Database Migration Commands

### Apply Migrations to Azure SQL Server
```bash
# Set connection string as environment variable (replace with actual credentials)
$env:ConnectionStrings__DefaultConnection="Server=school-chatbot-sql-10271900.database.windows.net;Database=school-ai-chatbot;User Id=YOUR_USERNAME;Password=YOUR_PASSWORD;Trusted_Connection=False;MultipleActiveResultSets=true;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# Apply migrations
dotnet ef database update --context AppDbContext --verbose
```

### Generate SQL Script (Alternative)
If you prefer to run SQL scripts manually in Azure portal:
```bash
dotnet ef migrations script --context AppDbContext --output azure-sql-setup.sql
```

## Firewall Configuration
Make sure to configure the Azure SQL Server firewall to allow:
1. Azure services and resources to access this server
2. Your IP address (for local development)
3. Azure App Service IP ranges (if needed)

## Verification Steps

### 1. Test Connection Locally
```bash
cd SchoolAiChatbotBackend
dotnet run --environment Development
```

### 2. Check Tables in Azure Portal
After successful migration, verify these tables exist in your Azure SQL database:
- Schools
- Faqs  
- UploadedFiles
- SyllabusChunks
- __EFMigrationsHistory

### 3. Test API Endpoints
- GET `/health` - Should return healthy status
- GET `/api/schools` - Should return empty array initially
- POST `/api/upload` - Should work with file uploads

## Troubleshooting

### Common Issues:
1. **Login failed for user**: Check username/password in connection string
2. **Cannot open database**: Ensure database name exists in Azure SQL
3. **Firewall blocking**: Add your IP to Azure SQL firewall rules
4. **SSL/TLS errors**: Ensure `Encrypt=True;TrustServerCertificate=False` in connection string

### Enable Detailed Logging:
Add to appsettings.json:
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```