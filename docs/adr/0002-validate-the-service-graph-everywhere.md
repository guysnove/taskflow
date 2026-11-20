# ADR 0002 — Validate the service graph in every environment

- **Status:** accepted
- **Date:** 2026-11-19 (Course 2, Chapter 13)

## Context

ASP.NET Core validates the dependency-injection container (`ValidateScopes`, `ValidateOnBuild`) only in the Development environment. A lifetime mistake — for example a singleton `Notifier` that captures scoped repositories — is reported in Development, but in Production the app starts normally and the mistake shows up later as wrong or leaked data. We saw both behaviours for real in Chapter 13.

## Decision

`Program.cs` calls `builder.Host.UseDefaultServiceProvider(o => { o.ValidateScopes = true; o.ValidateOnBuild = true; })` for every environment. `ServiceGraphTests` builds TaskFlow's real registrations with validation on every build.

## Consequences

- A lifetime mistake stops the app at startup, everywhere, with a message naming both services.
- Startup does a little more work (every service descriptor is checked once). Not measurable at TaskFlow's size; revisit if startup time matters (e.g. scale-to-zero hosting).
- Scope validation also refuses resolving scoped services from the root provider, so background work must create a scope (as `DueReminderService` already does).

## Alternatives considered

- Development-only validation (the default): cheaper startup, but production can run with a broken graph.
- Only the test: catches mistakes before merge, but not configuration-dependent registrations made at runtime.
