# 🚀 Ready to Deploy - Set Your API Keys First!

## Required API Keys Setup

Copy and paste these commands one by one, replacing the placeholder values with your actual API keys:

### 1. Set OpenAI API Key
```powershell
azd env set OPENAI_API_KEY "your-actual-openai-api-key-here"
```
**Get your key from:** https://platform.openai.com/api-keys

### 2. Set Pinecone Configuration  
```powershell
azd env set PINECONE_API_KEY "your-actual-pinecone-api-key"
azd env set PINECONE_ENVIRONMENT "your-pinecone-environment"
```
**Get your keys from:** https://app.pinecone.io/

### 3. Generate JWT Secret Key
```powershell
azd env set JWT_SECRET_KEY "your-super-secure-jwt-secret-key-minimum-32-characters-long"
```

### 4. Optional: Set Application Name
```powershell
azd env set APP_NAME "school-ai-chatbot"
```

## After Setting All Keys, Deploy:
```powershell
azd up
```

This will:
- ✅ Create all Azure resources (App Service, Database, Key Vault)
- ✅ Deploy your .NET backend API  
- ✅ Deploy your React frontend
- ✅ Configure all connections automatically

## Example with Sample Values (REPLACE WITH REAL KEYS):
```powershell
# ⚠️ REPLACE THESE WITH YOUR REAL API KEYS ⚠️
azd env set OPENAI_API_KEY "sk-1234567890abcdef..."
azd env set PINECONE_API_KEY "12345678-1234-1234-1234-123456789abc"  
azd env set PINECONE_ENVIRONMENT "us-east-1-aws"
azd env set JWT_SECRET_KEY "my-super-secure-jwt-secret-key-with-at-least-32-chars"

# Then deploy
azd up
```

## 🔐 Where to Get API Keys:

### OpenAI API Key:
1. Go to https://platform.openai.com/api-keys
2. Click "Create new secret key"
3. Copy the key (starts with "sk-")

### Pinecone API:
1. Go to https://app.pinecone.io/
2. Sign up/login
3. Go to API Keys section
4. Copy your API key and environment

### JWT Secret:
Generate a secure random string, example:
```
super-secure-jwt-secret-key-for-school-ai-chatbot-2024
```

## Ready? Run these commands now! 👇