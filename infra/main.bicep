targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the azd environment; used to derive resource names and the resource group name')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = {
  'azd-env-name': environmentName
}

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module monitoring 'appinsights.bicep' = {
  name: 'monitoring'
  scope: rg
  params: {
    location: location
    tags: tags
    resourceToken: resourceToken
  }
}

module storage 'storage.bicep' = {
  name: 'storage'
  scope: rg
  params: {
    location: location
    tags: tags
    resourceToken: resourceToken
  }
}

module functionApp 'functionapp.bicep' = {
  name: 'functionapp'
  scope: rg
  params: {
    location: location
    tags: tags
    resourceToken: resourceToken
    storageAccountName: storage.outputs.name
    deploymentContainerName: storage.outputs.deploymentContainerName
    applicationInsightsName: monitoring.outputs.name
    applicationInsightsConnectionString: monitoring.outputs.connectionString
  }
}

output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_FUNCTION_NAME string = functionApp.outputs.name
output AZURE_FUNCTION_HOSTNAME string = functionApp.outputs.hostName
output APPLICATIONINSIGHTS_CONNECTION_STRING string = monitoring.outputs.connectionString
