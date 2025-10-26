@description('Location for all resources')
param location string = resourceGroup().location

@description('Name of the application')
param appName string = 'school-ai-chatbot'

@description('Environment name')
param environmentName string

// Create unique suffix for resource names
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var appServicePlanName = 'plan-${resourceToken}'
var webAppName = 'app-${resourceToken}'

// App Service Plan - Basic tier (B1)
resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
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
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|9.0'
      appSettings: [
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'false'
        }
      ]
    }
    httpsOnly: true
  }
}

// Outputs
output BACKEND_URI string = 'https://${webApp.properties.defaultHostName}'
output AZURE_APP_SERVICE_NAME string = webApp.name