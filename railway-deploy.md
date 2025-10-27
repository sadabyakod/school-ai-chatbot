# Railway Deployment - No Quota Limits!

railway.json:
```json
{
  "$schema": "https://railway.app/railway.schema.json",
  "build": {
    "builder": "DOCKERFILE"
  },
  "deploy": {
    "startCommand": "dotnet SchoolAiChatbotBackend.dll",
    "healthcheckPath": "/health"
  }
}
```

Deploy to Railway:
1. Install Railway CLI: `npm install -g @railway/cli`
2. Login: `railway login`
3. Deploy: `railway up`

Your app will be live in 2 minutes at https://your-app.railway.app