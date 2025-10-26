@description('Location for all resources')
param location string = resourceGroup().location

@description('Name of the application')
param appName string = 'school-ai-chatbot'

@description('Environment name')
param environmentName string

// Create unique suffix for resource names
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var webAppName = 'app-${resourceToken}'
var appServicePlanName = 'plan-${resourceToken}'

// Try Premium V2 instead of Basic (different quota pool)
resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'P1v2'
    tier: 'PremiumV2'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// Web App for Containers
resource webApp 'Microsoft.Web/sites@2022-03-01' = {
  name: webAppName
  location: location
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
          name: 'WEBSITES_PORT'
          value: '8080'
        }
      ]
    }
    httpsOnly: true
  }
}

// Outputs
output BACKEND_URI string = 'https://${webApp.properties.defaultHostName}'
output WEB_APP_NAME string = webApp.name