# TaskFlow

TaskFlow is the task-management web application used in two video courses:

1. **Complete Web Application Development with C# and ASP.NET Core** (Course 1) — TaskFlow is built from scratch.
2. **Professional Software Engineering: From Writing Code to Engineering Production-Ready Systems** (Course 2) — TaskFlow v1 is progressively engineered into TaskFlow v2.

## Versions and milestones

| Branch | What it is |
|---|---|
| `milestone/v1.0` | **TaskFlow v1 — teaching baseline.** The application exactly as it stands at the end of Course 1 (Chapter 30): accounts, projects, tasks with checklists, EF Core + SQLite, a JSON API, security headers and rate limits, 41 automated tests, deployment files. |
| `milestone/ch01` … `milestone/ch30` | Course 2 milestones, one branch per chapter that changes the code (added as the course is produced). `main` always holds the newest version. |

## Code style and analysis (Course 2, Chapter 7)

`.editorconfig` holds the style rules and the analyser decisions; `Directory.Build.props` turns on the recommended .NET analysers. CI runs `dotnet format --verify-no-changes` and builds with `-warnaserror`. Fix formatting with `dotnet format`.

## Security (Course 2, Chapter 20)

`docs/security/threat-model.md`: assets, trust boundaries, STRIDE, findings and accepted risks. `SECURITY.md`: how to report a vulnerability. `dotnet test --filter Category=Security` runs the security tests. Secrets (SMTP password…) never go into `appsettings*.json`: use `dotnet user-secrets` in development and environment variables in production.

## Requirements (Course 2, Chapter 6)

`docs/testing.md` (Course 2, Chapter 17) explains how TaskFlow is tested: unit vs integration, conventions, test doubles, coverage, running tests in VS Code.

`docs/requirements/` holds TaskFlow's requirements, with IDs. Tests carry `[Trait("Req", "…")]`. `dotnet run --project tools/Traceability` reports which requirements have tests; `dotnet test --filter "Req=FR-DUP-1"` runs one requirement's tests.

## Growth timings (Course 2, Chapter 5)

`dotnet test -c Release --filter Category=Growth --logger "console;verbosity=detailed"` prints how pages behave as one user's task list grows. Normal runs and CI skip it.

## Chapter exercises (Course 2)

Small programs used in Course 2 chapters live in `exercises/chNN/`, separate from TaskFlow itself.

| Folder | Chapter | What it is |
|---|---|---|
| `exercises/ch01/BrightCart.Orders` | 1 | A deliberately flawed legacy console app (fictitious shop) to review. Run with `dotnet run` in its folder. |
| `exercises/ch01/MoneyDemo` | 1 | `money.cs`: double vs decimal for money. Run with `dotnet run money.cs` (.NET 10 file-based app). |
| `exercises/ch02` | 2 | `Ch02.slnx`: duplicate finding (pairs vs groups), discount rules, scheduling strategies, with 23 tests. `Ch02.Demo`: `dotnet run -c Release -- duplicates` / `dotnet run -- schedule`. `ZuneBug/zune.cs`: the case study's loop. |
| `exercises/ch03` | 3 | `Ch03.slnx`: array/list, linked list, stack, ring queue and hash map built from scratch, plus a task search (List vs Dictionary), with 29 tests. `Ch03.Demo`: `capacity`, `locality`, `search`, `hash`, `hashcode`, `modify`. |
| `exercises/ch04` | 4 | `Ch04.slnx`: recursion, a binary search tree, a min-heap, a graph (BFS, cycle detection, topological sort), searching and sorting, fewest coins, with 29 tests. `Ch04.Demo`: `overflow N`, `bst`, `sort`, `search`, `plan`, `queue`, `fib`, `stable`, `tree PATH`, `range`. |
| `exercises/ch05` | 5 | `Ch05.slnx`: step counters, de-duplication, six searches, snippets to analyse, space and cases, with 32 tests. `Ch05.Demo`: `dedupe`, `steps`, `doubling`, `cases`, `concat`, `space`, `jit`, `setup`. `Ch05.Benchmarks` (BenchmarkDotNet): `dotnet run -c Release -- --filter "*"`. |
| `exercises/ch06` | 6 | `Ch06.slnx`: US-2's acceptance criteria as tests (`ReminderPolicy`) and a tiny requirement linter, with 11 tests. `Ch06.Demo`: `interviews`, `ours`. |
| `exercises/ch07` | 7 | `Ch07.slnx`: a messy `DigestService` and its refactored `DailyDigest`, with characterisation tests (6); `CsvImport` to refactor (Independent B). `Ch07.Demo`: `before`, `after`, `clockbug`. |
| `exercises/ch17` | 17 | `Ch17.slnx`: a small shop's `PriceCalculator` with 17 unit tests (AAA, theories, the five test doubles, isolation); `Ch17.Mistakes.Tests` fails **on purpose** (2 of 4); `Ch17.Legacy/LateFees.cs` to make testable (Guided 2). `.vscode/tasks.json`: run tests as VS Code tasks. |
| `exercises/ch18` | 18 | `Ch18.slnx`: a tiny notes app (web page + JSON API + SQLite) with integration tests (`WebApplicationFactory`, a test sign-in scheme, real cookie sign-in), end-to-end tests with Playwright (`Category=E2E`, needs Chromium) and a deliberately flaky test (`Category=Flaky`). |
| `exercises/ch19` | 19 | `Ch19.slnx`: a shipping-cost kata built test-first (14 recorded steps), with 13 tests. Tasks: `test`, `test: watch` (`dotnet watch test`), `test: mutation (Stryker)` (`dotnet tool restore` first). |
| `exercises/ch20` | 20 | `Ch20.slnx`: a notes service with four classic vulnerabilities (`/bad/...`) next to their fixes (`/good/...`), 12 attack tests, `demo.sh` (curl attacks on localhost), and `Ch20.Audit` (a deliberately vulnerable package, to see NuGet audit; **not** part of the build gate). Never deploy the `/bad` endpoints. |

## Run it

Requirements: the .NET 10 SDK (LTS). Editor used in the course: Visual Studio Code.

```bash
dotnet restore
dotnet build
dotnet test                         # 41 tests at milestone/v1.0
./tools/coverage.sh                 # Course 2, Chapters 17 and 19: unit-test coverage of the business logic, and its quality gate
cd TaskFlow && dotnet run           # then open the address it prints
# migrations now live in TaskFlow.Infrastructure:
# dotnet ef migrations add <Name> --project TaskFlow.Infrastructure --startup-project TaskFlow
```

The first run creates a SQLite database (`taskflow.db`) and applies the migrations. In Development, sample tasks are added for the first account you register.

## Project layout (Course 2, Chapter 12: four layers)

```
TaskFlow/                  Presentation: the ASP.NET Core MVC app (controllers, views, API) and Program.cs, the composition root
TaskFlow.Application/      Application: use cases, rules, notifications, and the ports (interfaces) they need
TaskFlow.Domain/           Domain: entities and pure domain logic; no packages, no references
TaskFlow.Infrastructure/   Infrastructure: EF Core + SQLite, migrations, SMTP, the background job (adapters for the ports)
TaskFlow.Tests/            xUnit unit, integration, architecture and end-to-end (Playwright) tests
Deploy/              example Dockerfile, systemd service, nginx configuration, a small test proxy
.github/workflows/   continuous integration: restore, build, test, vulnerable-package check
```

All data in this repository is fictitious.
