targetScope = 'subscription'

@description('Must match the EXISTING resource group location exactly. Immutable post-creation — do not "fix" the stale value.')
param rgLocation string = 'uksouth'

@description('Real Azure region every actual resource lives in.')
param resourceLocation string = 'swedencentral'

@description('Microsoft Entra group object ID used for AKS administrative access.')
param aksAdminGroupObjectId string

param acrName string = 'bgdevopsacr2026'
param aksName string = 'eurojobagent-aks'
param keyVaultName string = 'devops-track-kv2026'   // check global availability before first apply

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'devops-track-rg'
  location: rgLocation
}

module resources 'modules/resources.bicep' = {
  name: 'resources-deployment'
  scope: rg
  params: {
    location: resourceLocation
    acrName: acrName
    aksName: aksName
    keyVaultName: keyVaultName
    aksAdminGroupObjectId: aksAdminGroupObjectId
  }
}

output aksName string = aksName
output acrLoginServer string = resources.outputs.acrLoginServer
output keyVaultUri string = resources.outputs.keyVaultUri

