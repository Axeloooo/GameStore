# Runbook: Azure DevOps CI/CD (deferred cloud chapters)

Order: Local validation, repo-layout fix (already applied), then cloud steps 0-10. Bash/zsh. Replace every `<placeholder>`; never commit real values.
Pipeline: `backend/.azdo/pipelines/azure-dev.yml` (jobs Build -> ParallelTesting x2 -> Deploy). azd project: `backend/azure.yaml` (service `app` -> `src/GameStore.AppHost`).
Prereqs: `az` (>= 2.50) with the `azure-devops` extension, `azd`, .NET 8 SDK, Docker, an Azure subscription where you can create app registrations and assign roles (Owner or Contributor + User Access Administrator, because azd/Aspire creates role assignments), the earlier runbooks' Entra values, a Stripe TEST key.

## Local validation
```bash
yamllint -d relaxed backend/.azdo/pipelines/azure-dev.yml
# the file has CRLF line endings and long lines; relaxed reports those. To silence them:
yamllint -d '{extends: relaxed, rules: {new-lines: disable, line-length: disable}}' backend/.azdo/pipelines/azure-dev.yml
dotnet test backend/Backend.sln        # Docker must be running (Testcontainers); 23 integration + 82 unit tests (1 skipped)
dotnet test backend/tests/GameStore.Api.UnitTests   # unit tests only, no Docker, a few seconds
```
Local equivalent of the pipeline test slicing (xunit v3 test DLL run directly, 2 slices):
```bash
cd backend && dotnet build
DLL=tests/GameStore.IntegrationTests/bin/Debug/net8.0/GameStore.IntegrationTests.dll
tests=($(dotnet $DLL --list-tests | grep '^ *GameStore\.IntegrationTests\.' | sed 's/^ *//'))
for pos in 1 2; do
  unset filter
  SYSTEM_TOTALJOBSINPHASE=2 SYSTEM_JOBPOSITIONINPHASE=$pos \
    bash tests/scripts/create_slicing_filter_condition.sh "${tests[@]}"   # prints "##vso[task.setvariable ...]<names>"
done
# run one slice by hand (space-delimited names from the line above):
dotnet $DLL --filter-method "<name1> <name2> ..."
```
Notes: the script sets `targetTestsFilter` with `##vso[task.setvariable]`, consumed by the step `Run tests` as `$(targetTestsFilter)`. Agents get tests round-robin (agent 1: 1,3,5..; agent 2: 2,4,6..). The script is committed with LF endings (was CRLF in the course zip; a CRLF `#!/bin/bash\r` breaks on ubuntu agents). Keep it LF: `sed -i '' 's/\r$//' backend/tests/scripts/create_slicing_filter_condition.sh` (macOS; GNU: drop the `''`) and mark `*.sh text eol=lf` in `.gitattributes` if the repo gains one.

## Repo-layout fix (the .NET solution lives in `backend/`)
Status: Option A is applied in `backend/.azdo/pipelines/azure-dev.yml`. The explanation below is kept for reference.

The course pipeline assumed the solution, `tests/`, `azure.yaml` are at the repo root and `.azdo/` at `<repo>/.azdo`. Here they are under `backend/`. Without changes: `DotNetCoreCLI@2 build`'s default `projects` glob would pick up every csproj under `backend/` individually (rather than the solution), and the `publish:` paths and `azd` still assume the repo-root layout, so set `projects: 'backend/Backend.sln'` explicitly; both `publish:` paths do not exist at the root; `azd` finds no `azure.yaml`.

Option A (applied): keep the monorepo and edit the pipeline. The diff the pipeline file now carries (the trigger uses `devel`):
```diff
-trigger:
-  - main
+trigger:
+  branches:
+    include: [devel]
+  paths:
+    include: [backend/*]
@@ Build
       - task: DotNetCoreCLI@2
         displayName: Build
         inputs:
           command: 'build'
+          projects: 'backend/Backend.sln'
-      - publish: $(System.DefaultWorkingDirectory)/tests/GameStore.IntegrationTests/bin/Debug/net8.0
+      - publish: $(System.DefaultWorkingDirectory)/backend/tests/GameStore.IntegrationTests/bin/Debug/net8.0
         artifact: TestBinaries
-      - publish: $(System.DefaultWorkingDirectory)/tests/scripts
+      - publish: $(System.DefaultWorkingDirectory)/backend/tests/scripts
         artifact: TestScripts
@@ Deploy (both AzureCLI@2 tasks, provision and deploy)
         inputs:
           azureSubscription: azconnection
           scriptType: bash
           scriptLocation: inlineScript
           keepAzSessionActive: true
+          workingDirectory: backend
           inlineScript: |
             azd provision --no-prompt     # and: azd deploy --no-prompt
```
- `workingDirectory: backend` (AzureCLI@2 input) is the pipeline equivalent of `cd backend`; alternatively put `cd backend` first in each `inlineScript`.
- `ParallelTesting` needs no change: it uses `checkout: none` and downloads the artifacts to `$(Pipeline.Workspace)/TestBinaries|TestScripts`.
- The Build job's `Run unit tests` step (`DotNetCoreCLI@2 test`) also needs the prefix: `projects: 'backend/tests/GameStore.Api.UnitTests/GameStore.Api.UnitTests.csproj'`.
- The course `trigger: - main` branch filter did not match this repo's default branch `devel`; the applied file triggers on `devel`, so the repository the pipeline reads must have a `devel` branch.
- Path to the YAML when creating the pipeline becomes `backend/.azdo/pipelines/azure-dev.yml` (step 6).

Option B: import `backend/` as the root of a dedicated repo (e.g. `git subtree split -P backend -b backend-only`, push that branch as `main` of `<repo-name>`). Then the original course pipeline works unchanged (the edited file in this repo would need its `backend/` prefixes and `paths` filter reverted) and the YAML path is `.azdo/pipelines/azure-dev.yml`. Loses history linkage with the monorepo.

Also required on Linux agents: the Bicep file name must match the AppHost reference (`backend/src/GameStore.AppHost/bicep/frontdoor.bicep`; renamed in this PR). Case mismatch breaks `azd provision` on ubuntu.

## 0. Login and variables
```bash
az login --tenant <tenant-id>
az account set --subscription <subscription-id>
az extension add --name azure-devops
az devops configure --defaults organization=https://dev.azure.com/<organization> project=<project>
```
Why: the `azure-devops` extension provides `az devops` / `az pipelines`; defaults avoid repeating `--org/--project`.

## 1. Organization and project
Organization: portal only, https://dev.azure.com > New organization (name `<organization>`, region `<location>`). It must be linked to the same Entra tenant (`<tenant-id>`) as the subscription: Organization settings > Microsoft Entra.
```bash
az devops login                         # PAT `<pat>` with Project + Code + Build + Service Connections scopes; paste at the prompt
az devops project create --name <project> --visibility private --source-control git
```
Why: the pipeline, repo and service connection live in the project.

## 2. Repo import or GitHub connection
Azure Repos (push the monorepo):
```bash
az repos create --name <repo-name>
git remote add azdo https://<organization>@dev.azure.com/<organization>/<project>/_git/<repo-name>
git push azdo devel:devel
```
or import: `az repos import create --git-source-url https://github.com/<github-owner>/<github-repo>.git --repository <repo-name>` (private source: add `--requires-authorization --user-name <user> --password <pat>`).
GitHub-hosted code instead: Project settings > Service connections > New > GitHub (OAuth or the Azure Pipelines GitHub App), then use `--repository <github-owner>/<github-repo> --repository-type github --service-connection <github-service-connection>` in step 6.
Why: the pipeline needs the source, and the trigger fires on pushes to the branch.

## 3. Service connection `azconnection` (Azure Resource Manager, workload identity federation)
The YAML hard-codes `azureSubscription: azconnection`, so the name must match unless you edit the YAML (`<service-connection-name>` = `azconnection`).
Portal (recommended; creates the app registration + federated credential automatically): Project settings > Service connections > New service connection > Azure Resource Manager > App registration (automatic) with Workload identity federation > scope Subscription `<subscription-id>`, resource group left empty (azd creates resource groups), name `azconnection`, uncheck "Grant access permission to all pipelines" if you want an approval gate.
CLI equivalent (manual WIF; the exact flags/JSON are inferred, not from the course):
```bash
az ad app create --display-name <app-registration-name> --query appId -o tsv        # <app-client-id>
az ad sp create --id <app-client-id>
az role assignment create --assignee <app-client-id> --role Contributor --scope /subscriptions/<subscription-id>
az role assignment create --assignee <app-client-id> --role "User Access Administrator" --scope /subscriptions/<subscription-id>
# issuer and subject are shown on the service connection page after you start the manual WIF flow in the portal:
az ad app federated-credential create --id <app-client-id> --parameters '{"name":"<credential-name>","issuer":"<wif-issuer-url>","subject":"<wif-subject>","audiences":["api://AzureADTokenExchange"]}'
az devops service-endpoint create --service-endpoint-configuration <endpoint.json>   # type azurerm, authorization scheme WorkloadIdentityFederation; see portal for field names
```
Why Contributor + User Access Administrator: `azd provision` creates resources and the role assignments that Aspire/Bicep emits (Key Vault, Service Bus, ACR pull, managed identities); Contributor alone fails on role assignments. (Inferred; Owner on the subscription also works.)
Check: `az devops service-endpoint list --query "[].{name:name,ready:isReady}" -o table` shows `azconnection` ready. Consumed by both `AzureCLI@2` tasks (`azureSubscription`).

## 4. azd tasks in the organization (`setup-azd@1`)
Install the "Azure Developer CLI" extension in the organization from the Visual Studio Marketplace (publisher Microsoft, extension "Azure Developer CLI Tasks"; Organization settings > Extensions > Browse marketplace > Get it free, requires org Owner/Project Collection Administrator). Portal only; no `az` command for marketplace installs.
Fallback if you cannot install it: in the YAML replace the `setup-azd@1` step with the commented block already in the file:
```yaml
      - task: Bash@3
        displayName: Install azd
        inputs:
          targetType: 'inline'
          script: |
            curl -fsSL https://aka.ms/install-azd.sh | bash
```
The following `pwsh: azd config set auth.useAzCliAuth "true"` step makes azd use the `az` session that `AzureCLI@2` opens through `azconnection`, so no `azd auth login` is needed.

## 5. Variables and secrets
Variables consumed by both AzureCLI@2 tasks via `env:` (`$(NAME)` macro syntax), then by azd/AppHost parameters:

| Variable | Value | Consumed as |
|---|---|---|
| AZURE_SUBSCRIPTION_ID | `<subscription-id>` | azd target subscription |
| AZURE_LOCATION | `<location>` | azd region |
| AZURE_ENV_NAME | `<azd-env-name>` | azd environment name (drives resource names; azd creates the env on the fly from these) |
| AZURE_CHECKOUT_RETURN_URL | `https://<frontend-container-app-fqdn>/order-created` | AppHost `CheckoutReturnUrl` -> `Stripe__CheckoutReturnUrl` |
| AZURE_ENTRA_VALID_AUDIENCE | `<entra-api-client-id>` | `Authentication__Schemes__Entra__ValidAudience` |
| AZURE_ENTRA_AUTHORITY | `https://<entra-tenant-id>.ciamlogin.com/<entra-tenant-id>/v2.0` | `Authentication__Schemes__Entra__Authority` |
| AZURE_ALLOWED_ORIGINS | `https://<frontend-container-app-fqdn>` (first run `http://localhost:5173`) | `AllowedOrigins` (CORS) |
| AZURE_STRIPE_API_KEY (SECRET) | `<stripe-test-secret-key>` (sk_test_ only) | AppHost secret `StripeApiKey` -> Key Vault `Stripe--SecretKey` |

(Mapping of `AZURE_<PARAM>` to AppHost parameters follows azd's convention for non-interactive runs; consistent with the earlier runbook's prompt table, not verified end to end.)
```bash
az pipelines variable-group create --name <variable-group-name> --authorize true --variables AZURE_SUBSCRIPTION_ID=<subscription-id> AZURE_LOCATION=<location> AZURE_ENV_NAME=<azd-env-name> AZURE_CHECKOUT_RETURN_URL=<checkout-return-url> AZURE_ENTRA_VALID_AUDIENCE=<entra-api-client-id> AZURE_ENTRA_AUTHORITY=<entra-authority> AZURE_ALLOWED_ORIGINS=<allowed-origins>
az pipelines variable-group variable create --group-id <variable-group-id> --name AZURE_STRIPE_API_KEY --value "<stripe-test-secret-key>" --secret true
```
Or, simpler and matching the YAML as-is (no `variables:` block): pipeline-level variables after step 6:
```bash
az pipelines variable create --pipeline-name <pipeline-name> --name AZURE_SUBSCRIPTION_ID --value <subscription-id>
az pipelines variable create --pipeline-name <pipeline-name> --name AZURE_STRIPE_API_KEY --value "<stripe-test-secret-key>" --secret true
```
Repeat for the other variables. Using the variable group requires adding `variables: - group: <variable-group-name>` to the YAML (not in the file today). Key Vault option: Pipelines > Library > variable group > "Link secrets from an Azure Key Vault" (needs `azconnection` to have `Key Vault Secrets User`/get+list on `<key-vault-name>`); also needs the `variables: - group:` edit.
Important: secret variables are NOT exposed to scripts automatically; the YAML maps `AZURE_STRIPE_API_KEY: $(AZURE_STRIPE_API_KEY)` in `env:`, which is why both tasks list it. Never echo it.

## 6. Create the pipeline
```bash
az pipelines create --name <pipeline-name> --repository <repo-name> --repository-type tfsgit \
  --branch devel --yml-path backend/.azdo/pipelines/azure-dev.yml --skip-first-run true
```
Why: registers the YAML without queuing a run before variables exist. (`--yml-path` is `.azdo/pipelines/azure-dev.yml` under Option B.) Authorize `azconnection` for the pipeline on first run (Permit prompt) if not granted to all pipelines.

## 7. Triggers, environments, approvals
- Trigger: pushes to `devel` that touch `backend/*` (no PR trigger, no schedules).
- The YAML uses plain jobs, no `environment:` or `deployment:` jobs, so there are no environment approvals. To add a gate: Pipelines > Environments > New `<environment-name>`, Approvals and checks > Approvals, and change `Deploy` to a `deployment` job with `environment: <environment-name>`; or require approval on the `azconnection` service connection (Approvals and checks) without touching the YAML.

## 8. Free parallel-jobs grant
New organizations have 0 free Microsoft-hosted parallel jobs. Request: https://aka.ms/azpipelines-parallelism-request (form: organization name `<organization>`, email, public/private project). Approval takes a few business days. Alternative: buy 1 Microsoft-hosted parallel job under Organization settings > Billing / Parallel jobs. Without it the first run stays "Queued: no hosted parallelism". Note `strategy: parallel: 2` needs 2 parallel jobs to run both slices at once; with 1 they run one after another (still correct).

## 9. Run and verify
```bash
az pipelines run --name <pipeline-name> --branch devel
az pipelines runs list --pipeline-name <pipeline-name> --top 1 -o table
az pipelines runs show --id <run-id> --query "{status:status,result:result}"
```
Expected, per job:
1. Build: .NET 8, `build`, `Run unit tests` (`DotNetCoreCLI@2 test --no-build` on `GameStore.Api.UnitTests`, no Docker, results published to the run's Tests tab; a failure stops the run before ParallelTesting), artifacts `TestBinaries` and `TestScripts`.
2. ParallelTesting (2 copies, `checkout: none`): downloads artifacts, lists tests, `create_slicing_filter_condition.sh` logs `Total agents: 2`, `Agent number: 1|2`, `Target tests:`; step `Run tests` runs `dotnet .../GameStore.IntegrationTests.dll --filter-method $(targetTestsFilter)` (xunit v3 runner executable, not `dotnet test`). Testcontainers starts Postgres etc. through Docker, preinstalled on `ubuntu-latest`. Together the slices cover 23 tests.
3. Deploy: `azd provision --no-prompt` then `azd deploy --no-prompt` through `azconnection`; then
```bash
az containerapp list -g <resource-group> -o table
curl -i https://<container-app-fqdn>/health/ready
```
Common failures: `azconnection` not authorized (Permit on run page); missing variable (`azd` prompts despite `--no-prompt`, fails); role assignment 403 (add User Access Administrator, step 3); no `azure.yaml` (the `workingDirectory: backend` input on the azd tasks, see the layout fix); `setup-azd@1` unknown task (step 4); `frontdoor.bicep` case mismatch.
Clean up: `azd down --force --purge` from `backend/` with the same env (local `azd env` or `AZURE_ENV_NAME`), then delete the project if disposable: `az devops project delete --id <project-id> --yes`.

## 10. Where each produced setting ends up
- `azconnection` -> `azureSubscription` in both AzureCLI@2 tasks.
- Pipeline variables/secret -> `env:` of the two azd tasks -> azd env values/AppHost parameters -> Container Apps env vars and Key Vault (`Stripe--SecretKey`).
- `targetTestsFilter` (runtime variable) -> `Run tests` step.
- azd state is created inside the agent per run (`.azure/` is gitignored); the same `AZURE_ENV_NAME` + subscription + location reuses the same resources.

## Not verifiable from repo/handouts
- Handouts.pdf is not in the course folder (only `Backend_Start` + zip); README/next-steps are the generic azd ones. Everything here is from the pipeline YAML, `azure.yaml`, the slicing script and general Azure DevOps knowledge.
- Not verified: `az devops`/`az pipelines`/`az ad` flags and the service-endpoint JSON, marketplace name of the azd extension, parallelism request URL/turnaround, role requirements of `azconnection`, `AZURE_*` to AppHost parameter mapping, Key Vault-linked variable group, the exact test count per slice, success of `azd` non-interactive provisioning, and the pipeline run itself (the YAML edits are applied and lint-checked locally, but the pipeline has not been run).
