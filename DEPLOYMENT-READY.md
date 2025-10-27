# Quick Azure Deployment Commands

## Prerequisites
- Active Azure subscription
- Azure CLI logged in
- Azure Developer CLI (azd) installed ✅

## Deployment Steps

### 1. Sign in to Azure
```powershell
# Login to Azure CLI
az login

# Login to Azure Developer CLI  
azd auth login
```

### 2. Initialize Environment
```powershell
# Create new environment
azd env new school-ai-prod

# Or use existing environment
azd env select school-ai-prod
```

### 3. Set Required Environment Variables
```powershell
# Set OpenAI API Key (required)
azd env set OPENAI_API_KEY "your-openai-api-key-here"

# Set Pinecone configuration (required)
azd env set PINECONE_API_KEY "your-pinecone-api-key"
azd env set PINECONE_ENVIRONMENT "your-pinecone-environment"

# Set JWT secret (generate a secure 32+ character string)
azd env set JWT_SECRET_KEY "your-very-secure-jwt-secret-key-here"

# Optional: Set database connection string (if not using default)
azd env set CONNECTION_STRING "your-database-connection-string"
```

### 4. Deploy Everything
```powershell
# Deploy infrastructure and applications in one command
azd up
```

This will:
- Create all Azure resources (App Service, Database, Key Vault, etc.)
- Build and deploy your .NET backend
- Build and deploy your React frontend
- Configure all connections and settings

### 5. Verify Deployment
```powershell
# Check application logs
azd logs --service backend

# Get application URLs
azd env get-values | findstr URL

# Check deployment status
azd show
```

## Environment Variables You'll Need

### Required API Keys:
1. **OpenAI API Key**: Get from https://platform.openai.com/api-keys
2. **Pinecone API Key**: Get from https://app.pinecone.io/
3. **JWT Secret Key**: Generate a secure random string (32+ characters)

### Example:
```powershell
azd env set OPENAI_API_KEY "sk-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
azd env set PINECONE_API_KEY "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
azd env set PINECONE_ENVIRONMENT "us-east-1-aws"
azd env set JWT_SECRET_KEY "super-secure-jwt-secret-key-with-at-least-32-characters"
```

## Troubleshooting

If deployment fails:
```powershell
# Check logs
azd logs

# Check infrastructure status  
azd show

# Retry deployment
azd deploy
```

## Cost Estimate
- **Development**: ~$35/month
- **Production**: ~$100/month (with scaling)
- **Free Tier**: Some services available in Azure free tier