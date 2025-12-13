# Configuration Files Setup

## Overview
This project uses `appsettings.json` for configuration, but the actual file with secrets is not committed to source control for security reasons.

## Files Structure

- **`appsettings.json`** - Local configuration file (ignored by git, contains real secrets)
- **`appsettings.template.json`** - Template file (committed to git, safe for public repos)

## Setup Instructions

### For Local Development

1. Copy the template file:
   ```bash
   cp SchoolAiChatbotBackend/appsettings.template.json SchoolAiChatbotBackend/appsettings.json
   ```

2. Update `appsettings.json` with your actual values:
   - Azure OpenAI endpoint and API key
   - Database connection string
   - Blob Storage connection string
   - Other API keys as needed

3. Never commit `appsettings.json` to source control!

### For CI/CD (GitHub Actions)

The workflow automatically creates `appsettings.json` from the template during deployment. The actual secrets are provided via:
- GitHub Secrets
- Azure App Service Configuration Settings
- Environment Variables

## Adding New Configuration

When adding new configuration settings:

1. Add the setting to **both** files:
   - `appsettings.json` (with real values)
   - `appsettings.template.json` (with placeholder values like `""` or `"YOUR_VALUE_HERE"`)

2. If it's a secret, add it to:
   - GitHub Secrets (for CI/CD)
   - Azure App Service Configuration (for production)

3. Update this README if the configuration is important

## Security Notes

- ✅ `appsettings.template.json` is safe to commit
- ❌ `appsettings.json` should NEVER be committed
- ✅ Use Azure Key Vault for production secrets
- ✅ Use User Secrets for local development (`dotnet user-secrets`)
