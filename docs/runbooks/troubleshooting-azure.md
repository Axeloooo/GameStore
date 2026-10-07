# Runbook: Troubleshooting .NET Apps in Azure (deferred cloud chapters)

Order: Local, reproduce the bug, then cloud steps 1-9. Bash/zsh. Replace every `<placeholder>`; never commit real values.
Prereqs: `az` (with `containerapp` and `application-insights` extensions), `azd`, .NET 8 SDK, the app already deployed to Azure Container Apps (see the earlier runbooks), a bearer token for the API, `curl` (optional: `hey`, PowerShell).

## Wiring already in the repo
- `GameStore.ServiceDefaults/Extensions.cs` (package `Azure.Monitor.OpenTelemetry.AspNetCore` 1.3.0): `if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"])) builder.Services.AddOpenTelemetry().UseAzureMonitor();`. No setting = no export.
- `GameStore.AppHost` (package `Aspire.Hosting.Azure.ApplicationInsights` 13.0.0): `AddAzureApplicationInsights("app-insights")` + `WithReference(insights)` for api and worker, publish mode only. It injects `APPLICATIONINSIGHTS_CONNECTION_STRING` into both.
- So with `azd up` steps 1-3 happen automatically; the manual commands below are for an app deployed without Aspire's insights resource, or to understand what azd does.

## Local
Nothing needs App Insights locally; tests and `dotnet run` are unaffected. To send local telemetry anyway:
```bash
export APPLICATIONINSIGHTS_CONNECTION_STRING="<connection-string>"   # from step 3
dotnet run --project backend/src/GameStore.Api
```

## Reproduce the bug (this repo is the post-fix state)
The course's slow path never existed in this tree. Either copy `courses/09-troubleshooting-azure/Backend_Start/src/GameStore.Api/Features/Games/GetGames/GetGamesEndpoint.cs` over the repo's file, or temporarily add this in `GetGamesEndpoint.cs` after `filteredGames` is built (the handler already has a `logger` from the injected `ILoggerFactory`, currently unused; documentation only, do not commit):
```csharp
// BUG: Simulate a performance issue for certain search patterns
if (!string.IsNullOrWhiteSpace(request.Name) &&
    request.Name.StartsWith("S", StringComparison.OrdinalIgnoreCase))
{
    logger.LogWarning("Applying inefficient search algorithm for name pattern: {SearchName}", request.Name);
    await Task.Delay(2000); // 2 second delay simulating slow processing
    logger.LogInformation("Slow search path completed for: {SearchName}", request.Name);
}
```
Redeploy (`azd deploy` or step 4 image update). Fix = remove the block and redeploy (step 8).

## 1. Log Analytics workspace (App Insights stores data here)
```bash
az account set --subscription <subscription-id>
az extension add --name application-insights
az monitor log-analytics workspace create -g <resource-group> -n <log-analytics-workspace> -l <location>
az monitor log-analytics workspace show -g <resource-group> -n <log-analytics-workspace> --query id -o tsv   # <workspace-resource-id>
```

## 2. Application Insights resource (workspace-based)
```bash
az monitor app-insights component create -g <resource-group> -a <app-insights-name> -l <location> \
  --kind web --application-type web --workspace <workspace-resource-id>
```
Portal alternative: Create a resource > Application Insights > pick resource group, name, region, Log Analytics workspace.

## 3. Read the connection string
```bash
az monitor app-insights component show -g <resource-group> -a <app-insights-name> --query connectionString -o tsv
```
Produces `<connection-string>` (treat as config, do not commit). Portal: App Insights > Overview > Connection String.

## 4. Set it on the Container Apps (manual path)
Consumed by `UseAzureMonitor()` in ServiceDefaults at startup. Use a secret reference:
```bash
az containerapp secret set -g <resource-group> -n <container-app-name> --secrets appinsights-cs="<connection-string>"
az containerapp update -g <resource-group> -n <container-app-name> \
  --set-env-vars APPLICATIONINSIGHTS_CONNECTION_STRING=secretref:appinsights-cs
# repeat for the worker app: <worker-container-app-name>
```
A new revision is created; the app restarts. Via azd/Aspire (publish mode) none of this is needed: `azd provision`/`azd up` creates the `app-insights` resource and injects the variable. Check it: `az containerapp show -g <resource-group> -n <container-app-name> --query "properties.template.containers[0].env[?name=='APPLICATIONINSIGHTS_CONNECTION_STRING']"`.

## 5. Generate load
Get the URL: `az containerapp show -g <resource-group> -n <container-app-name> --query properties.configuration.ingress.fqdn -o tsv` -> `<container-app-fqdn>`.

Course script (PowerShell 7 or Windows):
```powershell
./backend/tests/scripts/test-performance.ps1 -ApiBaseUrl https://<container-app-fqdn> -AccessToken <bearer-token> -RequestCount 50
```
It sends 50 random `GET /games?name=<term>&pageNumber=1&pageSize=10` every 200 ms and prints min/avg/max latency. Bash equivalent (documentation):
```bash
terms=(Sonic Mario Skyrim Zelda Super Halo Street Pokemon Starcraft Final Sims Call Spider Grand Red Mortal Sunset Dragon Metal Portal Spore Crash Resident Silent)
for i in $(seq 1 50); do
  t=${terms[RANDOM % ${#terms[@]}]}
  curl -s -o /dev/null -w "$t %{time_total}s\n" -H "Accept: application/json" \
    -H "Authorization: Bearer <bearer-token>" \
    "https://<container-app-fqdn>/games?name=$t&pageNumber=1&pageSize=10"
  sleep 0.2
done
```
Heavy load against the node-info endpoint (README; for Live metrics/CPU spikes):
```bash
hey -n 100000 -c 10000 -H "Authorization: Bearer <bearer-token>" https://<container-app-fqdn>/diagnostics/nodeinfo
```
Telemetry takes about 2-3 minutes to show up. Stop `hey` (Ctrl+C) when done to avoid cost.

## 6. Investigate in the portal (App Insights > <app-insights-name>)
1. Live metrics: request rate, duration, failures in real time while load runs.
2. Performance: Operations tab, select `GET /games`; compare the duration distribution; drill into the slowest samples (names starting with "S" show about 2 s).
3. Failures: exceptions and failed operations by response code.
4. Application map: api -> SQL/other dependencies, average duration and call counts.
5. Transaction search / End-to-end transaction: open a slow request, inspect the timeline and traces (the `LogWarning "Applying inefficient search algorithm..."` entry).
6. Logs (Kusto):
```kusto
requests | where name has "GET /games" | summarize percentiles(duration, 50, 95) by bin(timestamp, 5m)
requests | where name has "GET /games" and duration > 1000 | project timestamp, name, url, duration, resultCode | order by duration desc
traces | where message has "inefficient search" | project timestamp, message, customDimensions
dependencies | where timestamp > ago(30m) | summarize avg(duration) by target, name
exceptions | where timestamp > ago(1h) | summarize count() by type, outerMessage
```

## 7. Optional: Container Apps platform logs
```bash
az containerapp logs show -g <resource-group> -n <container-app-name> --follow --tail 100
```

## 8. Fix and verify
Remove the delay block, redeploy (`azd deploy` or push a new image and `az containerapp update -g <resource-group> -n <container-app-name> --image <image>`), rerun step 5, wait 2-3 minutes, rerun the percentile query and confirm p95 for `GET /games` drops from about 2000+ ms to well under 1 s; Performance blade shows no slow "S" samples.

## 9. Cost and cleanup
Log Analytics ingestion and retention cost money; Container Apps bill while running.
```bash
az monitor app-insights component delete -g <resource-group> -a <app-insights-name>
az monitor log-analytics workspace delete -g <resource-group> -n <log-analytics-workspace> --yes
# everything (azd-created environment):
azd down --purge
# or only the group:
az group delete -n <resource-group> --yes --no-wait
```

## Settings produced
| Value | Produced by | Consumed by |
|---|---|---|
| `<connection-string>` | step 3 / Aspire publish | `APPLICATIONINSIGHTS_CONNECTION_STRING` env on api + worker, read in ServiceDefaults `Extensions.cs` |
| `appinsights-cs` secret | step 4 | secretref for the env var |
| `<bearer-token>` | Entra sign-in (earlier runbooks) | load-test scripts |
