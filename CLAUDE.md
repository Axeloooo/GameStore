# CLAUDE.md

Guidance for Claude Code when working in this repository. See [README.md](README.md) for an overview and the quick start, [docs/local-development.md](docs/local-development.md) for running and troubleshooting locally, and [docs/architecture.md](docs/architecture.md) for the system design.

## What this repo is

GameStore: an ASP.NET Core API + worker (`backend/`) orchestrated locally by .NET Aspire, and a React/Vite front end (`frontend/`, run with npm). The code follows the .NET Academy .NET 8 bootcamp; each course's final tree was copied in verbatim, so the code keeps the course's naming (`GameStore.*`), structure and style. The product brand is **Lootlark** (UI copy, README title, docs; see [docs/branding/README.md](docs/branding/README.md)); the repository, `GameStore.*` namespaces, API routes and the Keycloak realm keep the GameStore name.

The project is never deployed. Nothing in the repository provisions cloud resources and there is no deployment scaffolding. If that ever changes, a deployment happens only when the owner merges `devel` into `main`, which agents never do.

Never run `azd` or `az` provisioning or deployment commands unless the owner explicitly asks.

## Documentation upkeep

Documentation is part of the change, not a follow-up. The index is [docs/README.md](docs/README.md); list every new document there.

- Changed flow, endpoint, data model, authorization, local topology or CI: update [docs/architecture.md](docs/architecture.md). Diagrams are Mermaid; check that they still parse.
- New tool, service or concept: add a row to [docs/glossary.md](docs/glossary.md) saying in plain words what it is and where the project uses it. When a user asks "what is X?" and the glossary lacks it, answer and then add it.
- Changed how to run or test: update [README.md](README.md) and [docs/local-development.md](docs/local-development.md).
- Changed name, logo, palette or voice: update the brand guide [docs/branding/README.md](docs/branding/README.md).

## Commands

```bash
dotnet build backend/Backend.sln
dotnet test backend/tests/GameStore.Api.UnitTests     # fast, no Docker (82 tests: 81 pass, 1 skipped on purpose)
dotnet test backend/Backend.sln                       # + 23 integration tests, needs Docker running
dotnet run --project backend/src/GameStore.AppHost --launch-profile http   # whole backend stack
cd frontend && npm ci && npm run dev                  # also: npm run lint, npm run build, npm run preview
```

## Conventions

- Branches: `feature/<name>`, `fix/<name>`, `refactor/<name>`, `chore/<name>`, `ci/<name>` or `docs/<name>`, cut from `devel`. Releases: `release/YYYY-MM-DD` (the release date), cut from `devel`.
- Commits follow Conventional Commits, enforced by [.commitlintrc.json](.commitlintrc.json) in the CI "Commit messages" job: type `feat`, `fix`, `perf`, `refactor`, `docs`, `test`, `build`, `ci`, `chore`, `style` or `revert`; one sentence, imperative past tense, capitalized subject, no trailing full stop, header at most 100 characters (`feat: Added X`).
- Commits are signed through the owner's 1Password; never disable signing. If signing fails, stop and ask.
- Never add `Co-Authored-By` trailers or "Generated with Claude Code" footers to commits or PR descriptions. The repository owner is the only author.
- Once a branch is pushed, add new commits; never amend or force-push.
- Pull requests target `devel` and use [.github/pull_request_template.md](.github/pull_request_template.md) (Issue, Solution, Test cases, UI changes). Assign `Axeloooo` and comment `@Axeloooo this PR is ready for your review`.
- Reviews: every required reviewer agent ends its report with two lines, `HEAD: <full sha>` then `VERDICT: APPROVE` or `VERDICT: REQUEST_CHANGES`. Required reviewers: quality and security, plus a design reviewer for UI changes.
- Merging into `devel`: the orchestrator merges (`gh pr merge <n> --merge --match-head-commit <sha>`, then deletes the local branch) only when every required reviewer returned `VERDICT: APPROVE` for that exact head and CI is green. A changed head after approval means re-review; a missing, failing or unclear verdict means no merge.
- Releases: agents may create `release/YYYY-MM-DD` from `devel` and open its PR to `main` (same template, assignee and comment). Only the owner merges into `main` (merge commit); agents never push to or merge into `main` and never create or push tags.
- Versions: semantic-release cuts each release on pushes to `main` ([.github/workflows/release.yml](.github/workflows/release.yml), settings in [.releaserc.json](.releaserc.json)): a `vX.Y.Z` tag and a GitHub Release, nothing deployed or published. The bump comes from the commit types, so pick them accurately: `feat` = minor, `fix`/`perf`/`revert` = patch, `!` after the type or a `BREAKING CHANGE:` footer = major; other types release nothing. The owner pushes the baseline tag `v0.1.0` on `main` before the first release; commits from before Conventional Commits were adopted are ignored.
- Ask the owner first for: cloud spend or provisioning, deleting remote branches or data, merges to `main`, anything outside the repository, bot checks or CAPTCHAs, logins and signing prompts.
- Never commit secrets or cloud identifiers: keys, Stripe keys (`sk_`, `pk_`, `whsec_`), connection strings, tenant, client or subscription IDs. Committed config holds `[... HERE]` or `<placeholder>` values; real values go in .NET user-secrets, environment variables or git-ignored files such as `frontend/.env.local`.
- Stripe is test mode only. A live key (`sk_live_`) must never be used or stored.

## Editing course-derived code

- Keep changes minimal and in the style of the surrounding code; do not restyle or refactor course code unless asked.
- Many files use CRLF line endings (csproj, sln, C#). Preserve a file's existing line endings and trailing newline when editing; check `git diff --stat` for whole-file churn.
- Intentional deviations from the course that must survive any future replacement of `backend/` with a course tree:
  1. `backend/src/GameStore.AppHost/AppHost.cs`: the Stripe webhook secret path uses `Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", ".stripe", "webhook_secret.txt"))` (the course used Windows `..\\..` separators).
  2. `backend/src/GameStore.Api/Shared/Stripe/StripeEventFactory.cs`: `ConstructEvent(..., throwOnApiVersionMismatch: false)`, because Stripe test accounts forward events in their own default API version.
  3. `backend/src/GameStore.AppHost/appsettings.json`: `CheckoutReturnUrl` is `http://localhost:5173/order-created`.
  4. `backend/src/GameStore.Api/GameStore.Api.csproj` exposes internals to `GameStore.IntegrationTests` and `GameStore.Api.UnitTests`.
  5. The AppHost is local-only: the course's Azure publish code (Container Apps, Front Door, Key Vault, Application Insights, the `AllowedOrigins` parameter) and its publish-only packages (`Aspire.Hosting.Azure.AppContainers`, `.ApplicationInsights`, `.KeyVault`) were removed. The `Aspire.Hosting.Azure.ServiceBus`, `.Storage` and `.PostgreSQL` packages stay because they run the local emulator and containers (`RunAsEmulator`, `RunAsContainer`). Do not bring back publish code or deployment files (`azure.yaml`, `.azure/`, pipelines, Bicep) from a course tree.
- The unit test project (`backend/tests/GameStore.Api.UnitTests`) is original work, not a course tree: xUnit 2.4.2, FluentAssertions 6.12.0 (the course's versions), NSubstitute, Moq, EF Core InMemory. Keep tests deterministic and Docker-free; Docker-dependent tests belong in `GameStore.IntegrationTests`.

## Local-only files

`.claude/` and `backend/.stripe/` (the Stripe webhook secret written by the AppHost) are git-ignored and must not be committed. Course material (`courses/`) and agent specs or plans (`docs/superpowers/`) are no longer in the repository or ignored; if recreated locally, never commit them.

## Known issues

- NuGet: `dotnet list backend/Backend.sln package --include-transitive --vulnerable` is clean; patched versions of former transitive findings are pinned explicitly in the csproj files (MessagePack, OpenTelemetry.Api, SSH.NET, System.Net.Http, System.Text.RegularExpressions).
- npm: `npm audit` in `frontend/` reports open findings, tracked for a follow-up; do not describe it as clean.
