param location string
param acrName string
param aksName string
param keyVaultName string

// ---------- ACR (adopting bgdevopsacr2026) ----------
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
  }
}

// ---------- AKS (adopting eurojobagent-aks) ----------
resource aks 'Microsoft.ContainerService/managedClusters@2024-05-01' = {
  name: aksName
  location: location
  sku: {
    name: 'Base'
    tier: 'Free'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    dnsPrefix: 'eurojobage-devops-track-rg-7eeca9'
    kubernetesVersion: '1.35'
    nodeResourceGroup: 'MC_devops-track-rg_eurojobagent-aks_swedencentral'
    agentPoolProfiles: [
      {
        name: 'nodepool1'
        count: 1
        vmSize: 'Standard_B2s_v2'
        osType: 'Linux'
        mode: 'System'
      }
    ]
  }
}

// ---------- ACR pull role for AKS's kubelet identity ----------
var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'

resource acrPullAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, aks.id, acrPullRoleId)
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
    principalId: aks.properties.identityProfile.kubeletidentity.objectId
    principalType: 'ServicePrincipal'
  }
}

// ---------- Key Vault (NEW) ----------
resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enablePurgeProtection: false
    enableSoftDelete: true
  }
}

output acrLoginServer string = acr.properties.loginServer
output keyVaultUri string = kv.properties.vaultUri
