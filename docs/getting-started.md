# Getting started

Course 2, Chapter 23. From a fresh machine to a running TaskFlow and a green test run, in about 15 minutes.
Every command below was run, in this order, in a fresh clone, when this page was written.
If a step doesn't work for you, that's a bug in this page: please open an issue.

## 1. Install

| Tool | Version | Check |
|---|---|---|
| .NET SDK | 10.0 (LTS) | `dotnet --version` prints `10.0.…` |
| Git | any recent | `git --version` |
| VS Code (recommended) | any recent, with the C# Dev Kit extension | |
| PowerShell (`pwsh`) | 7 or later | only for the end-to-end tests' browser install (step 6) |

## 2. Get the code

```bash
git clone https://github.com/guysnove/taskflow.git
cd taskflow
git config core.hooksPath tools/hooks      # the commit-message check (CONTRIBUTING.md §2)
```

## 3. Build

```bash
dotnet build -warnaserror
```

Expected: `Build succeeded.` with `0 Warning(s)` and `0 Error(s)`. Warnings fail the build here and in CI.

## 4. Run it

```bash
dotnet run --project TaskFlow
```

It prints `Now listening on: http://localhost:5xxx`. Open that address, register an account, and sign in.

- The first run creates `TaskFlow/taskflow.db` (SQLite) and applies the migrations.
- In **Development** (the default for `dotnet run`), the first account gets a few sample tasks, and emails are
  written as `.eml` files to `TaskFlow/mail-outbox/` instead of being sent.
- `GET /health` answers `Healthy` without signing in. `GET /openapi/v1.json` (Development only) is the API description.

Stop it with Ctrl+C.

## 5. Run the tests

```bash
dotnet test --filter "Category!=Growth&Category!=E2E"
```

Expected: `Passed!`, about 240 tests, in under a minute. This is what CI runs first.

| Run only… | Command |
|---|---|
| unit tests | `dotnet test --filter "FullyQualifiedName~TaskFlow.Tests.Unit"` |
| security tests | `dotnet test --filter Category=Security` |
| one requirement's tests | `dotnet test --filter "Req=FR-LIST-2"` |
| growth timings (slow, on request) | `dotnet test -c Release --filter Category=Growth --logger "console;verbosity=detailed"` |
| coverage, with the quality gate | `./tools/coverage.sh` |

## 6. The end-to-end test (a real browser)

The E2E test drives Chromium through Playwright. Install the browser once, after a build:

```bash
pwsh TaskFlow.Tests/bin/Debug/net10.0/playwright.ps1 install chromium
dotnet test --filter Category=E2E
```

On Linux, add `--with-deps` to the install to get the system libraries (needs `sudo`).

## 7. Database migrations

Migrations live in `TaskFlow.Infrastructure`. The EF Core command-line tool is not part of the SDK; install it once:

```bash
dotnet tool install --global dotnet-ef --version "10.0.*"
dotnet ef migrations add <Name> --project TaskFlow.Infrastructure --startup-project TaskFlow
```

If the shell says `dotnet-ef` or `dotnet ef` isn't found, global tools aren't on your PATH yet: add
`~/.dotnet/tools` (macOS/Linux) or `%USERPROFILE%\.dotnet\tools` (Windows), or open a new terminal.
`dotnet ef migrations list --project TaskFlow.Infrastructure --startup-project TaskFlow` lists them all.

TaskFlow applies pending migrations itself when it starts.

## 8. Next

- How the code is organised: [architecture.md](architecture.md).
- How to make a change: [../CONTRIBUTING.md](../CONTRIBUTING.md).
- Settings: [configuration.md](configuration.md).
