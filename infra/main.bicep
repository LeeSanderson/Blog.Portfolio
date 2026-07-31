targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the azd environment; used to derive resource names and the resource group name')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@secure()
@description('Resend API key used to send subscriber emails')
param resendApiKey string

@secure()
@description('Secret key used to HMAC-sign email-subscription confirm/unsubscribe links')
param emailSubscriptionSigningKey string

@description('The "from" address for subscriber emails; must be on a domain verified in Resend')
param emailSubscriptionFromAddress string = 'updates@sixsideddice.com'

var envName = toLower(environmentName)
var resourceToken = substring(toLower(uniqueString(subscription().id, environmentName, location)), 0, 8)
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
    envName: envName
  }
}

module storage 'storage.bicep' = {
  name: 'storage'
  scope: rg
  params: {
    location: location
    tags: tags
    envName: envName
    resourceToken: resourceToken
  }
}

module functionApp 'functionapp.bicep' = {
  name: 'functionapp'
  scope: rg
  params: {
    location: location
    tags: tags
    envName: envName
    resourceToken: resourceToken
    storageAccountName: storage.outputs.name
    deploymentContainerName: storage.outputs.deploymentContainerName
    applicationInsightsName: monitoring.outputs.name
    applicationInsightsConnectionString: monitoring.outputs.connectionString
    resendApiKey: resendApiKey
    emailSubscriptionSigningKey: emailSubscriptionSigningKey
    emailSubscriptionFromAddress: emailSubscriptionFromAddress
  }
}

output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_FUNCTION_NAME string = functionApp.outputs.name
output AZURE_FUNCTION_HOSTNAME string = functionApp.outputs.hostName
output APPLICATIONINSIGHTS_CONNECTION_STRING string = monitoring.outputs.connectionString
