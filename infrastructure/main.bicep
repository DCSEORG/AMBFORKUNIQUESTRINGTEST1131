// Main Bicep template for Expense Management System
// Orchestrates deployment of all Azure resources

@description('Location for main resources')
param location string = 'uksouth'

@description('Base name for resources')
param baseName string = 'expensemgmt'

@description('Entra ID admin login name for SQL Server')
param adminLogin string

@description('Entra ID admin object ID for SQL Server')
param adminObjectId string

@description('Deploy GenAI resources (Azure OpenAI and AI Search)')
param deployGenAI bool = false

// Generate unique suffix using resource group ID
var uniqueSuffix = uniqueString(resourceGroup().id)

// App Service module (includes Managed Identity)
module appService 'app-service.bicep' = {
  name: 'appServiceDeployment'
  params: {
    location: location
    baseName: baseName
    uniqueSuffix: uniqueSuffix
  }
}

// Azure SQL module
module azureSql 'azure-sql.bicep' = {
  name: 'azureSqlDeployment'
  params: {
    location: location
    baseName: baseName
    uniqueSuffix: uniqueSuffix
    adminLogin: adminLogin
    adminObjectId: adminObjectId
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// GenAI module (conditionally deployed)
module genAI 'genai.bicep' = if (deployGenAI) {
  name: 'genAIDeployment'
  params: {
    location: 'swedencentral' // GPT-4o availability
    baseName: baseName
    uniqueSuffix: uniqueSuffix
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Outputs
output webAppName string = appService.outputs.webAppName
output webAppHostName string = appService.outputs.webAppHostName
output webAppUrl string = 'https://${appService.outputs.webAppHostName}'
output managedIdentityId string = appService.outputs.managedIdentityId
output managedIdentityPrincipalId string = appService.outputs.managedIdentityPrincipalId
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output managedIdentityName string = appService.outputs.managedIdentityName
output sqlServerName string = azureSql.outputs.sqlServerName
output sqlServerFqdn string = azureSql.outputs.sqlServerFqdn
output databaseName string = azureSql.outputs.databaseName

// Conditional GenAI outputs
output openAIEndpoint string = deployGenAI ? genAI.outputs.openAIEndpoint : ''
output openAIName string = deployGenAI ? genAI.outputs.openAIName : ''
output openAIModelName string = deployGenAI ? genAI.outputs.openAIModelName : ''
output searchEndpoint string = deployGenAI ? genAI.outputs.searchEndpoint : ''
output searchName string = deployGenAI ? genAI.outputs.searchName : ''
