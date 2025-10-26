@description('Location for all resources')
param location string = resourceGroup().location

@description('Name of the application')
param appName string = 'school-ai-chatbot'

@description('Environment name')
param environmentName string

// Create unique suffix for resource names
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var containerGroupName = 'cg-${resourceToken}'

// Container Group (Azure Container Instances)
resource containerGroup 'Microsoft.ContainerInstance/containerGroups@2023-05-01' = {
  name: containerGroupName
  location: location
  properties: {
    containers: [
      {
        name: 'school-ai-backend'
        properties: {
          image: 'mcr.microsoft.com/dotnet/samples:aspnetapp'
          ports: [
            {
              port: 80
              protocol: 'TCP'
            }
          ]
          resources: {
            requests: {
              cpu: 1
              memoryInGB: 1
            }
          }
        }
      }
    ]
    osType: 'Linux'
    ipAddress: {
      type: 'Public'
      ports: [
        {
          port: 80
          protocol: 'TCP'
        }
      ]
    }
  }
}

// Outputs
output BACKEND_URI string = 'http://${containerGroup.properties.ipAddress.ip}'
output CONTAINER_GROUP_NAME string = containerGroup.name