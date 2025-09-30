param location string = resourceGroup().location
param environmentName string
param resourceToken string

// Load abbreviations for consistent naming
var abbrs = loadJsonContent('./abbreviations.json')
var tags = { 'azd-env-name': environmentName }

// Create Azure Storage Account for persistent data
resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: '${abbrs.storageStorageAccounts}${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
  }
}

// Create blob container for candidates data
resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2022-09-01' = {
  parent: storageAccount
  name: 'default'
}

resource candidatesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2022-09-01' = {
  parent: blobServices
  name: 'candidates-data'
  properties: {
    publicAccess: 'None'
  }
}

// App Service Plan for hosting the .NET application
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: '${abbrs.webServerFarms}${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'B1' // Basic tier suitable for development/testing
    capacity: 1
  }
  properties: {
    reserved: false // Windows App Service Plan
  }
}

// App Service for the HR MCP Server
resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: '${abbrs.webSitesAppService}${resourceToken}'
  location: location
  tags: union(tags, { 'azd-service-name': 'web' })
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      ftpsState: 'Disabled'
      netFrameworkVersion: 'v8.0' // .NET 8 runtime
      metadata: [
        {
          name: 'CURRENT_STACK'
          value: 'dotnet'
        }
      ]
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ASPNETCORE_URLS'
          value: 'http://*:80'
        }
        {
          name: 'HRMCPServer__CandidatesPath'
          value: './Data/candidates.json'
        }
        {
          name: 'Azure__StorageConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'Azure__BlobContainerName'
          value: 'candidates-data'
        }
        {
          name: 'SCM_DO_BUILD_DURING_DEPLOYMENT'
          value: 'true'
        }
        {
          name: 'ENABLE_ORYX_BUILD'
          value: 'true'
        }
        {
          name: 'WEBSITE_NODE_DEFAULT_VERSION'
          value: '~18'
        }
        {
          name: 'WEBSITE_DOTNET_DEFAULT_VERSION'
          value: '8.0'
        }
        {
          name: 'MSDEPLOY_RENAME_LOCKED_FILES'
          value: '1'
        }
      ]
      cors: {
        allowedOrigins: ['*']
        supportCredentials: false
      }
      webSocketsEnabled: true // Enable WebSockets for MCP if needed
    }
  }
}

// Output values needed by azd
output WEB_IDENTITY_PRINCIPAL_ID string = appService.identity.principalId
output WEB_NAME string = appService.name
output WEB_URI string = 'https://${appService.properties.defaultHostName}'