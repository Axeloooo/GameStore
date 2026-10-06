# Runbook: Containers & Aspire (deferred cloud chapters)

Order: Local run, then cloud steps 0-9. Bash/zsh (course README uses PowerShell variable syntax; converted here). Replace every `<placeholder>`; never commit real values.
Backend: `backend/src/GameStore.Api` (image repository `gamestore-api`), AppHost `backend/src/GameStore.AppHost`. Frontend: `frontend/GameStore.Frontend`, AppHost `frontend/GameStore.Frontend.AppHost`.
Prereqs: Docker running, .NET 8 SDK + Aspire workload/templates, `az`, `azd`, Node 22. Reuses the Entra external tenant, API app registration and SPA registration from the Azure for .NET Developers runbook (`<entra-api-client-id>`, `<entra-tenant-id>`, `<entra-spa-client-id>`).

## Local run
```bash
dotnet run --project backend/src/GameStore.AppHost --launch-profile http
```
- Aspire dashboard http://localhost:15054; API http://localhost:5082; pgAdmin http://localhost:5050; Keycloak http://localhost:8080 (run mode only); Postgres 5432; Azurite blob 10000.
- Health: `/health/ready` and `/health/alive` are restricted by `RequireHost("*:8081","localhost:5082")`, so call them via the local API host: `curl http://localhost:5082/health/ready` and `curl http://localhost:5082/health/alive`. In Azure the probes hit port 8081 (`HTTP_PORTS=8080;8081`).
- Local parameters come from `backend/src/GameStore.AppHost/appsettings.Development.json` (or `appsettings.json`) under `Parameters`: `EntraValidAudience` = `<entra-api-client-id>`, `EntraAuthority` = `https://<entra-tenant-id>.ciamlogin.com/<entra-tenant-id>/v2.0`. Use user-secrets for real values: `dotnet user-secrets set "Parameters:EntraValidAudience" "<entra-api-client-id>" --project backend/src/GameStore.AppHost`.
- Frontend (separate terminal):
```bash
cd frontend/GameStore.Frontend
npm install
npm run dev     # http://localhost:5173
```
- Persistent containers: Postgres, pgAdmin, Azurite, Keycloak use `ContainerLifetime.Persistent`, so they keep running after the AppHost stops (and keep their data volumes). Remove them:
```bash
docker ps --filter "name=postgres" --filter "name=pgadmin" --filter "name=storage" --filter "name=keycloak"
docker rm -f <container-id> ...
docker volume ls    # then: docker volume rm <volume-name>   (wipes DB/blob/Keycloak data)
```

## 0. Login and variables
```bash
az login --tenant <tenant-id>
az account set --subscription <subscription-id>
az provider register -n Microsoft.ContainerRegistry -n Microsoft.App -n Microsoft.OperationalInsights -n Microsoft.ManagedIdentity -n Microsoft.DBforPostgreSQL -n Microsoft.Storage -n Microsoft.Cdn
az group create -n <resource-group> -l <location>
```
Why: providers must be registered before ACR/Container Apps/azd can create resources. (Provider list is inferred, not in the handouts.)

## 1. Azure Container Registry
Why: holds the `gamestore-api` image Container Apps pulls.
```bash
az acr create -g <resource-group> -n <acr-name> --sku Basic
az acr login -n <acr-name>
```
Produces `<acr-name>.azurecr.io` (`<registry>`), consumed by steps 2-4.

## 2. Managed identity with AcrPull
Why: Container App pulls from ACR with no admin password.
```bash
az identity create -g <resource-group> -n <identity-name>
az role assignment create --assignee <identity-principal-id> --assignee-principal-type ServicePrincipal --role AcrPull --scope $(az acr show -n <acr-name> --query id -o tsv)
```
`<identity-resource-id>` = `/subscriptions/<subscription-id>/resourcegroups/<resource-group>/providers/Microsoft.ManagedIdentity/userAssignedIdentities/<identity-name>`. Consumed by `--registry-identity` in step 4.

## 3. Publish the API image to ACR
Why: .NET SDK container support builds and pushes without a Dockerfile (`ContainerRepository` = `gamestore-api` in GameStore.Api.csproj).
```bash
IMAGE_TAG=1.0
REGISTRY=<acr-name>.azurecr.io
cd backend/src/GameStore.Api
dotnet publish /t:PublishContainer -p ContainerImageTag=$IMAGE_TAG -p ContainerRegistry=$REGISTRY
```
Needs the `az acr login` from step 1 (or Docker credentials for the registry).

## 4. Azure Container Apps (manual path from README)
Why: runs the image serverless; env first, then app.
```bash
az containerapp env create -n <cae-name> -g <resource-group> --location <location> --logs-destination none
az containerapp create -n <container-app-name> -g <resource-group> --environment <cae-name> \
  --image <acr-name>.azurecr.io/gamestore-api:$IMAGE_TAG \
  --registry-server <acr-name>.azurecr.io \
  --registry-identity <identity-resource-id>
az containerapp show -n <container-app-name> -g <resource-group> --query properties.configuration.ingress.fqdn -o tsv
```
Produces `<container-app-fqdn>`. Note: the README command sets no ingress, target port or app settings; to reach the API add `--ingress external --target-port 8080` and `--env-vars Authentication__Schemes__Entra__ValidAudience=<entra-api-client-id> Authentication__Schemes__Entra__Authority=https://<entra-tenant-id>.ciamlogin.com/<entra-tenant-id>/v2.0 AllowedOrigins=<frontend-origin> ConnectionStrings__GameStoreDB=<postgres-connection> ConnectionStrings__Blobs=https://<storage-account>.blob.core.windows.net` (flags are my addition, not in the handouts; the azd path below does all of this for you).

Update to a new version:
```bash
IMAGE_TAG=1.1
dotnet publish /t:PublishContainer -p ContainerImageTag=$IMAGE_TAG -p ContainerRegistry=$REGISTRY
az containerapp update -n <container-app-name> -g <resource-group> --image $REGISTRY/gamestore-api:$IMAGE_TAG
```

## 5. Load test (README `hey`)
Why: shows scale-out (AppHost sets min 0, max 10 replicas in the azd path).
```bash
TOKEN=<access-token>     # acquire from the SPA / MSAL for <entra-api-client-id>
./hey -n 100000 -c 10000 -H "Authorization: Bearer $TOKEN" https://<container-app-fqdn>/diagnostics/nodeinfo
az containerapp replica list -n <container-app-name> -g <resource-group> -o table
```

## 6. Aspire IaC with azd (backend)
Why: the AppHost already models Postgres Flexible Server, Storage, Container App, probes and Front Door; azd turns it into Bicep and deploys it.
Files: `backend/azure.yaml` (one service `app`, `host: containerapp`, `project: ./src/GameStore.AppHost/GameStore.AppHost.csproj`); `backend/next-steps.md` explains azd.
```bash
cd backend
azd auth login --tenant-id <tenant-id>
azd env new <azd-env-name>
azd env set AZURE_SUBSCRIPTION_ID <subscription-id>
azd env set AZURE_LOCATION <location>
azd env set AZURE_RESOURCE_GROUP <resource-group>   # optional
azd up
```
`azd up` prompts for the AppHost parameters (not secrets here) and stores them in `backend/.azure/<azd-env-name>/config.json`:
```json
{
  "infra": {
    "parameters": {
      "AllowedOrigins": "https://<frontend-container-app-fqdn>",
      "EntraAuthority": "https://<entra-tenant-id>.ciamlogin.com/<entra-tenant-id>/v2.0",
      "EntraValidAudience": "<entra-api-client-id>"
    }
  }
}
```
Consumed in `AppHost.cs`: `EntraValidAudience` -> `Authentication__Schemes__Entra__ValidAudience`; `EntraAuthority` -> `Authentication__Schemes__Entra__Authority`; `AllowedOrigins` -> `AllowedOrigins` (CORS, publish mode only). Set them without prompts: `azd env set` is not used here; edit config.json or run `azd provision` and answer prompts. Chicken-and-egg: `AllowedOrigins` needs the frontend FQDN (step 8); first run with a placeholder origin such as `http://localhost:5173`, then update and `azd deploy`.
Publish-mode extras produced: Front Door hostname -> `AZURE_FRONTDOOR_HOSTNAME` on the API; storage `AllowBlobPublicAccess=true`; probes on 8081 (`/health/alive`, `/health/ready`).
Inspect/persist IaC: `azd config set alpha.infraSynth on` then `azd infra synth` (per next-steps.md); `azd show`; CI/CD `azd pipeline config -e <azd-env-name>`; clean up `azd down`.
Output: `<container-app-fqdn>` (API endpoint printed by azd).

## 7. PostgreSQL Flexible Server and Storage (what azd creates)
`AddAzurePostgresFlexibleServer("postgres")` (database `GameStoreDB`/`gamestore`) and `AddAzureStorage("storage")` run as containers locally (`RunAsContainer`/`RunAsEmulator`) and publish as real Azure resources. Aspire injects `ConnectionStrings__GameStoreDB` and `ConnectionStrings__Blobs` into the API; no manual step. Verify: portal > `<resource-group>` shows the Postgres flexible server and storage account. Upload seed images to the blob container if needed (see the previous runbook step 2; role "Storage Blob Data Contributor" for you). Whether the API's identity gets Postgres/Storage roles automatically is generated by Aspire; confirm in the portal (not verifiable from the repo).

## 8. Front Door (Bicep)
File: `backend/src/GameStore.AppHost/bicep/frontDoor.bicep`. Why: CDN in front of blob storage for game images.
- Params: `location` (AppHost passes `Global`), `storageBlobEndpoint` (from storage output `blobEndpoint`).
- Creates: Front Door profile `afd<uniqueString>` (Standard_AzureFrontDoor), endpoint `fde<uniqueString>`, origin group + origin (the blob host), route with `UseQueryString` caching. Output `frontDoorEndpointHostName`.
- Only added in publish mode (`builder.AddBicepTemplate("frontdoor", "./bicep/frontdoor.bicep")`). Caution: AppHost.cs references `frontdoor.bicep` (lowercase) but the file is `frontDoor.bicep`; on Linux/CI rename one to match.
- Test: `curl -I https://<frontdoor-hostname>/game-images/<image-file>` (public blob access required; first hit can be slow while the route propagates).

## 9. Front end with Aspire
Project: `frontend/GameStore.Frontend.AppHost`; image built from `frontend/GameStore.Frontend/Dockerfile` (node 22 build, nginx runtime) via `PublishAsDockerFile`.
Parameters (`appsettings.Development.json` `Parameters`, or `azd` prompts when publishing) and where they land:

| Parameter | Value | Vite var / build arg |
|---|---|---|
| BackendUrl | `https://<container-app-fqdn>` (local: `http://localhost:5082`) | `VITE_BACKEND_API_URL` |
| IdentityProvider (config key, not a parameter) | `Entra` or `Keycloak` | `VITE_IDENTITY_PROVIDER` |
| EntraClientId | `<entra-spa-client-id>` | `VITE_ENTRA_CLIENT_ID` |
| EntraAuthority | `https://<entra-tenant-id>.ciamlogin.com/<entra-tenant-id>/v2.0` | `VITE_ENTRA_AUTHORITY` |
| EntraScope | `api://<entra-api-client-id>/<scope-name>` | `VITE_ENTRA_SCOPE` |
| KeycloakClientId | `gamestore-frontend` | `VITE_KEYCLOAK_CLIENT_ID` (run mode only) |
| KeycloakAuthority | `http://localhost:8080/realms/gamestore` | `VITE_KEYCLOAK_AUTHORITY` (run mode only) |
| KeycloakScope | `openid gamestore_api.all` | `VITE_KEYCLOAK_SCOPE` (run mode only) |

Local run against Entra: set `IdentityProvider` to `Entra` and the Entra values, then `dotnet run --project frontend/GameStore.Frontend.AppHost --launch-profile http` (dashboard http://localhost:15157, app http://localhost:5173). Keycloak locally: `IdentityProvider` = `Keycloak` (backend AppHost running).
Note: the Dockerfile only has build args for Backend/Identity/Entra; Keycloak vars exist only in run mode.
Deploy:
```bash
cd frontend
azd init                     # pick the AppHost project; creates azure.yaml
azd env new <azd-env-name>
azd up                       # supply BackendUrl, EntraClientId, EntraAuthority, EntraScope
```
(`azd init` flow for the frontend is inferred from the backend pattern; not shown in repo.) Produces `<frontend-container-app-fqdn>`.
Then wire back:
1. Entra portal > SPA app registration > Authentication > add redirect URI `https://<frontend-container-app-fqdn>/authentication/callback` (the callback path is from the frontend README; confirm).
2. Backend: set `AllowedOrigins` = `https://<frontend-container-app-fqdn>` in `backend/.azure/<azd-env-name>/config.json`, then `cd backend && azd deploy` (or `azd up`).
3. Smoke test: open `https://<frontend-container-app-fqdn>`, sign in, browse games; images load from `https://<frontdoor-hostname>`.

## Not verifiable from repo/handouts
- Handouts.pdf could not be rendered (pdftoppm missing); content taken from READMEs, AppHost code and the Backend_Final command shapes only.
- Provider registration list, ACR SKU, `--ingress`/`--env-vars` flags, `azd env set` keys, frontend `azd init` flow, EntraScope format, SPA redirect URI path, automatic role assignments on Postgres/Storage.
