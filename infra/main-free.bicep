targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string = ''

// Optional parameters with defaults for free tier
@description('Location for OpenAI resource (free tier available in limited regions)')
@allowed(['eastus', 'southcentralus', 'westeurope'])
param openAiLocation string = 'eastus'

@description('SKU name for OpenAI (using free tier)')
param openAiSkuName string = 'F0'

@description('Model name for OpenAI deployment')
param openAiModelName string = 'gpt-3.5-turbo'

@description('Model version for deployment')
param openAiModelVersion string = '0613'

// Generate a unique token for resource names
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var prefix = '${environmentName}-${resourceToken}'
var tags = { 'azd-env-name': environmentName }

// Create resource group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'rg-${prefix}'
  location: location
  tags: tags
}

// Deploy main resources into the resource group
module resources 'main-free.resources.bicep' = {
  name: 'resources'
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    principalId: principalId
    resourceToken: resourceToken
    openAiLocation: openAiLocation
    openAiSkuName: openAiSkuName
    openAiModelName: openAiModelName
    openAiModelVersion: openAiModelVersion
  }
}

// Output important values
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_SUBSCRIPTION_ID string = subscription().subscriptionId
output AZURE_RESOURCE_GROUP_NAME string = resourceGroup.name

// Service-specific outputs
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output AZURE_CONTAINER_REGISTRY_NAME string = resources.outputs.AZURE_CONTAINER_REGISTRY_NAME
output SERVICE_WEB_IDENTITY_CLIENT_ID string = resources.outputs.SERVICE_WEB_IDENTITY_CLIENT_ID
output SERVICE_API_IDENTITY_CLIENT_ID string = resources.outputs.SERVICE_API_IDENTITY_CLIENT_ID
output AZURE_KEY_VAULT_NAME string = resources.outputs.AZURE_KEY_VAULT_NAME
output AZURE_OPENAI_ENDPOINT string = resources.outputs.AZURE_OPENAI_ENDPOINT
output AZURE_SQL_CONNECTION_STRING string = resources.outputs.AZURE_SQL_CONNECTION_STRING
output SERVICE_WEB_URI string = resources.outputs.SERVICE_WEB_URI
output SERVICE_API_URI string = resources.outputs.SERVICE_API_URI