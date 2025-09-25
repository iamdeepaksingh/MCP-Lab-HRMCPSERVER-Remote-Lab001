targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Name of the resource group. If empty, a name will be generated.')
param resourceGroupName string = ''

// Generate a unique suffix for resource names
var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }

// Generate resource group name if not provided
var finalResourceGroupName = !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourcesResourceGroups}${environmentName}'

// Create resource group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: finalResourceGroupName
  location: location
  tags: tags
}

// Set metadata for the subscription deployment
metadata description = 'HR MCP Server deployment to Azure'
metadata name = 'hr-mcp-server'

// Deploy the main resources
module resources 'resources.bicep' = {
  name: 'resources'
  scope: rg
  params: {
    location: location
    environmentName: environmentName
    resourceToken: resourceToken
  }
}

// Output important values for azd
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_RESOURCE_GROUP string = rg.name

// Output the app service details
output SERVICE_WEB_IDENTITY_PRINCIPAL_ID string = resources.outputs.WEB_IDENTITY_PRINCIPAL_ID
output SERVICE_WEB_NAME string = resources.outputs.WEB_NAME
output SERVICE_WEB_URI string = resources.outputs.WEB_URI