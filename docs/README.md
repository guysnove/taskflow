# TaskFlow documentation

Course 2, Chapter 23. The map of everything written down about TaskFlow, arranged by what you're trying to do
(the four kinds of documentation from the Diátaxis framework). If you add a document, add it here;
`tools/DocsCheck` (run in CI) fails the build if a link here, or anywhere in the docs, points at nothing.

## Learning: start here

- [Getting started](getting-started.md): from a fresh machine to a running TaskFlow and a green test run.

## How-to guides: doing a specific job

| Task | Where |
|---|---|
| Run one kind of test (unit, integration, security, E2E, growth) | [testing.md §4](testing.md) and [getting-started.md](getting-started.md#5-run-the-tests) |
| Add a database migration | [getting-started.md](getting-started.md#7-database-migrations) |
| Check unit-test coverage, the quality gate | [testing.md](testing.md), `tools/coverage.sh` |
| Trace a requirement to its tests | `dotnet run --project tools/Traceability` ([requirements/README.md](requirements/README.md)) |
| Deploy behind nginx with systemd | [configuration.md](configuration.md), [../Deploy/](../Deploy/) |
| Deploy a release, and roll back | [ci-cd.md](ci-cd.md#deployment-and-rollback) |
| Fix a red build | [ci-cd.md](ci-cd.md#when-the-build-is-red) |
| Respond to an incident | [incidents/README.md](incidents/README.md) |
| Contribute a change: branch, commit, pull request, review | [../CONTRIBUTING.md](../CONTRIBUTING.md) |

## Reference: looking something up

| What | Where |
|---|---|
| The JSON API: endpoints, authentication, errors, examples | [api/README.md](api/README.md); the rules behind it: [api/guidelines.md](api/guidelines.md) |
| Settings (appsettings, environment variables, secrets) | [configuration.md](configuration.md) |
| Requirements, with ids | [requirements/](requirements/README.md) |
| Definitions of Ready and Done | [planning/definition-of-done.md](planning/definition-of-done.md) |

## Explanation: understanding why

| Topic | Where |
|---|---|
| Architecture: layers, modules, the main flows (diagrams) | [architecture.md](architecture.md), [design/modules.md](design/modules.md) |
| Decisions and their reasons | [adr/](adr/) (architecture decision records), [proposals/](proposals/) |
| How we test, and why | [testing.md](testing.md) |
| Security: threats, findings, accepted risks | [security/threat-model.md](security/threat-model.md), [../SECURITY.md](../SECURITY.md) |
| Technical debt we know about | [tech-debt.md](tech-debt.md) |
| How we plan | [planning/README.md](planning/README.md) |
| How code reaches production: the pipeline | [ci-cd.md](ci-cd.md) |

## Course material

Each document says which chapter of Course 2 wrote it. Engineering updates written during the course: [updates/](updates/).
