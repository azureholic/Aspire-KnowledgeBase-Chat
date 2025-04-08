param searchName string
param storageName string
param openaiName string
param containerAppManagedIdentityName string

//needed because Asspire generates a main that passes "location"
#disable-next-line no-unused-params
param location string = resourceGroup().location

var storageRoleDefinition = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
var openaiRoleDefinition = '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'

resource search 'Microsoft.Search/searchServices@2023-11-01' existing = {
  name: searchName
}

resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
  name: storageName
}

resource openai 'Microsoft.CognitiveServices/accounts@2024-10-01' existing = {
  name: openaiName
}

resource mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' existing = {
  name: containerAppManagedIdentityName
}


resource storage_roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(search.name, storageRoleDefinition, storage.id)
  scope: storage
  properties: {
    principalId: search.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageRoleDefinition)
    principalType: 'ServicePrincipal'
  }

}


resource openai_roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(search.name, openaiRoleDefinition, openai.id)
  scope: openai
  properties: {
    principalId: search.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', openaiRoleDefinition) 
    principalType: 'ServicePrincipal'
  }
}

resource mi_storage_roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(mi.id, storageRoleDefinition, storage.id)
  scope: storage
  properties: {
    principalId: mi.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageRoleDefinition)
    principalType: 'ServicePrincipal'
  }

}
