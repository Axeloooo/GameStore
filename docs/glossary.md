# Glossary: tools and concepts

Short explanations of the tools, services and ideas used in this project, with where each one shows up in the repository. For how the pieces fit together see [architecture.md](architecture.md); for running the app see [local-development.md](local-development.md).

## Contents

- [Build and run tools](#build-and-run-tools)
- [Continuous integration and commits](#continuous-integration-and-commits)
- [Azure services and SDKs](#azure-services-and-sdks)
- [Local development services](#local-development-services)
- [Identity and payments](#identity-and-payments)
- [Testing tools](#testing-tools)
- [Design concepts](#design-concepts)
- [Project conventions](#project-conventions)

## Build and run tools

| Term | What it is | In this project |
| --- | --- | --- |
| **.NET Aspire** | A .NET framework for composing a distributed app from code. One "AppHost" project declares the services and their dependencies; Aspire starts them, injects connection strings, waits for dependencies and shows a dashboard. | `backend/src/GameStore.AppHost` (declares API, worker, databases, emulators, Keycloak, Stripe CLI). `GameStore.ServiceDefaults` adds shared health checks, OpenTelemetry and resilience. Dashboard at http://localhost:15054. |
| **AppHost** | The Aspire orchestration project. In **run mode** (`dotnet run`) it starts containers and .NET projects on your machine. | `backend/src/GameStore.AppHost/AppHost.cs`. This project only uses run mode; Keycloak and the Stripe CLI are added only there. |
| **`dotnet user-secrets`** | A .NET feature that stores secrets in your user profile (`~/.microsoft/usersecrets/<id>/secrets.json`), outside the repository. | The Stripe secret key, the Entra parameters and generated passwords. See [Secrets and parameters](local-development.md#secrets-and-parameters). |
| **Docker / container / volume** | A container is an isolated process with its own filesystem; a volume keeps data after the container is removed. | Aspire runs PostgreSQL, Azurite, Keycloak and others as containers with persistent lifetime, so they keep running and keep data after the AppHost stops. |
| **SDK container publishing** | `dotnet publish /t:PublishContainer` builds a container image without a Dockerfile. | `GameStore.Api.csproj` still names the image `gamestore-api` (`ContainerRepository`, from the course); the local run does not build images. |
| **Vite** | The frontend dev server and build tool. `VITE_*` variables are baked into the bundle at build time. | `frontend`; dev server on http://localhost:5173; settings in `.env.local` (see `.env.example`). |
| **Mermaid** | Text-based diagrams that GitHub renders in Markdown. | The diagrams in `docs/architecture.md` and the root `README.md`. |
| **nvm** | nvm (Node Version Manager) installs and switches between Node.js versions on your machine. | Optional: use it to run Node 22, the version CI uses (see [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)). |

## Continuous integration and commits

| Term | What it is | In this project |
| --- | --- | --- |
| **Continuous integration (CI)** | Building and testing every change automatically on a server, so a broken change is caught before it is merged. | Runs on every pull request to `devel` or `main` and every push to them. |
| **GitHub Actions** | GitHub's built-in automation service. A workflow is a YAML file in `.github/workflows/` made of jobs that run on GitHub-hosted machines (runners). | [`.github/workflows/ci.yml`](../.github/workflows/ci.yml): backend build and unit tests, backend integration tests, front end lint and build, and commit message checks. |
| **Conventional Commits** | A convention for commit messages: a type such as `feat`, `fix` or `docs`, a colon and a short description. | Messages look like `docs: Rewrote the README for local development`: one sentence, imperative past tense, at most 100 characters, no trailing full stop. |
| **commitlint** | A tool that checks commit messages against a set of rules. | CI runs it on every pull request commit with the rules in [`.commitlintrc.json`](../.commitlintrc.json) (allowed types, lower-case type, header length). |

## Azure services and SDKs

The project runs locally, but the course code uses Azure client libraries, and some of it is written for Azure hosting. Locally these services are replaced by containers or are simply not configured.

| Term | What it is | In this project |
| --- | --- | --- |
| **Azure Database for PostgreSQL, Flexible Server** | Managed PostgreSQL in Azure. | The AppHost declares the database this way but runs it as a local `postgres` container (`RunAsContainer`). |
| **Azure Storage (Blob)** | Object storage for files. | Game cover images, stored in Azurite locally. |
| **Azure Service Bus** | A managed message broker with queues, duplicate detection and dead-letter queues. | The `orders` queue between the API and the worker, provided by the Service Bus emulator locally. |
| **Key Vault** | A managed store for secrets in Azure. | The API reads secrets from a Key Vault only in the `Production` environment (`AddAzureKeyVaultSecrets` in `Program.cs`); the local run is `Development` and uses user-secrets. |
| **Managed identity / `DefaultAzureCredential`** | An Azure-assigned identity for an app, so it can call other Azure services without passwords or keys. `DefaultAzureCredential` picks the right credential automatically (managed identity in Azure, your login locally). | Passed by the API and the worker to the PostgreSQL, Storage and Service Bus clients (`AZURE_CLIENT_ID` selects the identity). Locally the AppHost supplies emulator connection strings instead. |
| **Azure Front Door** | A global entry point that can also act as a CDN. | `CdnUrlTransformer` in the API rewrites image URLs to a Front Door host only when `AZURE_FRONTDOOR_HOSTNAME` is set; locally it is not, so blob URLs are returned unchanged. |
| **Application Insights** | Azure Monitor's application telemetry service. | `GameStore.ServiceDefaults` exports telemetry to it only when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set; the local run does not set it. |
| **OpenTelemetry** | A vendor-neutral standard for traces, metrics and logs. | Configured in `GameStore.ServiceDefaults`; shown in the Aspire dashboard. |
| **Microsoft Entra ID (external tenant)** | Microsoft's identity service, here in its customer-facing "external" flavour (sign-in domain `ciamlogin.com`). | Optional alternative to Keycloak with your own tenant. The API validates Entra tokens (issuer on `ciamlogin.com`); the SPA signs users in when `VITE_IDENTITY_PROVIDER=entra`. See [local-development.md](local-development.md#using-microsoft-entra-id-instead-of-keycloak). |

## Local development services

| Term | What it is | In this project |
| --- | --- | --- |
| **Azurite** | A local emulator of Azure Blob storage. | Image uploads when running locally and in integration tests. |
| **Service Bus emulator** | Microsoft's local emulator of Service Bus (it also starts a SQL Server container). | The `orders` queue locally and in integration tests. |
| **Keycloak** | An open-source identity provider. | The default sign-in; the realm in `backend/localinfra/gamestore-realm.json` is imported by the AppHost. It ships clients, the `gamestore_api.all` scope and an `Admin` role, but no users. |
| **pgAdmin** | A web UI for PostgreSQL. | Started by the AppHost on http://localhost:5050. |

## Identity and payments

| Term | What it is | In this project |
| --- | --- | --- |
| **OIDC / JWT / bearer token** | OpenID Connect is the sign-in protocol; it yields a signed JSON Web Token (access token) that the frontend sends as `Authorization: Bearer <token>`. | `oidc-client-ts` in the frontend; JWT validation in the API (`Shared/Authorization`). |
| **Scope / role / policy** | A scope says what an app may call (`gamestore_api.all`); a role says what a user may do (`Admin`); a policy combines requirements. | `UserAccess` and `AdminAccess` policies; see [Security](architecture.md#6-security). |
| **Stripe test mode** | Stripe's sandbox where no real money moves. Keys start with `sk_test_` and `pk_test_`. | Only test mode is ever used. Test card: `4242 4242 4242 4242`. |
| **Secret key vs publishable key** | `sk_test_...` is private and used by the API; `pk_test_...` is public and ships in the frontend bundle. | `Parameters:StripeApiKey` (backend AppHost) and `VITE_STRIPE_PUBLISHABLE_KEY` (frontend `.env.local`). |
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

## Design concepts

| Term | What it is | In this project |
| --- | --- | --- |
| **Vertical slice** | Organising code by use case (endpoint, DTOs, handler together) instead of by technical layer. | `GameStore.Api/Features/*`. |
| **Transactional outbox** | Writing the message to publish in the same database transaction as the state change, and publishing it afterwards from a background loop. | `OutboxMessages` table and `OutboxProcessor`; see [architecture.md](architecture.md#43-outbox-and-message-handling). |
| **At-least-once delivery and duplicate detection** | A message may arrive more than once, so receivers must tolerate duplicates; the queue can also drop repeats by message id. | Message id = Stripe PaymentIntent id; `orders` queue has duplicate detection on. |
| **Dead-letter queue (DLQ)** | A side queue for messages that cannot be processed. | Messages without a `MessageType` go to the DLQ, which `OrdersDeadLetterQueueProcessor` logs. |
| **Eventual consistency** | The state settles shortly after, not within the request. | A paid order is `Processing` until the worker marks it `Completed`. |
| **Run mode vs publish mode** | Aspire's two ways of using the AppHost: run mode starts the app locally; publish mode describes it for a deployment target. | Only run mode is used; see [AppHost](#build-and-run-tools). |
| **Design tokens** | Named values for colours, fonts, corner radius and shadows, kept in one place (CSS custom properties) so the whole UI changes together. | `frontend/src/styles/lootlark-tokens.css` holds the Lootlark tokens and maps them onto Bootstrap's variables. The accent yellow is a fill only; text uses the darker `--brand-accent-text`. |
| **`data-bs-theme`** | A Bootstrap 5.3 HTML attribute (`light` or `dark`) that switches every Bootstrap colour variable below that element to the matching theme. | The tokens file defines both a light and a dark set. The app is light only today (the old dark navbar attribute was removed); setting `data-bs-theme="dark"` on `<html>` would switch it. |
| **Self-hosted fonts (Fontsource)** | Fontsource packages font files on npm, so the app serves them itself instead of loading them from a third-party font site. Both fonts used here are under the SIL Open Font License. | `@fontsource-variable/baloo-2` (headings) and `@fontsource-variable/nunito-sans` (text), imported in `main.tsx`, with `font-display: swap` so text shows at once in a fallback font. |
| **Contrast ratio (WCAG)** | A number from 1 to 21 comparing the brightness of text and its background. Text needs at least 4.5; focus outlines and large UI parts need at least 3. | The brand pairs are listed in [branding/README.md](branding/README.md); the front end uses a solid 2px focus outline checked against it. |

## Project conventions

| Term | What it is | In this project |
| --- | --- | --- |
| **Course-derived code ("Approach C")** | Each .NET Academy course's final tree was copied in verbatim instead of being ported or restyled. | Keeps the repo comparable to the course; intentional deviations are listed in [CLAUDE.md](../CLAUDE.md). |
| **Placeholder (`[... HERE]` / `<name>`)** | A stand-in for a real value that must never be committed. | Committed config and docs. Real values go in user-secrets, shell variables or git-ignored files. |
| **Local-only files** | Files git ignores. | `.claude/`, `backend/.stripe/`, `frontend/.env.local`. |
| **Merge gate** | The rule that decides when a pull request may be merged without a human clicking merge: every required reviewer agent approved the current head commit and CI is green. | See the merging rules in [CLAUDE.md](../CLAUDE.md). |
