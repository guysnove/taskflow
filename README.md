# TaskFlow

TaskFlow is the task-management web application used in two video courses:

1. **Complete Web Application Development with C# and ASP.NET Core** (Course 1) — TaskFlow is built from scratch.
2. **Professional Software Engineering: From Writing Code to Engineering Production-Ready Systems** (Course 2) — TaskFlow v1 is progressively engineered into TaskFlow v2.

## Versions and milestones

| Tag | What it is |
|---|---|
| `v1.0` | **TaskFlow v1 — teaching baseline.** The application exactly as it stands at the end of Course 1 (Chapter 30): accounts, projects, tasks with checklists, EF Core + SQLite, a JSON API, security headers and rate limits, 41 automated tests, deployment files. |
| `ch01` … `ch30` | Course 2 milestones, one tag per chapter that changes the code (added as the course is produced). |

## Run it

Requirements: the .NET 10 SDK (LTS). Editor used in the course: Visual Studio Code.

```bash
dotnet restore
dotnet build
dotnet test                         # 41 tests at v1.0
cd TaskFlow && dotnet run           # then open the address it prints
```

The first run creates a SQLite database (`taskflow.db`) and applies the migrations. In Development, sample tasks are added for the first account you register.

## Project layout (v1.0)

```
TaskFlow/            the ASP.NET Core MVC application (controllers, views, API, EF Core data, services)
TaskFlow.Tests/      xUnit unit and integration tests
Deploy/              example Dockerfile, systemd service, nginx configuration, a small test proxy
.github/workflows/   continuous integration: restore, build, test, vulnerable-package check
```

All data in this repository is fictitious.
