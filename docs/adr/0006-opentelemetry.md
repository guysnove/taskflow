# ADR 0006 — Metrics and traces with OpenTelemetry; health split into /health and /health/details

- **Status:** accepted
- **Date:** 2026-11-23 (Course 2, Chapter 29)

## Context

TaskFlow had logs (JSON in production) and `/health`. Questions we couldn't answer without reading logs line by line:
how slow is the task list right now, for everyone? How many emails failed today? Where did the time go in one slow request?
And a dead email (Chapter 28) was only a log line.

## Decision

- **Metrics:** a `TaskFlow` meter (`System.Diagnostics.Metrics`, part of .NET) with a few instruments; ASP.NET Core's built-in
  request metrics. **Traces:** an `ActivitySource` with spans for `CompleteTask` and `SendEmail`, inside ASP.NET Core's request spans.
- **Export with OpenTelemetry** (`OpenTelemetry.Extensions.Hosting` 1.21, ASP.NET Core instrumentation, OTLP and console exporters):
  the vendor-neutral standard. Where data goes is configuration (`OTEL_EXPORTER_OTLP_ENDPOINT`); no vendor in the code.
- **Logs carry the trace id** (`IncludeScopes` in production), so a trace leads to its log lines.
- **Health:** `/health` stays "alive and the database works" — deploys (Chapter 24) and containers (Chapter 25) act on it.
  `/health/details` adds checks a person should look at (the email outbox), as JSON, **Degraded** rather than Unhealthy:
  restarting TaskFlow doesn't fix a mail server.

## Consequences

- Four new packages to keep updated (Dependabot); a little CPU per request for spans. No measurable change at our load (Chapter 29 run).
- Instruments are a public contract for dashboards and alerts: renaming one breaks them. Names follow OpenTelemetry's conventions.
- `/health/details` reveals operational numbers (not secrets). In production, only the monitoring system should reach it.
- Not yet: a collector and a dashboard service run by us; until then, `dotnet-counters` and logs.

## Alternatives considered

- **Logs only**, counting with log queries: possible, but slow and expensive at volume, and no request timelines.
- **A vendor SDK** (Application Insights, Datadog): good tools, but the code would be tied to one vendor.
- **Prometheus exporter endpoint (`/metrics`)**: the ASP.NET Core exporter package is still prerelease; OTLP covers Prometheus through a collector.
