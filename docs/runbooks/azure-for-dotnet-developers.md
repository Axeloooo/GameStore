# Runbook: Azure for .NET Developers (deferred cloud chapters)

Order: Local setup, then cloud steps 0-9. Bash/zsh. Replace every `<placeholder>`; never commit real values.
Backend: `backend/src/GameStore.Api` (.NET 8, Linux App Service). Frontend: `courses/04-azure-for-dotnet-developers/React_Frontend_Start/React-Frontend-Start` (or repo `frontend`).

## Local setup (manual Keycloak config missing from backend/localinfra/gamestore-realm.json)

Start infra: `dotnet run --project backend/src/GameStore.AppHost --launch-profile http` (since course 2 the AppHost starts Keycloak, PostgreSQL and Azurite; the course 1 `docker-compose.yml` no longer exists, see the README). Admin console: http://localhost:8080 (admin password: the `Parameters:keycloak-password` user secret), realm `gamestore`.

1. Client scope `gamestore_api.all` (Client scopes > Create, type Optional, protocol OpenID Connect).
   - Mapper "Audience": Add mapper > By configuration > Audience. Included Client Audience `gamestore-api`, Add to access token ON. Why: API validates `ValidAudience=gamestore-api`.
   - Mapper "Realm roles": User Realm Role. Token Claim Name `role`, Claim JSON type String, Multivalued ON, Add to access token ON. Why: API uses `RoleClaimType = "role"` for Keycloak.
2. Client `gamestore-frontend-react`: OpenID Connect, Client authentication OFF (public), Standard flow ON, Direct access grants ON, Valid redirect URIs `http://localhost:5173/*`, Web origins `http://localhost:5173`. Client scopes tab > Add client scope > `gamestore_api.all` (Default or Optional; frontend requests it explicitly).
3. Realm role `Admin` (Realm roles > Create). Test user (Users > Add, set Email verified ON, Credentials > Set password, Temporary OFF). Role mapping > Assign role > `Admin`.

Backend `appsettings.json` (keys that need real values only for cloud use):
- `Authentication:Schemes:Entra:ValidAudience` = `<entra-api-app-client-id>`
- `Authentication:Schemes:Entra:Authority` = `https://<tenant-subdomain>.ciamlogin.com/<tenant-id>/v2.0`
- Keycloak entries and `ConnectionStrings` (`GameStoreDB`, `Blobs` = `UseDevelopmentStorage=true`) and `AllowedOrigins` (`http://localhost:5173`) already work locally.

Frontend `.env` (local):
```
VITE_BACKEND_API_URL=http://localhost:5082
VITE_IDENTITY_PROVIDER=keycloak
VITE_KEYCLOAK_AUTHORITY=http://localhost:8080/realms/gamestore
VITE_KEYCLOAK_CLIENT_ID=gamestore-frontend-react
VITE_KEYCLOAK_SCOPE=openid gamestore_api.all
```
Cloud also uses `VITE_IDENTITY_PROVIDER=entra`, `VITE_ENTRA_AUTHORITY`, `VITE_ENTRA_CLIENT_ID`, `VITE_ENTRA_SCOPE` (see steps 3 and 8). Note: the frontend README lists redirect `/authentication/callback`; the wildcard above covers it.

## 0. Login and variables
```bash
az login
az account set --subscription <subscription-id>
az provider register -n Microsoft.Web -n Microsoft.Storage -n Microsoft.Cdn -n Microsoft.DBforPostgreSQL -n Microsoft.KeyVault -n Microsoft.ManagedIdentity
az group create -n <resource-group> -l <location>   # one RG holds everything; delete it to clean up
```

## 1. Managed identity (user-assigned)
Why: one identity for the API to reach Storage and PostgreSQL with no secrets.
```bash
az identity create -g <resource-group> -n <identity-name>
az identity show -g <resource-group> -n <identity-name> --query "{clientId:clientId, principalId:principalId, id:id}" -o json
```
Produces: `<identity-client-id>`, `<identity-principal-id>`, `<identity-resource-id>`. Consumed by app setting `AZURE_CLIENT_ID` (Program.cs `ManagedIdentityClientId`) in step 4.

## 2. Azure Storage (cloud part)
Why: replaces Azurite (`UseDevelopmentStorage=true`); holds game images.
```bash
az storage account create -g <resource-group> -n <storage-account> -l <location> --sku Standard_LRS --kind StorageV2 --allow-blob-public-access true --allow-shared-key-access false
az role assignment create --assignee-object-id <identity-principal-id> --assignee-principal-type ServicePrincipal --role "Storage Blob Data Contributor" --scope $(az storage account show -g <resource-group> -n <storage-account> --query id -o tsv)
# your own user, to upload seed images
az role assignment create --assignee <your-user-principal-name> --role "Storage Blob Data Contributor" --scope $(az storage account show -g <resource-group> -n <storage-account> --query id -o tsv)
az storage container create --account-name <storage-account> --name game-images --public-access blob --auth-mode login
az storage blob upload-batch --account-name <storage-account> -d game-images -s courses/04-azure-for-dotnet-developers/GameImages --auth-mode login
```
Why public blob read: Front Door (step 7) fetches images anonymously; container name `game-images` is `StorageNames.GameImagesFolder`. Role assignments can take minutes to propagate.
Produces: `ConnectionStrings__Blobs` = `https://<storage-account>.blob.core.windows.net` (a URL, not a key; the API uses the managed identity in non-Development).

## 3. Microsoft Entra external tenant and app registrations (portal + CLI)
Why: customer identity for the cloud API and SPA (`Entra` scheme in appsettings.json).
Portal (external tenant creation is not available via az CLI):
1. https://entra.microsoft.com > Entra ID > Overview > Manage tenants > Create > External > Name `<tenant-name>`, domain `<tenant-subdomain>`, location, subscription `<subscription-id>`, resource group `<resource-group>`. Switch directory into it.
2. Overview: note `<tenant-id>`; `<tenant-subdomain>` is the domain prefix (`<tenant-subdomain>.onmicrosoft.com`).
3. External Identities > User flows > New user flow: email + password sign-up. Add the SPA app (step 3b) to the flow afterwards (Applications tab).

CLI into the external tenant:
```bash
az login --tenant <tenant-id> --allow-no-subscriptions
# 3a. API app
az ad app create --display-name "GameStore API" --sign-in-audience AzureADMyOrg --query appId -o tsv     # <entra-api-app-client-id>
az ad sp create --id <entra-api-app-client-id>
az ad app update --id <entra-api-app-client-id> --identifier-uris "api://<entra-api-app-client-id>"
```
Portal for the rest of the API app (App registrations > GameStore API):
- Expose an API: add scope `gamestore_api.all` (admins and users can consent), state Enabled.
- Manifest: set `requestedAccessTokenVersion` to `2` (`api.requestedAccessTokenVersion`). Why: v2 tokens match the `/v2.0` Authority and `aud` = client id.
- App roles: create role display name `Admin`, value `Admin`, allowed member types Users/Groups. Why: API reads claim `roles` for Entra.
- Enterprise applications > GameStore API > Users and groups > assign your test user to `Admin`.
```bash
# 3b. SPA app
az ad app create --display-name "GameStore React" --sign-in-audience AzureADMyOrg \
  --enable-id-token-issuance true --query appId -o tsv        # <entra-spa-app-client-id>
az ad sp create --id <entra-spa-app-client-id>
az ad app update --id <entra-spa-app-client-id> --set spa='{"redirectUris":["http://localhost:5173/authentication/callback","https://<static-web-app-hostname>/authentication/callback"]}'
```
Portal: SPA app > API permissions > Add > My APIs > GameStore API > Delegated `gamestore_api.all`; also OpenID `openid`, `profile`, `email`, `offline_access`; Grant admin consent. Add the SPA app to the user flow.
Produces:
- Backend: `Authentication__Schemes__Entra__ValidAudience=<entra-api-app-client-id>`, `Authentication__Schemes__Entra__Authority=https://<tenant-subdomain>.ciamlogin.com/<tenant-id>/v2.0` (step 4).
- Frontend: `VITE_ENTRA_CLIENT_ID=<entra-spa-app-client-id>`, `VITE_ENTRA_AUTHORITY=https://<tenant-subdomain>.ciamlogin.com/<tenant-id>/v2.0`, `VITE_ENTRA_SCOPE=api://<entra-api-app-client-id>/gamestore_api.all openid profile email offline_access` (step 8).

Return to the subscription tenant: `az login --tenant <subscription-tenant-id>`; `az account set --subscription <subscription-id>`.

## 4. Azure Database for PostgreSQL (Flexible Server, Entra auth)
Why: managed Postgres; the API gets an Entra token as password (DataExtensions `UsePeriodicPasswordProvider`, scope `https://ossrdbms-aad.database.windows.net/.default`) when not in Development.
```bash
az postgres flexible-server create -g <resource-group> -n <postgres-server> -l <location> --tier Burstable --sku-name Standard_B1ms --storage-size 32 --version 16 \
  --microsoft-entra-auth Enabled --password-auth Disabled --public-access 0.0.0.0 --yes
az postgres flexible-server ad-admin create -g <resource-group> -s <postgres-server> -u <your-user-principal-name> -i <your-user-object-id> -t User
az postgres flexible-server db create -g <resource-group> -s <postgres-server> -d gamestore
```
If `--password-auth Disabled` is rejected at create time, create with Entra enabled and disable password auth afterwards via `az postgres flexible-server update`. `0.0.0.0` allows Azure services; add your IP for psql: `az postgres flexible-server firewall-rule create -g <resource-group> -n <postgres-server> --rule-name dev --start-ip-address <your-ip> --end-ip-address <your-ip>`.

Grant the managed identity DB rights (connect as the Entra admin):
```bash
export PGPASSWORD=$(az account get-access-token --resource-type oss-rdbms --query accessToken -o tsv)
psql "host=<postgres-server>.postgres.database.azure.com dbname=postgres user=<your-user-principal-name> sslmode=require" <<'SQL'
SELECT * FROM pgaadauth_create_principal('<identity-name>', false, false);
SQL
psql "host=<postgres-server>.postgres.database.azure.com dbname=gamestore user=<your-user-principal-name> sslmode=require" <<'SQL'
GRANT ALL PRIVILEGES ON DATABASE gamestore TO "<identity-name>";
GRANT ALL ON SCHEMA public TO "<identity-name>";
SQL
```
Why: the API runs EF migrations at startup (`MigrateDbAsync`) so the identity needs create rights.
Produces: `ConnectionStrings__GameStoreDB=Host=<postgres-server>.postgres.database.azure.com;Database=gamestore;Username=<identity-name>;Ssl Mode=Require` (no password).

## 5. Azure App Service (API)
Why: PaaS host for the API.
```bash
az appservice plan create -g <resource-group> -n <app-service-plan> -l <location> --is-linux --sku B1
az webapp create -g <resource-group> -p <app-service-plan> -n <app-service-name> --runtime "DOTNETCORE:8.0"
az webapp identity assign -g <resource-group> -n <app-service-name> --identities <identity-resource-id>
az webapp config appsettings set -g <resource-group> -n <app-service-name> --settings \
  ASPNETCORE_ENVIRONMENT=Production \
  AZURE_CLIENT_ID=<identity-client-id> \
  "ConnectionStrings__GameStoreDB=Host=<postgres-server>.postgres.database.azure.com;Database=gamestore;Username=<identity-name>;Ssl Mode=Require" \
  ConnectionStrings__Blobs=https://<storage-account>.blob.core.windows.net \
  "Authentication__Schemes__Entra__ValidAudience=<entra-api-app-client-id>" \
  "Authentication__Schemes__Entra__Authority=https://<tenant-subdomain>.ciamlogin.com/<tenant-id>/v2.0" \
  AllowedOrigins=https://<static-web-app-hostname> \
  AZURE_FRONTDOOR_HOSTNAME=<front-door-endpoint-hostname>
```
(Set `AZURE_FRONTDOOR_HOSTNAME` after step 7; re-run the command with just that setting.) `AllowedOrigins` is consumed by `CorsExtensions`; multiple origins are separated by `;`.
Deploy:
```bash
dotnet publish backend/src/GameStore.Api -c Release -o <publish-dir>
cd <publish-dir> && zip -r ../api.zip . && cd -
az webapp deploy -g <resource-group> -n <app-service-name> --src-path api.zip --type zip
curl https://<app-service-name>.azurewebsites.net/games
```
Keycloak scheme settings are unused in the cloud; the Entra scheme is selected from the token issuer.

## 6. Key Vault
Why: store secrets (course shows a Blazor client secret; the React SPA has none, so this is for any future secret and for the pattern).
```bash
az keyvault create -g <resource-group> -n <key-vault> -l <location> --enable-rbac-authorization true
az role assignment create --assignee <your-user-principal-name> --role "Key Vault Secrets Officer" --scope $(az keyvault show -n <key-vault> --query id -o tsv)
az role assignment create --assignee-object-id <identity-principal-id> --assignee-principal-type ServicePrincipal --role "Key Vault Secrets User" --scope $(az keyvault show -n <key-vault> --query id -o tsv)
az keyvault secret set --vault-name <key-vault> -n <secret-name> --value "<secret-value>"
# app setting as Key Vault reference using the user-assigned identity
az webapp update -g <resource-group> -n <app-service-name> --set keyVaultReferenceIdentity=<identity-resource-id>
az webapp config appsettings set -g <resource-group> -n <app-service-name> --settings "<Setting__Name>=@Microsoft.KeyVault(SecretUri=https://<key-vault>.vault.azure.net/secrets/<secret-name>/)"
```
Consumed as a normal configuration key (`<Setting__Name>`) by the API.

## 7. Azure Front Door (CDN for images)
Why: cache `game-images` blobs at the edge; API rewrites image URLs via `CdnUrlTransformer` when `AZURE_FRONTDOOR_HOSTNAME` is set.
```bash
az afd profile create -g <resource-group> --profile-name <front-door-profile> --sku Standard_AzureFrontDoor
az afd endpoint create -g <resource-group> --profile-name <front-door-profile> --endpoint-name <front-door-endpoint> --enabled-state Enabled
az afd origin-group create -g <resource-group> --profile-name <front-door-profile> --origin-group-name storage-og --probe-request-type HEAD --probe-protocol Https --probe-path / --probe-interval-in-seconds 100 --sample-size 4 --successful-samples-required 3 --additional-latency-in-milliseconds 50
az afd origin create -g <resource-group> --profile-name <front-door-profile> --origin-group-name storage-og --origin-name storage --host-name <storage-account>.blob.core.windows.net --origin-host-header <storage-account>.blob.core.windows.net --http-port 80 --https-port 443 --priority 1 --weight 1000 --enabled-state Enabled
az afd route create -g <resource-group> --profile-name <front-door-profile> --endpoint-name <front-door-endpoint> --route-name images --origin-group storage-og --supported-protocols Https --https-redirect Enabled --forwarding-protocol HttpsOnly --link-to-default-domain Enabled --patterns-to-match "/*"
az afd endpoint show -g <resource-group> --profile-name <front-door-profile> --endpoint-name <front-door-endpoint> --query hostName -o tsv
```
Produces `<front-door-endpoint-hostname>` -> app setting `AZURE_FRONTDOOR_HOSTNAME` (step 5). Verify after a few minutes: `https://<front-door-endpoint-hostname>/game-images/<image-file-name>`.

## 8. Front-end deployment (React on Static Web App)
Why: static hosting; `staticwebapp.config.json` already provides SPA fallback.
```bash
az staticwebapp create -g <resource-group> -n <static-web-app> -l <location> --sku Free
az staticwebapp show -g <resource-group> -n <static-web-app> --query defaultHostname -o tsv      # <static-web-app-hostname>
az staticwebapp secrets list -g <resource-group> -n <static-web-app> --query properties.apiKey -o tsv   # <swa-deployment-token>, treat as secret
```
Create `.env.production` in the frontend folder (Vite bakes values at build):
```
VITE_BACKEND_API_URL=https://<app-service-name>.azurewebsites.net
VITE_IDENTITY_PROVIDER=entra
VITE_ENTRA_AUTHORITY=https://<tenant-subdomain>.ciamlogin.com/<tenant-id>/v2.0
VITE_ENTRA_CLIENT_ID=<entra-spa-app-client-id>
VITE_ENTRA_SCOPE=api://<entra-api-app-client-id>/gamestore_api.all openid profile email offline_access
```
Build and deploy:
```bash
cd <frontend-dir> && npm ci && npm run build
npx @azure/static-web-apps-cli deploy ./dist --deployment-token <swa-deployment-token> --env production
```
Then make sure `https://<static-web-app-hostname>/authentication/callback` is a SPA redirect URI (step 3b) and `AllowedOrigins` on the API (step 5) matches.

## 9. Smoke test
- Open `https://<static-web-app-hostname>`; catalog loads without login; images come from `<front-door-endpoint-hostname>`.
- Sign in with the Entra test user (assigned `Admin`); create a game with an image: blob appears in `game-images`, row in `gamestore` DB.
- Troubleshoot: `az webapp log tail -g <resource-group> -n <app-service-name>`.

## Settings summary
| Setting | Produced in | Consumed by |
|---|---|---|
| AZURE_CLIENT_ID | 1 | Program.cs DefaultAzureCredential |
| ConnectionStrings__Blobs | 2 | FileUploadExtensions |
| ConnectionStrings__GameStoreDB | 4 | DataExtensions |
| Authentication__Schemes__Entra__* | 3 | AuthorizationExtensions |
| AllowedOrigins | 8 | CorsExtensions |
| AZURE_FRONTDOOR_HOSTNAME | 7 | CdnUrlTransformer |
| VITE_* (entra) | 3, 8 | frontend build |
