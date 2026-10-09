# Documentation

Where to find what. Start with the [project README](../README.md) for an overview and the short way to run the app.

| I want to... | Read |
| --- | --- |
| Get an overview, the stack, the quick start and the test commands | [README](../README.md) |
| Run and configure the app locally, or fix a local problem | [local-development.md](local-development.md) |
| Understand how the system is built (context, topology, flows, data model, security, CI) | [architecture.md](architecture.md) |
| Look up what a tool or concept is (Aspire, outbox, Testcontainers, commitlint, ...) | [glossary.md](glossary.md) |
| See the product name, brand direction, logo and design tokens | [branding/SUMMARY.md](branding/SUMMARY.md) |
| Set up or run the React front end | [frontend/README.md](../frontend/README.md) |
| Work on the code with Claude Code (commands, conventions, intentional deviations) | [../CLAUDE.md](../CLAUDE.md) |
| Open a pull request | [../.github/pull_request_template.md](../.github/pull_request_template.md) |

## Files in `docs/`

| File | Contents |
| --- | --- |
| [local-development.md](local-development.md) | Prerequisites, secrets and parameters, the local services, Keycloak users, the payment flow, Entra as an alternative, cleanup and troubleshooting. |
| [architecture.md](architecture.md) | System design with Mermaid diagrams. |
| [glossary.md](glossary.md) | Tools, services and concepts in plain words, with where the project uses them. |
| [branding/](branding/SUMMARY.md) | The Lootlark brand: [SUMMARY.md](branding/SUMMARY.md), [DIRECTIONS.md](branding/DIRECTIONS.md) and [NAMING.md](branding/NAMING.md). |

## Keeping the documentation current

- Change a flow, endpoint, the data model, authorization, the local topology or CI: update [architecture.md](architecture.md). Diagrams are Mermaid; make sure they still render. There are 13 today: 12 in `architecture.md` and 1 in the root `README.md`.
- Introduce a new tool, service or concept: add a row to [glossary.md](glossary.md) with what it is and where it is used.
- Change how to run or test the app: update the [README](../README.md) and [local-development.md](local-development.md).
- Add a document: list it in this index.
- Never commit real IDs, keys or secrets; use placeholders such as `<tenant-id>` or `[... HERE]`.

`docs/superpowers/` (specs and plans) is local-only and git-ignored.
