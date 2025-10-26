@description('Name of the application')
param appName string = 'school-ai-chatbot'

@description('Location for all resources')
param location string = 'East US'

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

// App Service Plan (Free Tier)
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'F1'
    tier: 'Free'
  }
  kind: 'app'
}

// App Service
resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: appServiceName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
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
          value: 'InMemory'
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

// Outputs
output appServiceName string = appService.name
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'