# Running TaskFlow in production: monitoring and maintenance

Course 2, Chapter 29. What TaskFlow tells us while it runs, what we watch, when a person is woken up, and what they do.

## 1. The three signals

| Signal | What it answers | In TaskFlow | Where it goes |
|---|---|---|---|
| **Logs** | "what happened, to this request?" | JSON lines in production (Chapter 18), with `TraceId`/`SpanId` in each line's scopes | the console → the host's log collector |
| **Metrics** | "how much, how fast, how often — over time?" | the `TaskFlow` meter (below) + ASP.NET Core's `Microsoft.AspNetCore.Hosting` meter | `dotnet-counters`; OpenTelemetry (OTLP) when `OTEL_EXPORTER_OTLP_ENDPOINT` is set |
| **Traces** | "where did the time go in this request, across components?" | a span per request (ASP.NET Core) + `CompleteTask` and `SendEmail` spans | OTLP; or the console with `Telemetry__ConsoleTraces=true` |

A log line and a trace share the **trace id**: find a slow or failed span, then `grep` its trace id in the logs to read what happened.

## 2. TaskFlow's metrics

| Instrument | Type | Tags | Normal | Worry when |
|---|---|---|---|---|
| `http.server.request.duration` (ASP.NET Core) | histogram, s | route, method, status | p99 under 0.5 s | p99 > 1 s for 10 min |
| `http.server.active_requests` (ASP.NET Core) | up-down counter | method | a few | keeps growing |
| `taskflow.tasks.created` | counter | `taskflow.via` = web, api | follows the day | 0 for hours on a weekday |
| `taskflow.tasks.completed` | counter | | follows the day | 0 for hours on a weekday |
| `taskflow.emails` | counter | `taskflow.outcome` = sent, failed, dead | mostly `sent` | any `dead`; `failed` > `sent` |
| `taskflow.email.send.duration` | histogram, s | `taskflow.outcome` | under 2 s | near `Email:SendTimeout` |
| `taskflow.email.outbox.waiting` | gauge | | 0–5 | > 50, or growing for 15 min |

Watch them live on a running TaskFlow:

```bash
dotnet tool install --global dotnet-counters
dotnet-counters monitor -n TaskFlow --counters TaskFlow,Microsoft.AspNetCore.Hosting
```

## 3. The dashboard

One screen, top to bottom, in the order someone on call reads it:

1. **Is it up?** `/health` from outside (uptime check), and `/health/details` status.
2. **Are users OK?** requests per second; error rate (5xx ÷ all); p50 and p99 duration — by route.
3. **Is the work getting done?** emails by outcome; outbox waiting; tasks created and completed.
4. **Is the machine OK?** CPU, memory, disk free (the database and the data protection keys live on disk).

## 4. Service level objectives (SLOs)

| SLO | Target, over 28 days | Measured by |
|---|---|---|
| Availability | 99.5% of `/health` checks answer `Healthy` (about 3.5 hours of downtime allowed) | the uptime check |
| Latency | 99% of page requests finish in under 1 s | `http.server.request.duration` |
| Notifications | 99% of emails sent within 10 minutes; none dead without a person knowing | `taskflow.emails`, `/health/details` |

The allowed failure (0.5%, 1%) is the **error budget**. While budget is left, we ship features; when it's spent, reliability work comes first.

## 5. Alerts

An alert must be **actionable**: someone can do something about it now. Everything else is a dashboard, not a page.

| Alert | Condition | Severity | Runbook |
|---|---|---|---|
| TaskFlow down | `/health` not `Healthy` for 1 minute (from the incident action in [the 2026-11-22 drill](incidents/2026-11-22-drill-database-path.md)) | page (SEV-1) | §6.1 |
| Errors | 5xx more than 2% of requests for 5 minutes | page (SEV-2) | §6.2 |
| Slow | p99 over 2 s for 10 minutes | ticket | §6.2 |
| Email given up | `/health/details` → `email-outbox` Degraded (any dead email in 24 h, or one overdue 10 min) | ticket; page if > 10 | §6.3 |
| Disk | free disk under 15% | ticket | grow the disk; check the database size |

## 6. Runbooks

### 6.1 TaskFlow is down
1. `curl -s https://<host>/health` and the host's status. Was there a deploy in the last hour? **Roll it back first** ([ci-cd.md](ci-cd.md#deployment-and-rollback)).
2. Read the last 50 log lines for `crit`/`fail`/`Unhandled exception`. Startup failures name the setting.
3. Follow [incidents/README.md](incidents/README.md): say it out loud, keep a timeline.

### 6.2 Errors or slowness
1. Which routes? (`http.route` tag). One route → a recent change there; all routes → the database or the machine.
2. Pick a failing or slow request's trace id, `grep` it in the logs.
3. Database: Chapter 26's tools (`SQLLOG=Information`, query plans).

### 6.3 Emails given up (dead)
1. `curl -s https://<host>/health/details` shows how many. The log has one `Error` per dead email: "failed N times; given up (dead): <error>".
2. Fix the cause (mail server down? a wrong `Email:Host`/`Email:Port`? an expired password?).
3. Send them again:
   ```sql
   UPDATE OutboxEmails SET DeadAt = NULL, Attempts = 0, NextAttemptAt = CreatedAt WHERE DeadAt IS NOT NULL;
   ```
   The worker sends them within seconds. Check `/health/details` is `Healthy` again.
4. Rehearsed: [incidents/2026-11-23-drill-mail-port.md](incidents/2026-11-23-drill-mail-port.md).

## 7. Maintenance, on a schedule

| Every | What |
|---|---|
| week | merge Dependabot's grouped updates (CI decides, Chapter 24); read the error log for new kinds of errors |
| month | restore the latest backup on a spare machine and start TaskFlow on it (a backup never restored is a hope); review alerts: which fired, which were noise |
| quarter | update the .NET runtime (patch releases monthly; a new major version yearly, LTS every two years); review the [technical debt register](tech-debt.md) |
| incident | a blameless report within days; its actions go into the backlog ([incidents/](incidents/README.md)) |
