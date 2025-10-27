@description('Location for all resources')
param location string = resourceGroup().location

@description('Name of the application')
param appName string = 'school-ai-chatbot'

@description('Environment name')
param environmentName string

@description('JWT Secret Key')
param jwtSecretKey string

@description('OpenAI API Key')
param openAiApiKey string

@description('Pinecone API Key') 
param pineconeApiKey string

@description('Pinecone Environment')
param pineconeEnvironment string = 'us-east-1-aws'

// Create unique suffix for resource names
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var webAppName = 'app-${resourceToken}'
var appServicePlanName = 'plan-${resourceToken}'

// Try Standard tier (S1) - different quota pool
resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'S1'
    tier: 'Standard'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// Web App
resource webApp 'Microsoft.Web/sites@2022-03-01' = {
  name: webAppName
  location: location
  tags: {
    'azd-service-name': 'backend'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|9.0'
      appSettings: [
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'false'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'JWT__SecretKey'
          value: jwtSecretKey
        }
        {
          name: 'OpenAI__ApiKey'
          value: openAiApiKey
        }
        {
          name: 'Pinecone__ApiKey'
          value: pineconeApiKey
        }
        {
          name: 'Pinecone__Environment'
          value: pineconeEnvironment
        }
        {
          name: 'JWT__Issuer'
          value: 'SchoolAIChatbot'
        }
        {
          name: 'JWT__Audience'
          value: 'SchoolAIChatbot'
        }
      ]
    }
    httpsOnly: true
  }
}

// Outputs
output BACKEND_URI string = 'https://${webApp.properties.defaultHostName}'
output WEB_APP_NAME string = webApp.name