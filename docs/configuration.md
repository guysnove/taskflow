# Configuration

Course 2, Chapter 23. Every setting TaskFlow reads, where it comes from, and its default.

ASP.NET Core reads settings, later sources winning: `appsettings.json` → `appsettings.{Environment}.json` →
user secrets (Development only) → environment variables. In an environment variable, `__` stands for `:`
(`Email__Password` sets `Email:Password`). Invalid settings stop TaskFlow at startup, with a message (`ValidateOnStart`).

## Settings

| Key | Default | Meaning |
|---|---|---|
| `ConnectionStrings:TaskFlow` | `Data Source=taskflow.db` | the SQLite database file |
| `TaskFlow:SiteName` | `TaskFlow` | shown in page titles; 1–40 characters |
| `TaskFlow:DueSoonDays` | `2` | how many days ahead counts as "due soon" (0–30) |
| `Email:From` | `taskflow@example.com` | sender address (required) |
| `Email:SiteUrl` | `https://localhost:5001` | the public address, for links in emails (required) |
| `Email:Host`, `Email:Port` | none, `587` | the SMTP server; required unless `Email:PickupDirectory` is set |
| `Email:Username`, `Email:Password` | none | SMTP sign-in. **Secrets**: never in a settings file (see below) |
| `Email:PickupDirectory` | none (`mail-outbox` in Development) | write emails as `.eml` files here instead of sending |
| `Email:Attempts`, `Email:RetryDelay`, `Email:SendTimeout` | `3`, `00:00:02`, `00:00:30` | retrying a failed send (1–10 attempts) |
| `Caching:DueCountsSeconds` | `30` | how long the header's due counts are cached per user; `0` = no cache (Course 2, Chapter 27) |
| `ASPNETCORE_ENVIRONMENT` | `Production` (`Development` with `dotnet run`) | turns on sample data, the mail outbox, OpenAPI and `/debug/routes` in Development |

## Secrets

- **Development:** `dotnet user-secrets set "Email:Password" "…" --project TaskFlow` (stored in your user profile, outside the repository).
- **Server:** `/etc/taskflow/secrets.env`, readable only by root, loaded by `Deploy/taskflow.service` (`EnvironmentFile=`), with lines like `Email__Password=…`.
- `NoSecretsInRepoTests` fails the build if a settings file in the repository contains a secret-looking key with a value.

## Deployment files

| File | What it is |
|---|---|
| `Deploy/taskflow.service` | a systemd unit: runs TaskFlow as its own user, restarts it, loads the secrets file |
| `Deploy/nginx-taskflow.conf` | nginx in front: HTTPS, forwarded headers |
| `Deploy/Dockerfile` | the container image ([containers.md](containers.md)) |
| `compose.yaml`, `compose.dev.yaml` | run the image with its data volume; development settings on top |
