# TaskFlow

[![build-and-test](https://github.com/guysnove/taskflow/actions/workflows/ci.yml/badge.svg)](https://github.com/guysnove/taskflow/actions/workflows/ci.yml)

TaskFlow is a task-management web application: personal task lists with projects, checklists, due dates,
priorities and dependencies, in-app and email notifications, and a JSON API. ASP.NET Core 10, EF Core and SQLite.

It is also the teaching project of two video courses (see [About the courses](#about-the-courses)). All data in this repository is fictitious.

## Quick start

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and Git.

```bash
git clone https://github.com/guysnove/taskflow.git && cd taskflow
dotnet test --filter "Category!=Growth&Category!=E2E"   # about 240 tests, under a minute
dotnet run --project TaskFlow                           # then open the http://localhost:… address it prints
```

Register an account; in Development, a few sample tasks are added for the first account.
The full setup, including the end-to-end tests, coverage and migrations, is in [docs/getting-started.md](docs/getting-started.md).

## Documentation

| I want to… | Read |
|---|---|
| set up a development machine | [docs/getting-started.md](docs/getting-started.md) |
| understand how TaskFlow is built | [docs/architecture.md](docs/architecture.md) and the decisions in [docs/adr/](docs/adr/) |
| use the JSON API | [docs/api/README.md](docs/api/README.md) |
| configure or deploy it | [docs/configuration.md](docs/configuration.md), [Deploy/](Deploy/) |
| change the code | [CONTRIBUTING.md](CONTRIBUTING.md), [docs/testing.md](docs/testing.md) |
| see what's planned | [docs/planning/](docs/planning/README.md) |
| report a vulnerability | [SECURITY.md](SECURITY.md) |

Everything else: [docs/README.md](docs/README.md), the map of all documentation.

## Project layout

```
TaskFlow/                  the web app: MVC pages, the JSON API, Program.cs (the composition root)
TaskFlow.Application/      use cases, rules, notifications, and the interfaces they need
TaskFlow.Domain/           entities and pure domain logic; no packages, no references
TaskFlow.Infrastructure/   EF Core + SQLite, migrations, SMTP, the background email worker
TaskFlow.Tests/            unit, integration, architecture, security and end-to-end (Playwright) tests
tools/                     coverage gate, traceability report, commit-message hook, module and metrics tools
Deploy/                    example Dockerfile, systemd service, nginx configuration
docs/                      documentation (start at docs/README.md)
exercises/                 small programs used in Course 2's chapters (exercises/README.md)
```

## About the courses

TaskFlow is used in two video courses:

1. **Complete Web Application Development with C# and ASP.NET Core** (Course 1): TaskFlow is built from scratch.
2. **Professional Software Engineering: From Writing Code to Engineering Production-Ready Systems** (Course 2): TaskFlow v1 is progressively engineered into TaskFlow v2.

| Branch | What it is |
|---|---|
| `milestone/v1.0` | TaskFlow v1, the teaching baseline: the application at the end of Course 1. |
| `milestone/ch01` … `milestone/ch30` | Course 2 milestones, one per chapter that changes the repository. `main` always holds the newest version. |

Small programs from Course 2's chapters are in [exercises/](exercises/README.md).
