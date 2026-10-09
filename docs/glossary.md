# Glossary: tools and concepts

Short explanations of the tools, services and ideas used in this project, with where each one shows up in the repository. For how the pieces fit together see [architecture.md](architecture.md); for running and deploying see the [README](../README.md) and [deployment.md](deployment.md).

## Contents

- [Build, run and deploy tools](#build-run-and-deploy-tools)
- [Azure services](#azure-services)
- [Local development services](#local-development-services)
- [Identity and payments](#identity-and-payments)
- [Testing tools](#testing-tools)
- [Design concepts](#design-concepts)
- [Project conventions](#project-conventions)

## Build, run and deploy tools

| Term | What it is | In this project |
| --- | --- | --- |
| **.NET Aspire** | A .NET framework for composing a distributed app from code. One "AppHost" project declares the services and their dependencies; Aspire starts them, injects connection strings, waits for dependencies and shows a dashboard. | `backend/src/GameStore.AppHost` (declares API, worker, databases, emulators, Keycloak, Stripe CLI). `GameStore.ServiceDefaults` adds shared health checks, OpenTelemetry and resilience. Dashboard at http://localhost:15054. |
| **AppHost** | The Aspire orchestration project. In **run mode** it starts containers and .NET projects on your machine; in **publish mode** it describes the same app for Azure. | Keycloak, the Stripe CLI and the emulators exist only in run mode; Key Vault, Front Door and Application Insights only in publish mode. |
| **`dotnet user-secrets`** | A .NET feature that stores secrets in your user profile (`~/.microsoft/usersecrets/<id>/secrets.json`), outside the repository. | Stripe keys and generated passwords. See [Secrets](../README.md#secrets). |
| **`azd` (Azure Developer CLI)** | A Microsoft CLI that provisions Azure infrastructure from a project description and deploys the code, in one flow. Not the same as `az`. Commands: `azd auth login`, `azd env new`, `azd up` (provision and deploy), `azd deploy` (code only), `azd down` (delete everything it created). | `backend/azure.yaml` points `azd` at the AppHost; `frontend/azure.yaml` does the same for the frontend. Environment settings live in the git-ignored `backend/.azure/`. See [deployment.md](deployment.md). |
| **`az` (Azure CLI)** | The general Azure command-line tool for managing individual resources, logins and subscriptions. | Used in the runbooks for steps `azd` does not cover (Key Vault secrets, role assignments, logs). |
| **Bicep** | Azure's language for describing infrastructure. `azd` generates it from the AppHost; one file is hand-written. | `backend/src/GameStore.AppHost/bicep/frontdoor.bicep` (CDN in front of blob storage). |
| **Docker / container / volume** | A container is an isolated process with its own filesystem; a volume keeps data after the container is removed. | Aspire runs PostgreSQL, Azurite, Keycloak and others as containers with persistent lifetime, so they keep running and keep data after the AppHost stops. |
| **SDK container publishing** | `dotnet publish /t:PublishContainer` builds an image without a Dockerfile. | The API image `gamestore-api` (course 2). The frontend has a Dockerfile (nginx). |
| **Vite** | The frontend dev server and build tool. `VITE_*` variables are baked into the bundle at build time. | `frontend/GameStore.Frontend`; dev server on http://localhost:5173. |
| **Azure DevOps pipeline** | CI/CD defined in YAML: build, test, deploy. | `backend/.azdo/pipelines/azure-dev.yml`; see [azure-devops-cicd.md](runbooks/azure-devops-cicd.md). |
| **`yamllint`** | A linter for YAML files. | Used to check the pipeline file; see the [README](../README.md#tests). |
| **Mermaid** | Text-based diagrams that GitHub renders in Markdown. | All diagrams in `docs/`. |

## Azure services

| Term | What it is | In this project |
| --- | --- | --- |
| **Container Apps** | Managed hosting for containers that scales on demand, including down to zero. | The API (0 to 10 replicas), the worker and the frontend in Azure. |
| **Azure Database for PostgreSQL, Flexible Server** | Managed PostgreSQL. | The production database; locally a `postgres` container. |
| **Azure Storage (Blob)** | Object storage for files. | Game cover images; locally Azurite. |
| **Azure Service Bus** | A managed message broker with queues, duplicate detection and dead-letter queues. | The `orders` queue between the API and the worker; locally the Service Bus emulator. |
| **Key Vault** | A managed store for secrets. | `Stripe--SecretKey` in production. |
| **Managed identity / `DefaultAzureCredential`** | An Azure-assigned identity for an app, so it can call other Azure services without passwords or keys. `DefaultAzureCredential` picks the right credential automatically (managed identity in Azure, your login locally). | API access to PostgreSQL, Storage, Service Bus and Key Vault (`AZURE_CLIENT_ID` selects the identity). |
| **Azure Front Door** | A global entry point that can also act as a CDN. | Caches game images in front of blob storage; the API rewrites image URLs to its host (`AZURE_FRONTDOOR_HOSTNAME`). |
| **Application Insights** | Azure Monitor's application telemetry service. | Optional; active only when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set. See [troubleshooting-azure.md](runbooks/troubleshooting-azure.md). |
| **OpenTelemetry** | A vendor-neutral standard for traces, metrics and logs. | Configured in `GameStore.ServiceDefaults`; shown in the Aspire dashboard, exportable to Application Insights. |
| **Microsoft Entra ID (external tenant)** | Microsoft's identity service, here in its customer-facing "external" flavour (sign-in domain `ciamlogin.com`). | Production sign-in. The API validates Entra tokens; the SPA signs users in. |

## Local development services

| Term | What it is | In this project |
| --- | --- | --- |
| **Azurite** | A local emulator of Azure Blob storage. | Image uploads when running locally and in integration tests. |
| **Service Bus emulator** | Microsoft's local emulator of Service Bus (it also starts a SQL Server container). | The `orders` queue locally and in integration tests. |
| **Keycloak** | An open-source identity provider. | Local sign-in instead of Entra; the realm in `backend/localinfra/gamestore-realm.json` is imported by the AppHost. It ships clients, the `gamestore_api.all` scope and an `Admin` role, but no users. |
| **pgAdmin** | A web UI for PostgreSQL. | Started by the AppHost on http://localhost:5050. |

## Identity and payments

| Term | What it is | In this project |
| --- | --- | --- |
| **OIDC / JWT / bearer token** | OpenID Connect is the sign-in protocol; it yields a signed JSON Web Token (access token) that the frontend sends as `Authorization: Bearer <token>`. | `oidc-client-ts` in the frontend; JWT validation in the API (`Shared/Authorization`). |
| **Scope / role / policy** | A scope says what an app may call (`gamestore_api.all`); a role says what a user may do (`Admin`); a policy combines requirements. | `UserAccess` and `AdminAccess` policies; see [Security](architecture.md#6-security). |
| **Stripe test mode** | Stripe's sandbox where no real money moves. Keys start with `sk_test_` and `pk_test_`. | Only test mode is ever used. Test card: `4242 4242 4242 4242`. |
| **Secret key vs publishable key** | `sk_test_...` is private and used by the API; `pk_test_...` is public and ships in the frontend bundle. | `Parameters:StripeApiKey` (AppHost) and `Parameters:StripePublishableKey` (frontend AppHost). |
| **Checkout session** | A Stripe object describing one payment attempt. | Created by `POST /payments/checkout`; the frontend completes it with Stripe.js. |
| **Webhook** | An HTTP call from Stripe to your API when something happens (here `checkout.session.completed`). It is signed so the API can verify it. | `POST /payments/stripe-webhook`; the signing secret is `Stripe:EndpointSecret`. |
| **Stripe CLI** | Stripe's command-line tool that can forward webhooks to a local server. | Runs as the container `stripe/stripe-cli` (`stripeListener`) and writes the webhook secret to `backend/.stripe/webhook_secret.txt`. |
| **Idempotency key** | A value that makes repeating a request safe: the same key returns the same result instead of a duplicate. | `cs-create-{orderId}` on Stripe calls; also the client's `operationId`. |

## Testing tools

| Term | What it is | In this project |
| --- | --- | --- |
| **xUnit** | The .NET test framework. | Both test projects. |
| **FluentAssertions** | Readable assertions (`result.Should().Be(...)`). | Unit tests (version 6.12.0, the course's version). |
| **NSubstitute / Moq** | Libraries that create fake collaborators (stubs return values; mocks also verify calls). | Unit tests use both; Moq in `OutboxProcessorMoqTests`. |
| **Testcontainers** | A library that starts real throwaway containers from test code. | Integration tests start PostgreSQL, Azurite and the Service Bus emulator; the Ryuk container removes them afterwards. |
| **`WebApplicationFactory`** | Hosts the real API in-process for tests. | `GameStoreWebApplicationFactory` with a fake authentication handler. |
| **EF Core InMemory** | An in-memory database provider for unit tests. | `OrderCreator`, `BasketItemsProvider` and outbox unit tests. |
| **Mutation check** | Temporarily breaking production code to prove a test can fail. | Done once per subject when the unit tests were written. |
| **Test slicing** | Splitting the test list across parallel pipeline agents. | `backend/tests/scripts/create_slicing_filter_condition.sh`. |

## Design concepts

| Term | What it is | In this project |
| --- | --- | --- |
| **Vertical slice** | Organising code by use case (endpoint, DTOs, handler together) instead of by technical layer. | `GameStore.Api/Features/*`. |
| **Transactional outbox** | Writing the message to publish in the same database transaction as the state change, and publishing it afterwards from a background loop. | `OutboxMessages` table and `OutboxProcessor`; see [architecture.md](architecture.md#43-outbox-and-message-handling). |
| **At-least-once delivery and duplicate detection** | A message may arrive more than once, so receivers must tolerate duplicates; the queue can also drop repeats by message id. | Message id = Stripe PaymentIntent id; `orders` queue has duplicate detection on. |
| **Dead-letter queue (DLQ)** | A side queue for messages that cannot be processed. | Messages without a `MessageType` go to the DLQ, which `OrdersDeadLetterQueueProcessor` logs. |
| **Eventual consistency** | The state settles shortly after, not within the request. | A paid order is `Processing` until the worker marks it `Completed`. |
| **Run mode vs publish mode** | Aspire's two ways of running the AppHost: local development vs generating Azure resources. | See [AppHost](#build-run-and-deploy-tools). |
| **Design tokens** | Named values for colours, fonts, corner radius and shadows, kept in one place (CSS custom properties) so the whole UI changes together. | `frontend/GameStore.Frontend/src/styles/lootlark-tokens.css` holds the Lootlark tokens and maps them onto Bootstrap's variables. The accent yellow is a fill only; text uses the darker `--brand-accent-text`. |
| **`data-bs-theme`** | A Bootstrap 5.3 HTML attribute (`light` or `dark`) that switches every Bootstrap colour variable below that element to the matching theme. | The tokens file defines both a light and a dark set. The app is light only today (the old dark navbar attribute was removed); setting `data-bs-theme="dark"` on `<html>` would switch it. |
| **Self-hosted fonts (Fontsource)** | Fontsource packages font files on npm, so the app serves them itself instead of loading them from a third-party font site. Both fonts used here are under the SIL Open Font License. | `@fontsource-variable/baloo-2` (headings) and `@fontsource-variable/nunito-sans` (text), imported in `main.tsx`, with `font-display: swap` so text shows at once in a fallback font. |
| **Contrast ratio (WCAG)** | A number from 1 to 21 comparing the brightness of text and its background. Text needs at least 4.5; focus outlines and large UI parts need at least 3. | The brand pairs are listed in [branding/DIRECTIONS.md](branding/DIRECTIONS.md); the front end uses a solid 2px focus outline checked against it. |

## Project conventions

| Term | What it is | In this project |
| --- | --- | --- |
| **Course-derived code ("Approach C")** | Each .NET Academy course's final tree was copied in verbatim instead of being ported or restyled. | Keeps the repo comparable to the course; intentional deviations are listed in [CLAUDE.md](../CLAUDE.md). |
| **Runbook** | A parameterised, step-by-step procedure for something that was not executed here (cloud setup). | [docs/runbooks/](runbooks/). Placeholders like `<tenant-id>` are replaced with your own values. |
| **Placeholder (`[... HERE]` / `<name>`)** | A stand-in for a real value that must never be committed. | Committed config and docs. Real values go in user-secrets, shell variables or git-ignored files. |
| **Local-only files** | Files git ignores. | `.claude/`, `courses/`, `docs/superpowers/`, `backend/.stripe/`, `backend/.azure/`, `.env.local`. |
| **Merge gate** | The rule that decides when a pull request may be merged without a human clicking merge: every required reviewer agent approved the current head commit and CI is green. | See [Merging in CLAUDE.md](../CLAUDE.md) and the README's Git Workflow. |
