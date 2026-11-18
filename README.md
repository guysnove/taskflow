# TaskFlow

TaskFlow is the task-management web application used in two video courses:

1. **Complete Web Application Development with C# and ASP.NET Core** (Course 1) — TaskFlow is built from scratch.
2. **Professional Software Engineering: From Writing Code to Engineering Production-Ready Systems** (Course 2) — TaskFlow v1 is progressively engineered into TaskFlow v2.

## Versions and milestones

| Branch | What it is |
|---|---|
| `milestone/v1.0` | **TaskFlow v1 — teaching baseline.** The application exactly as it stands at the end of Course 1 (Chapter 30): accounts, projects, tasks with checklists, EF Core + SQLite, a JSON API, security headers and rate limits, 41 automated tests, deployment files. |
| `milestone/ch01` … `milestone/ch30` | Course 2 milestones, one branch per chapter that changes the code (added as the course is produced). `main` always holds the newest version. |

## Chapter exercises (Course 2)

Small programs used in Course 2 chapters live in `exercises/chNN/`, separate from TaskFlow itself.

| Folder | Chapter | What it is |
|---|---|---|
| `exercises/ch01/BrightCart.Orders` | 1 | A deliberately flawed legacy console app (fictitious shop) to review. Run with `dotnet run` in its folder. |
| `exercises/ch01/MoneyDemo` | 1 | `money.cs`: double vs decimal for money. Run with `dotnet run money.cs` (.NET 10 file-based app). |
| `exercises/ch02` | 2 | `Ch02.slnx`: duplicate finding (pairs vs groups), discount rules, scheduling strategies, with 23 tests. `Ch02.Demo`: `dotnet run -c Release -- duplicates` / `dotnet run -- schedule`. `ZuneBug/zune.cs`: the case study's loop. |

## Run it

Requirements: the .NET 10 SDK (LTS). Editor used in the course: Visual Studio Code.

```bash
dotnet restore
dotnet build
dotnet test                         # 41 tests at milestone/v1.0
cd TaskFlow && dotnet run           # then open the address it prints
```

The first run creates a SQLite database (`taskflow.db`) and applies the migrations. In Development, sample tasks are added for the first account you register.

## Project layout (TaskFlow v1)

```
TaskFlow/            the ASP.NET Core MVC application (controllers, views, API, EF Core data, services)
TaskFlow.Tests/      xUnit unit and integration tests
Deploy/              example Dockerfile, systemd service, nginx configuration, a small test proxy
.github/workflows/   continuous integration: restore, build, test, vulnerable-package check
```

All data in this repository is fictitious.
