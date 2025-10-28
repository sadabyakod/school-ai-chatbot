# Azure MySQL Database Setup

## Required Environment Variables in Azure App Service

Add these environment variables in Azure Portal → App Services → app-wlanqwy7vuwmu → Settings → Environment variables:

### Database Connection
```
ConnectionStrings__DefaultConnection = Server=school-ai-mysql-server.mysql.database.azure.com;Database=flexibleserverdb;Uid=adminuser;Pwd=YOUR_MYSQL_PASSWORD;SslMode=Required;
```

### API Keys (Optional for full functionality)
```
OPENAI_API_KEY = your_openai_api_key_here
Pinecone__ApiKey = your_pinecone_api_key_here
Pinecone__Host = your_pinecone_host_url_here
Pinecone__IndexName = your_pinecone_index_name_here
```

## MySQL Server Details
- **Server**: school-ai-mysql-server.mysql.database.azure.com
- **Database**: flexibleserverdb
- **Username**: adminuser
- **SSL**: Required

## Notes
- Replace `YOUR_MYSQL_PASSWORD` with your actual MySQL password
- The application will now require a MySQL connection string to start
- In-memory database support has been completely removed