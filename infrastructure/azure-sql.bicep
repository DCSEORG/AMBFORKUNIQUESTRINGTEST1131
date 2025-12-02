// Azure SQL Database Bicep template
// Uses Entra ID-only authentication (no SQL authentication)

@description('Location for all resources')
param location string = 'uksouth'

@description('Base name for resources')
param baseName string = 'expensemgmt'

@description('Unique suffix for resource names')
param uniqueSuffix string = uniqueString(resourceGroup().id)

@description('Entra ID admin login name')
param adminLogin string

@description('Entra ID admin object ID')
param adminObjectId string

@description('Managed Identity Principal ID for database access')
param managedIdentityPrincipalId string

// Generate lowercase names
var sqlServerName = toLower('sql-${baseName}-${uniqueSuffix}')
var databaseName = 'Northwind'

// SQL Server with Entra ID-only authentication
resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'User'
      login: adminLogin
      sid: adminObjectId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
  }
}

// SQL Database - Basic tier for development
resource database 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
  }
}

// Firewall rule to allow Azure services
resource firewallAzureServices 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAllAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Outputs
output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = database.name
