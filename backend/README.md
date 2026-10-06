# Game Store Backend
The .NET Backend for the Game Store application

## To create the Container Apps Environment
```powershell
az containerapp env create -n cae-gamestore-dev-eastus2-01 -g rg-gamestore-dev-eastus2-01 --location eastus2 --logs-destination none
```

## To create the Container App
```powershell
az containerapp create -n ca-gamestore-api-dev -g rg-gamestore-dev-eastus2-01 --environment cae-gamestore-dev-eastus2-01 --image <acr-name>.azurecr.io/gamestore-api:1.5 --registry-server <acr-name>.azurecr.io --registry-identity /subscriptions/<subscription-id>/resourcegroups/rg-gamestore-dev-eastus2-01/providers/Microsoft.ManagedIdentity/userAssignedIdentities/id-gamestore-dev-eastus2-01
```

## To update the container app
```powershell
$imageTag = "1.8"
$registry = "<acr-name>.azurecr.io"

dotnet publish /t:PublishContainer -p ContainerImageTag=$imageTag -p ContainerRegistry=$registry

az containerapp update -n ca-gamestore-api-dev -g rg-gamestore-dev-eastus2-01 --image $registry/gamestore-api:$imageTag
```

## To run load test using `hey` tool
$accessToken = "TOKEN HERE"
.\hey -n 100000 -c 10000 -H "Authorization: Bearer $accessToken" https://<container-app-fqdn>/diagnostics/nodeinfo