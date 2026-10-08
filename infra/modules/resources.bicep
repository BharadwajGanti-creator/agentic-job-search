param location string
param acrName string
param aksName string
param keyVaultName string
param aksAdminGroupObjectId string

// ---------- ACR (adopting bgdevopsacr2026) ----------
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
    dataEndpointEnabled: false
    encryption: {
      status: 'disabled'
    }
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
    enableRBAC: true
    aadProfile: {
    managed: true
    enableAzureRBAC: true
    adminGroupObjectIDs: [
      aksAdminGroupObjectId
    ]
    tenantID: subscription().tenantId
  }
    supportPlan: 'KubernetesOfficial'
    autoUpgradeProfile: {
      nodeOSUpgradeChannel: 'NodeImage'
    }
    oidcIssuerProfile: {
      enabled: true
    }
    metricsProfile: {
      costAnalysis: {
        enabled: false
      }
    }
    storageProfile: {
      diskCSIDriver: { enabled: true }
      fileCSIDriver: { enabled: true }
      snapshotController: { enabled: true }
    }
    linuxProfile: {
      adminUsername: 'azureuser'
      ssh: {
        publicKeys: [
          {
            keyData: 'ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQC+b0X63eZs+3QDg3QAvg9vCTJLbhqPpJPuynkajnngORwEUTgbmejt3HQsbM3tCWX+nIAbRWit+8T+lGOUJCAtoG685YyRcbnHSyaMqCtChyTaBcPX+JPGz0zxTVWJqb0Tn4Ydg5xy9KD7BYM5btvRTTq6TD6rruhpd0RpoJNddOyrc2J3cSCOKd+YE6vLgHBfAroeG18ULmBOEYE3lUyr+qjT4oY7xcBuEGiXHc0ih+seUveTjZb5NiX8UgIaA7hQU7ePi5x8kYXn2zBX9L5YpJdGTno+eADSmAujwZxqDSi0L6x1jKvF67u0osehKBJw7tXrRjs2649aXn9nx2zp'
          }
        ]
      }
    }
    networkProfile: {
      networkPlugin: 'azure'
      networkPluginMode: 'overlay'
      networkDataplane: 'azure'
      networkPolicy: 'none'
      loadBalancerSku: 'standard'
      outboundType: 'loadBalancer'
      podCidr: '10.244.0.0/16'
      serviceCidr: '10.0.0.0/16'
      dnsServiceIP: '10.0.0.10'
      ipFamilies: ['IPv4']
    }
    agentPoolProfiles: [
      {
        name: 'nodepool1'
        count: 1
        vmSize: 'Standard_B2s_v2'
        osType: 'Linux'
        osSKU: 'Ubuntu'
        mode: 'System'
        maxPods: 250
        osDiskSizeGB: 128
        osDiskType: 'Managed'
        kubeletDiskType: 'OS'
        enableEncryptionAtHost: false
        enableFIPS: false
        enableNodePublicIP: false
        enableUltraSSD: false
        scaleDownMode: 'Delete'
        type: 'VirtualMachineScaleSets'
        upgradeSettings: {
          maxSurge: '10%'
        }
      }
    ]
  }
}

// ---------- ACR pull role for AKS's kubelet identity ----------
var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'

resource acrPullAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: '07e71c263a2443dabfc5d84ea094ed7a'
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
    enablePurgeProtection: true
    enableSoftDelete: true
  }
}

output acrLoginServer string = acr.properties.loginServer
output keyVaultUri string = kv.properties.vaultUri
