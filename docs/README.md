# Documentation

Where to find what. Start with the [project README](../README.md) to run the app.

| I want to... | Read |
| --- | --- |
| Run the app locally, run the tests, fix a common problem | [README](../README.md) |
| Understand how the system is built (context, topology, flows, data model, security) | [architecture.md](architecture.md) |
| Look up what a tool or concept is (Aspire, `azd`, outbox, Testcontainers, ...) | [glossary.md](glossary.md) |
| Deploy to Azure, in order, and know what it will create | [deployment.md](deployment.md) |
| Run one specific cloud procedure in detail | [runbooks/](runbooks/) |
| Work on the code with Claude Code (commands, conventions, intentional deviations) | [../CLAUDE.md](../CLAUDE.md) |
| See the product name, brand direction, logo and design tokens | [branding/SUMMARY.md](branding/SUMMARY.md) |
| Open a pull request | [../PULL_REQUEST_TEMPLATE.md](../PULL_REQUEST_TEMPLATE.md) |

## Runbooks

Cloud procedures from the courses that were intentionally not executed. Commands are parameterised with `<placeholders>`.

| Runbook | Covers |
| --- | --- |
| [azure-for-dotnet-developers.md](runbooks/azure-for-dotnet-developers.md) | Local Keycloak setup, Entra, App Service, Storage, Front Door, Managed Identities, PostgreSQL, Key Vault, static web app. |
| [containers-and-aspire.md](runbooks/containers-and-aspire.md) | Local run, Container Registry, Container Apps, `azd up`, Bicep Front Door, front-end deployment. |
| [payments-queues-workers.md](runbooks/payments-queues-workers.md) | Local payment flow, Stripe webhook, Service Bus, Key Vault secrets, deploys, deviations from the course. |
| [azure-devops-cicd.md](runbooks/azure-devops-cicd.md) | Local validation, Azure DevOps project, service connection, pipeline, parallel jobs. |
| [troubleshooting-azure.md](runbooks/troubleshooting-azure.md) | Application Insights, load tests, diagnosing a slow endpoint. |

## Other documents in the repository

| File | Notes |
| --- | --- |
| [backend/README.md](../backend/README.md) | Course notes for container commands. Real IDs are replaced by placeholders. |
| [backend/next-steps.md](../backend/next-steps.md) | The generic `azd` guide that ships with the course tree. |
| [frontend/GameStore.Frontend/README.md](../frontend/GameStore.Frontend/README.md) | Course steps for the React front end. |
| [scripts/deploy-preflight.sh](../scripts/deploy-preflight.sh) | Read-only check of tools, logins and secrets before a deployment. |

## Keeping the documentation current

- Change a flow, endpoint, the data model, authorization, a topology or the pipeline: update [architecture.md](architecture.md) (diagrams are Mermaid; make sure they still render; there are 15 today: 13 in `architecture.md`, 1 in `deployment.md` and 1 in the root `README.md`).
- Introduce a new tool, service or concept: add a row to [glossary.md](glossary.md) with what it is and where it is used.
- Change how to run, test or deploy: update the [README](../README.md) and [deployment.md](deployment.md).
- Add or change a cloud procedure: edit or add a runbook, using placeholders only. Never commit real IDs, keys or secrets.
- Add a document: list it in this index.

`docs/superpowers/` (specs and plans) is local-only and git-ignored.
