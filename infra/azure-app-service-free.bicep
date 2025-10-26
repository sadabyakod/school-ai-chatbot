@description('Name of the application')
param appName string = 'school-ai-chatbot'

@description('Location for all resources')
param location string = 'West US 2'

@description('OpenAI API Key')
@secure()
param openAiApiKey string = ''

@description('JWT Secret Key')
@secure()
param jwtSecretKey string

// Variables
var resourceSuffix = uniqueString(resourceGroup().id)
var appServiceName = '${appName}-app-${resourceSuffix}'
var appServicePlanName = '${appName}-plan-${resourceSuffix}'

// App Service Plan (Free Tier - Try again)
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'F1'  // Free tier
    tier: 'Free'
    size: 'F1'
    family: 'F'
    capacity: 1
  }
  properties: {
    reserved: false  // Windows App Service Plan
  }
}

// App Service (Web App)
resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: appServiceName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v6.0'
      defaultDocuments: [
        'index.html'
      ]
      appSettings: [
        {
          name: 'OpenAI__ApiKey'
          value: openAiApiKey
        }
        {
          name: 'JWT__SecretKey'
          value: jwtSecretKey
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: 'InMemory'  // Use in-memory database instead
        }
      ]
      cors: {
        allowedOrigins: [
          '*'
        ]
      }
    }
  }
}

// Output the app service URL
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output appServiceName string = appService.name