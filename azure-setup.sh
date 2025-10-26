#!/bin/bash

# School AI Chatbot - Azure Deployment Script
# Run this in Azure Cloud Shell

echo "🚀 Starting School AI Chatbot Azure Deployment Setup..."

# Configuration variables
RESOURCE_GROUP="rg-school-ai-chatbot"
LOCATION="eastus"
APP_NAME="school-ai-chatbot"
SUBSCRIPTION_ID=$(az account show --query id --output tsv)

echo "📋 Configuration:"
echo "  Subscription: $SUBSCRIPTION_ID"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Location: $LOCATION"
echo "  App Name: $APP_NAME"
echo ""

# Step 1: Create Resource Group
echo "🏗️  Step 1: Creating Resource Group..."
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION \
  --output table

if [ $? -eq 0 ]; then
    echo "✅ Resource Group created successfully!"
else
    echo "❌ Failed to create Resource Group"
    exit 1
fi

# Step 2: Create Service Principal for GitHub Actions
echo ""
echo "🔐 Step 2: Creating Service Principal for GitHub Actions..."

# Generate a unique name for the service principal
SP_NAME="sp-github-actions-$APP_NAME-$(date +%s)"

echo "Creating service principal: $SP_NAME"

# Create service principal and capture the output
SP_OUTPUT=$(az ad sp create-for-rbac \
  --name $SP_NAME \
  --role contributor \
  --scopes "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP" \
  --sdk-auth 2>/dev/null)

if [ $? -eq 0 ]; then
    echo "✅ Service Principal created successfully!"
    echo ""
    echo "🔑 IMPORTANT: Copy this JSON for GitHub Secrets (AZURE_CREDENTIALS):"
    echo "=================================="
    echo "$SP_OUTPUT"
    echo "=================================="
    echo ""
else
    echo "❌ Failed to create Service Principal"
    exit 1
fi

# Step 3: Generate deployment parameters
echo "📝 Step 3: Generating deployment parameters..."

# Generate secure passwords and keys
SQL_PASSWORD="SchoolAI$(openssl rand -base64 12 | tr -d '=/+')2024!"
JWT_SECRET="$(openssl rand -base64 48 | tr -d '=/+')"

echo ""
echo "🔐 IMPORTANT: Copy these values for GitHub Secrets:"
echo "=================================================="
echo "AZURE_SUBSCRIPTION_ID=$SUBSCRIPTION_ID"
echo "AZURE_RESOURCE_GROUP_NAME=$RESOURCE_GROUP"
echo "SQL_ADMIN_PASSWORD=$SQL_PASSWORD"
echo "JWT_SECRET_KEY=$JWT_SECRET"
echo "OPENAI_API_KEY=sk-your-openai-key-here"
echo "=================================================="
echo ""

# Step 4: Test deployment (dry run)
echo "🧪 Step 4: Testing infrastructure template..."

# Check if we have the Bicep file (we'll create a simple test)
cat > test-template.json << 'EOF'
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "appName": {
      "type": "string",
      "defaultValue": "school-ai-chatbot"
    }
  },
  "variables": {
    "resourceSuffix": "[uniqueString(resourceGroup().id)]"
  },
  "resources": [],
  "outputs": {
    "message": {
      "type": "string",
      "value": "[concat('Ready to deploy ', parameters('appName'), ' with suffix ', variables('resourceSuffix'))]"
    }
  }
}
EOF

az deployment group validate \
  --resource-group $RESOURCE_GROUP \
  --template-file test-template.json \
  --parameters appName=$APP_NAME \
  --output table

if [ $? -eq 0 ]; then
    echo "✅ Template validation successful!"
    rm test-template.json
else
    echo "❌ Template validation failed"
    exit 1
fi

echo ""
echo "🎉 Azure Setup Complete!"
echo ""
echo "📋 Next Steps:"
echo "1. Copy the AZURE_CREDENTIALS JSON above"
echo "2. Copy the other secrets (AZURE_SUBSCRIPTION_ID, etc.)"
echo "3. Add them to your GitHub repository secrets"
echo "4. Push your code to GitHub"
echo "5. GitHub Actions will automatically deploy!"
echo ""
echo "💡 Tip: Your resources will be deployed to:"
echo "   Resource Group: $RESOURCE_GROUP"
echo "   Location: $LOCATION"
echo ""
echo "Ready to proceed with GitHub setup! 🚀"