# Runbook: Payments, Queues & Workers (deferred cloud chapters)

Order: Local run, then cloud steps 0-7. Bash/zsh. Replace every `<placeholder>`; never commit real values.
Backend AppHost: `backend/src/GameStore.AppHost` (projects `gamestore-api`, `gamestore-worker`). The frontend AppHost was removed in LRN-283; the frontend runs with npm.
Prereqs: Docker running, .NET 8 SDK + Aspire, `az`, `azd`, Node 22, a Stripe account in TEST mode. Reuses the Entra tenant and app registrations from the earlier runbooks (`<entra-api-client-id>`, `<entra-tenant-id>`, `<entra-spa-client-id>`) and the resource group from the containers runbook.

## Local run
0. One-time cleanup of course 2 containers/volumes. DESTRUCTIVE: wipes local dev data only (Postgres, Azurite, Keycloak). Needed because the old `gamestore.apphost-*` persistent containers collide with this AppHost and the reset migration ids break `MigrateAsync`.
```bash
docker ps -a --filter "name=gamestore.apphost"      # review the list first
docker ps -a -q --filter "name=gamestore.apphost" | xargs -r docker rm -f
docker volume ls --filter "name=gamestore.apphost"  # review the list first
docker volume ls -q --filter "name=gamestore.apphost" | xargs -r docker volume rm
```
1. Stripe test key (sk_test_ ONLY, never a live key). Get it in Stripe Dashboard (test mode) > Developers > API keys.
```bash
dotnet user-secrets set "Parameters:StripeApiKey" "<stripe-test-secret-key>" --project backend/src/GameStore.AppHost
```
Consumed by `AddParameter("StripeApiKey", secret: true)`: passed as `STRIPE_API_KEY` to the Stripe CLI containers and as `Stripe__SecretKey` to the API. Other parameters come from `backend/src/GameStore.AppHost/appsettings.json` (`EntraValidAudience`, `EntraAuthority`, `AllowedOrigins`, `CheckoutReturnUrl` = `http://localhost:5173/order-created`); override with user-secrets if needed.
2. Run:
```bash
dotnet run --project backend/src/GameStore.AppHost --launch-profile http
```
What starts: Postgres + pgAdmin, Azurite, Service Bus emulator (queue `orders`, duplicate detection on), Keycloak, API, Worker, and the Stripe CLI as Docker image `stripe/stripe-cli`: `stripeSecretGen` (prints the webhook signing secret to `backend/.stripe/webhook_secret.txt`, gitignored) and `stripeListener` (forwards to `<api>/payments/stripe-webhook`). The AppHost reads that file and injects `Stripe__EndpointSecret` into the API. `gamestore-api` waits for `stripeSecretGen`, so it will not start with an invalid key. The webhook path is already macOS-safe in the repo (course used a Windows path).
3. Keycloak user (realm has clients `gamestore-frontend*`/`postman` and scope `gamestore_api.all`, but no users): http://localhost:8080 admin console > realm `gamestore` > Users > add user, set password, Role mapping > assign the `Admin` role. For curl testing, enable "Direct access grants" on the client you use.
```bash
TOKEN=$(curl -s -X POST http://localhost:8080/realms/gamestore/protocol/openid-connect/token \
  -d grant_type=password -d client_id=<keycloak-client-id> -d username=<user> -d password=<password> \
  -d scope="openid gamestore_api.all" | jq -r .access_token)
```
(Token request shape is standard OIDC, not from the repo.)
4. Payment flow:
```bash
# 1) basket (shape per course Postman collection GameStore.Api.postman_collection.json)
curl -X PUT http://localhost:5082/baskets/<basket-id> -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '<basket-json>'
# 2) checkout: returns clientSecret (Stripe UiMode custom); idempotent on operationId
curl -X POST http://localhost:5082/payments/checkout -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '<checkout-json-with-operationId>'
# 3) simulate payment (needs the Stripe CLI logged in to the same test account, or run inside the container)
stripe trigger checkout.session.completed
```
Expected: order status `Completed`, `OutboxMessages` rows processed (pgAdmin http://localhost:5050), and a Worker log line in the Aspire dashboard http://localhost:15054. Note `stripe trigger` creates a generic session without your order metadata, so a real order completes only by paying a session from the front end (test card 4242 4242 4242 4242); confirm in the dashboard logs.
5. Front end (the frontend AppHost step was removed in LRN-283; the frontend now runs with npm): in `frontend/GameStore.Frontend/.env.local` set `VITE_STRIPE_PUBLISHABLE_KEY=<stripe-test-publishable-key>` (pk_test_ only), then `npm ci && npm run dev` (app http://localhost:5173).

Local deviations from the course
- `backend/src/GameStore.Api/Shared/Stripe/StripeEventFactory.cs` (course 4 moved `ConstructEvent` there; `StripeWebhookEndpoint.cs` now takes `IStripeEventFactory`) calls `EventUtility.ConstructEvent(..., throwOnApiVersionMismatch: false)`. Stripe.net pins API version `2025-09-30.clover`; `stripe listen` forwards events in the account's default version (for example `2022-11-15`; `--latest` gives a newer release, which also mismatches), so without this every webhook returned 400 (`Received event with API version ..., but Stripe.net expects API version 2025-09-30.clover`) and orders stayed `Pending`. The signature is still verified.
- `EntraAuthority` placeholder: with `[ENTRA AUTHORITY HERE]`, every anonymous request (including `/games` and `/health/ready`) returns 500 (`The MetadataAddress or Authority must use HTTPS ...`). Locally, set any https authority:
```bash
dotnet user-secrets set "Parameters:EntraAuthority" "https://login.microsoftonline.com/common/v2.0" --project backend/src/GameStore.AppHost
```
- Postgres/Keycloak password mismatch after restarts: if `Parameters:postgres-password` / `Parameters:keycloak-password` were not in the AppHost user-secrets when the persistent volumes were created, a restart generates new passwords and Postgres logs `password authentication failed for user "postgres"` (API stays Waiting), and the Keycloak admin login fails. Remove the local volumes (DESTRUCTIVE, local dev data only) and restart; Aspire then saves the passwords to user-secrets.
```bash
docker ps -a --filter "name=-<apphost-hash>"        # review first
docker rm -f -v <postgres-container> <keycloak-container>
docker volume rm gamestore.apphost-<apphost-hash>-postgres-data gamestore.apphost-<apphost-hash>-keycloak-data
```

Known issues
- `AppHost.cs` references `./bicep/frontdoor.bicep`; since the Azure DevOps course the file is also named `frontdoor.bicep` (lowercase), so the case mismatch that breaks publish on case-sensitive filesystems (Linux/CI) is gone.
- `backend/.gitignore` was dropped; the root `.gitignore` now ignores `.azure/` (azd env, may hold parameters), `.env.local`, `node_modules/` and `dist/`. Never `git add` them.

## 0. Login and variables
```bash
az login --tenant <tenant-id>
az account set --subscription <subscription-id>
az provider register -n Microsoft.ServiceBus -n Microsoft.KeyVault -n Microsoft.App -n Microsoft.ContainerRegistry -n Microsoft.DBforPostgreSQL -n Microsoft.Storage -n Microsoft.Cdn
```
Why: new resource providers (Service Bus, Key Vault) must be registered. (List inferred.)

## 1. Stripe webhook endpoint (after the API is deployed, step 4)
Why: Stripe must call the deployed API; the endpoint gets its own signing secret, different from the local CLI one.
Stripe Dashboard (test mode) > Developers > Webhooks (event destinations) > Add endpoint:
- URL: `https://<container-app-fqdn>/payments/stripe-webhook`
- Event: `checkout.session.completed` (the event the handler processes; add others only if the handler uses them)
- API version: the account's default is fine. The repo passes `throwOnApiVersionMismatch: false` in `backend/src/GameStore.Api/Shared/Stripe/StripeEventFactory.cs` (see "Local deviations from the course"), so events in a version other than Stripe.net's pinned one are accepted.
- Copy the signing secret `<stripe-webhook-secret>` (whsec_...) from the endpoint page.
Consumed by step 3 as `Stripe--EndpointSecret` -> `Stripe:EndpointSecret` -> `StripeOptions.EndpointSecret`, used by the webhook endpoint to verify the `Stripe-Signature` header.
Ordering: you need `<container-app-fqdn>` first, so do step 4, then this step, then step 3's endpoint-secret command, then restart the revision.

## 2. Service Bus (what azd creates)
`AddAzureServiceBus("serviceBus")` plus queue `orders` with `RequiresDuplicateDetection = true` run as the emulator locally and are provisioned by azd in publish mode; no manual step. Connection name `serviceBus` is consumed by API (`AddMessaging`) and Worker (`AddAzureServiceBusClient`) using managed identity (`credential` in Program.cs).
Verify after step 4:
```bash
az servicebus namespace list -g <resource-group> -o table          # <service-bus-namespace>
az servicebus queue show -g <resource-group> --namespace-name <service-bus-namespace> -n orders --query requiresDuplicateDetection
```
Manual alternative (only if not using azd; not in AppHost):
```bash
az servicebus namespace create -g <resource-group> -n <service-bus-namespace> -l <location> --sku Standard
az servicebus queue create -g <resource-group> --namespace-name <service-bus-namespace> -n orders --enable-duplicate-detection true
```
Roles needed on the namespace for the API and Worker identities: `Azure Service Bus Data Sender` / `Data Receiver` (Aspire normally assigns them; confirm in portal).

## 3. Key Vault secrets (Stripe)
In publish mode the AppHost adds `AddAzureKeyVault("keyvault")` and secret `stripeApiKeySecret` named `Stripe--SecretKey` with the value of parameter `StripeApiKey`; the API references `keyvault` and, when `ASPNETCORE_ENVIRONMENT=Production`, calls `AddAzureKeyVaultSecrets("keyvault")`, so `Stripe--SecretKey` becomes `Stripe:SecretKey`. azd prompts for `StripeApiKey` during `azd up` (use the sk_test_ value; stored by azd as a secret in the azd env, not in config.json).
Endpoint secret: the AppHost does not create it, so add it yourself after step 1 (name chosen to match `StripeOptions.EndpointSecret`; inferred):
```bash
az role assignment create --assignee <your-object-id> --role "Key Vault Secrets Officer" --scope $(az keyvault show -n <key-vault-name> --query id -o tsv)
az keyvault secret set --vault-name <key-vault-name> --name "Stripe--EndpointSecret" --value "<stripe-webhook-secret>"
az keyvault secret list --vault-name <key-vault-name> -o table
az containerapp revision restart -g <resource-group> -n <container-app-name> --revision <revision-name>
```
The API's managed identity needs `Key Vault Secrets User` on the vault (Aspire normally assigns it; confirm). `Stripe__CheckoutReturnUrl` comes from parameter `CheckoutReturnUrl`.

## 4. Backend deploy (azd / Container Apps / Front Door)
The Bicep file name already matches the AppHost reference (`frontdoor.bicep`), so no rename is needed on Linux/CI.
```bash
cd backend
azd auth login --tenant-id <tenant-id>
azd env new <azd-env-name>
azd env set AZURE_SUBSCRIPTION_ID <subscription-id>
azd env set AZURE_LOCATION <location>
azd up
```
Prompts and where they land:

| Parameter | Value | Consumed as |
|---|---|---|
| EntraValidAudience | `<entra-api-client-id>` | `Authentication__Schemes__Entra__ValidAudience` |
| EntraAuthority | `https://<entra-tenant-id>.ciamlogin.com/<entra-tenant-id>/v2.0` | `Authentication__Schemes__Entra__Authority` |
| AllowedOrigins | `https://<frontend-container-app-fqdn>` (first run: `http://localhost:5173`) | `AllowedOrigins` (CORS, publish only) |
| CheckoutReturnUrl | `https://<frontend-container-app-fqdn>/order-created` | `Stripe__CheckoutReturnUrl` |
| StripeApiKey (secret) | `<stripe-test-secret-key>` | Key Vault `Stripe--SecretKey` |

Also produced: Front Door hostname -> `AZURE_FRONTDOOR_HOSTNAME`, Service Bus namespace + `orders` queue, Key Vault, Worker container app. Output: `<container-app-fqdn>`.
```bash
azd show
az containerapp list -g <resource-group> -o table
curl https://<container-app-fqdn>/payments/stripe-webhook -X POST -i    # expect 400 (no signature), proves it is reachable
```
Run steps 1 and 3 now, then update `AllowedOrigins`/`CheckoutReturnUrl` once the front end exists and `azd deploy`. Never commit `backend/.azure/`.

## 5. Front-end deploy (StripePublishableKey)
> Removed in LRN-283: `frontend/azure.yaml` and the front-end AppHost no longer exist, so the `cd frontend; azd env new; azd up` flow below and the frontend `azd down` in step 7 are historical. The `VITE_*` values (including the Stripe publishable key) are now passed as Docker build args or set in `.env.local`; see `frontend/GameStore.Frontend/.env.example`.
Parameter `StripePublishableKey` (`<stripe-test-publishable-key>`, pk_test_) -> Docker build arg `VITE_STRIPE_PUBLISHABLE_KEY` (baked into the bundle; publishable, not secret). Other parameters: `BackendUrl` = `https://<container-app-fqdn>`, `EntraClientId` = `<entra-spa-client-id>`, `EntraAuthority`, `EntraScope`; `IdentityProvider` = `Entra` in config.
```bash
cd frontend
azd env new <azd-env-name-frontend>
azd up      # supply BackendUrl, EntraClientId, EntraAuthority, EntraScope, StripePublishableKey
```
Produces `<frontend-container-app-fqdn>`.

## 6. Entra values to update
1. SPA registration > Authentication: redirect URI `https://<frontend-container-app-fqdn>/authentication/callback` (path from earlier runbook; confirm).
2. Backend `AllowedOrigins` and `CheckoutReturnUrl` = frontend FQDN (step 4), then `cd backend && azd deploy`.

## 7. Smoke test
Sign in on `https://<frontend-container-app-fqdn>`, add a game to the basket, check out with test card 4242 4242 4242 4242. Then:
- Stripe Dashboard > Webhooks > endpoint shows 200 delivery.
- Order status `Completed`; outbox processed; Worker logs: `az containerapp logs show -g <resource-group> -n <worker-container-app-name> --follow`.
- Clean up: `azd down` (both backend and frontend envs).

## Not verifiable from repo/handouts
- Handouts.pdf was not rendered; content comes from the scrubbed README, AppHost code, StripeCLI.Hosting and the front-end AppHost.
- Provider list, `Stripe--EndpointSecret` Key Vault name and the need to add it manually (AppHost only creates `Stripe--SecretKey`), role assignments and their auto-creation, Stripe webhook event list, Keycloak token request, basket/checkout JSON bodies, `azd env set` keys, `revision restart`, frontend `azd` flow, Entra redirect path, 400 on unsigned webhook, `stripe trigger` not producing a matching order.
