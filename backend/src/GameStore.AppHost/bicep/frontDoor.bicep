param location string

param storageBlobEndpoint string

resource frontDoorProfile 'Microsoft.Cdn/profiles@2025-04-15' = {
  location: location
  name: 'afd${uniqueString(resourceGroup().id)}'
  sku: {
    name: 'Standard_AzureFrontDoor'
  }
}

resource frontDoorEndpoint 'Microsoft.Cdn/profiles/afdEndpoints@2025-04-15' = {
  parent: frontDoorProfile
  location: location
  name: 'fde${uniqueString(resourceGroup().id)}'
  properties: {
    enabledState: 'Enabled'
  }
}

resource frontDoorOriginGroup 'Microsoft.Cdn/profiles/originGroups@2025-04-15' = {
  parent: frontDoorProfile
  name: 'default-origin-group'
  properties: {
    healthProbeSettings: {
      probePath: '/'
    }
    loadBalancingSettings: {
      sampleSize: 4
      successfulSamplesRequired: 3
    }
  }
}

resource frontDoorOrigin 'Microsoft.Cdn/profiles/originGroups/origins@2025-04-15' = {
  parent: frontDoorOriginGroup
  name: 'default-origin'
  properties: {
    hostName: replace(replace(storageBlobEndpoint, 'https://', ''), '/', '')
    originHostHeader: replace(replace(storageBlobEndpoint, 'https://', ''), '/', '')
  }
}

resource frontDoorRoute 'Microsoft.Cdn/profiles/afdEndpoints/routes@2025-04-15' = {
  parent: frontDoorEndpoint
  name: 'default-route'
  properties: {
    cacheConfiguration: {
      queryStringCachingBehavior: 'UseQueryString'
    }
    linkToDefaultDomain: 'Enabled'
    originGroup: {
      id: frontDoorOriginGroup.id
    }
  }
  dependsOn: [
    frontDoorOrigin
  ]
}

output frontDoorEndpointHostName string = frontDoorEndpoint.properties.hostName
