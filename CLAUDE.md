# CLAUDE.md

Guidance for Claude Code when working in this repository. See [README.md](README.md) for setup and how to run the app, and [docs/architecture.md](docs/architecture.md) for the system design and diagrams. Keep the architecture document in sync when you change flows, endpoints, the data model, authorization or the pipeline (diagrams are Mermaid; check that they still parse).

## What this repo is

GameStore: an ASP.NET Core API + worker (`backend/`) orchestrated by .NET Aspire, and a React/Vite front end (`frontend/`). The code follows the .NET Academy .NET 8 bootcamp. Each course's final tree was copied in verbatim ("replace, don't port"), so the code deliberately keeps the course's naming (`GameStore.*`), structure and style.

## Documentation upkeep

Documentation is part of the change, not a follow-up. The index is [docs/README.md](docs/README.md).

- New tool, service or concept (a CLI, an Azure service, a pattern): add a row to [docs/glossary.md](docs/glossary.md) saying what it is and where the project uses it. Explain it in plain words; do not assume the reader knows it.
- Changed flow, endpoint, data model, authorization, topology or pipeline: update [docs/architecture.md](docs/architecture.md).
- Changed how to run, test or deploy: update `README.md` and [docs/deployment.md](docs/deployment.md).
- New or changed cloud procedure: edit or add a runbook in `docs/runbooks/` (placeholders only) and list it in [docs/README.md](docs/README.md).
- When a user asks "what is X?" about something the project uses and the glossary lacks it, answer and then add it.

## Commands

```bash
dotnet build backend/Backend.sln
dotnet test backend/tests/GameStore.Api.UnitTests     # fast, no Docker (82 tests, 1 skipped)
dotnet test backend/Backend.sln                       # + 23 integration tests, needs Docker running
dotnet run --project backend/src/GameStore.AppHost --launch-profile http   # whole backend stack
cd frontend/GameStore.Frontend && npm ci && npm run dev && npm run build && npm run lint
yamllint -d '{extends: relaxed, rules: {new-lines: disable, line-length: disable}}' backend/.azdo/pipelines/azure-dev.yml
```

## Conventions

- Branches: `feature/<name>` or `fix/<name>`; PRs target `devel` and follow `PULL_REQUEST_TEMPLATE.md` (Issue, Solution, Test cases, UI changes).
- Commits: `feat: ...` or `fix: ...`, one sentence, imperative past tense ("Added ...").
- Never add `Co-Authored-By` trailers or "Generated with Claude Code" footers to commits or PR descriptions. The repository owner is the only author.
- Prefer a new commit over `git commit --amend` once a branch is pushed; never force-push unless asked.
- Merging: the owner does not merge by hand (decision of 2026-10-07). The orchestrator merges a PR into `devel` (`gh pr merge <n> --merge`, then delete the local branch) only when every required reviewer agent has returned an explicit `VERDICT: APPROVE` for the current head commit and CI is green or no checks exist. Required reviewers: security and quality for code PRs (plus the course reviewer for course PRs); quality and security for docs PRs. A changed head after approval means re-review; a missing, failing or unclear verdict means no merge. Never merge to `main`.
- Still ask the owner first for: cloud spend or provisioning, deleting remote branches or data, merges to `main`, anything outside the repository, bot checks or CAPTCHAs, logins and signing prompts.
- Never commit secrets or cloud identifiers: Azure subscription/tenant/resource IDs, ACR names, Container App FQDNs, connection strings, Stripe keys (`sk_`, `pk_`, `whsec_`). Committed config holds `[... HERE]` or `<placeholder>` values; real values go in .NET user-secrets, shell environment variables or git-ignored files (`.env.local`).
- Do not provision cloud resources. Cloud steps live in `docs/runbooks/` as parameterised commands; extend a runbook instead of running the commands. The ordered Azure deployment checklist is `docs/deployment.md`; `scripts/deploy-preflight.sh` is read-only (checks tools, logins and secret key prefixes, prints no subscription names, IDs or secret values) and is safe to run. Only run `azd up`, `azd provision`, `azd deploy` or `azd down` when the repository owner explicitly asks.
- Deployment: not part of normal work. If it ever happens it is triggered only by the owner merging `devel` into `main` (the Azure DevOps pipeline triggers on `main` only, with `pr: none` and a Deploy condition on `refs/heads/main`); agents never merge to `main` and never run azd up/provision/deploy/down.
- Stripe is test mode only. A live key (`sk_live_`) must never be used or stored.

## Editing course-derived code

- Keep changes minimal and in the style of the surrounding code; do not restyle or refactor course code unless asked.
- Many files use CRLF line endings (csproj, sln, YAML, C#). Preserve a file's existing line endings and trailing newline when editing; check `git diff --stat` for whole-file churn.
- Deviations from the course that are intentional and must survive any future replacement of `backend/` with a course tree:
  1. `backend/src/GameStore.AppHost/AppHost.cs`: the Stripe webhook secret path uses `Path.GetFullPath(Path.Combine(builder.AppHostDirectory, ..., ".stripe", "webhook_secret.txt"))` (the course used Windows `..\\..` separators).
  2. `backend/src/GameStore.Api/Shared/Stripe/StripeEventFactory.cs`: `ConstructEvent(..., throwOnApiVersionMismatch: false)`, because Stripe test accounts forward events in their own default API version.
  3. `backend/src/GameStore.AppHost/appsettings.json`: `CheckoutReturnUrl` is `http://localhost:5173/order-created`.
  4. `backend/README.md` keeps the scrubbed placeholders (`<acr-name>`, `<subscription-id>`, `<container-app-fqdn>`); the course versions contain real IDs. Never copy a course `.azure/` folder.
  5. `backend/tests/scripts/create_slicing_filter_condition.sh` uses LF line endings (the course ships CRLF, which breaks under bash).
  6. `backend/src/GameStore.Api/GameStore.Api.csproj` exposes internals to `GameStore.IntegrationTests` and `GameStore.Api.UnitTests`.
  7. `backend/.azdo/pipelines/azure-dev.yml` is adapted to the monorepo (trigger `main` only, filtered to `paths: backend/*`, `pr: none`, and a Deploy job condition on `refs/heads/main`; `backend/Backend.sln` and `backend/tests/...` paths, `workingDirectory: backend` on both `AzureCLI@2` azd tasks); the course file assumes the solution at the repository root and trigger `main`.
  8. `frontend/GameStore.Frontend.AppHost`, `frontend/azure.yaml` and `frontend/React-Frontend.sln` were removed on the owner's decision (LRN-283); the frontend runs with npm (`frontend/GameStore.Frontend/.env.example` documents the `VITE_*` settings). Do not bring them back when replacing `frontend/` with a course tree.
- The unit test project (`backend/tests/GameStore.Api.UnitTests`) is original work, not a course tree: xUnit 2.4.2, FluentAssertions 6.12.0 (the course's versions), NSubstitute, Moq, EF Core InMemory. Keep tests deterministic and Docker-free; Docker-dependent tests belong in `GameStore.IntegrationTests`.

## Local-only files

`.claude/`, `courses/` (copyrighted course material), `docs/superpowers/` (specs and plans) and `backend/.stripe/` (Stripe webhook secret written by the AppHost) are git-ignored. They are not in a fresh clone and must not be committed.

## Known issues (left as in the course)

- NU1902 vulnerability warnings for `OpenTelemetry.Exporter.OpenTelemetryProtocol` appear on every build.
- `Aspire.Hosting.Azure.ApplicationInsights` 13.0.0 is mixed with other Aspire packages at 9.5.2.
