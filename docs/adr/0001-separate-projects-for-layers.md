# ADR 0001 — Separate projects for TaskFlow's layers

- **Status:** accepted
- **Date:** 2026-11-19 (Course 2, Chapter 12)

## Context

Since Chapter 11, TaskFlow's modules are folders in one project, and architecture tests check which module may use which. The tests found real problems (a cycle, controllers using the database). The module boundaries have been stable since then. The Application code still referenced EF Core directly in four places (`Notifier`, `UnblockedTasksObserver`, `DueReminders`, `AccountService`).

## Decision

Split TaskFlow into four projects — `TaskFlow.Domain`, `TaskFlow.Application`, `TaskFlow.Infrastructure`, and the web project `TaskFlow` — with references pointing inwards only. Where Application needed the database, add a port (`INotificationQueries`, `IAccountStore`) and implement it in Infrastructure. Keep the existing namespaces.

## Consequences

- The compiler refuses Application → Infrastructure references; `LayerRulesTests` refuses outward project references and database packages in Application.
- EF Core migrations live in `TaskFlow.Infrastructure`: `dotnet ef … --project TaskFlow.Infrastructure --startup-project TaskFlow`.
- The `.editorconfig` rule for generated migrations and the Dockerfile had to change with the move.
- Four project files to maintain; renaming or merging layers is slower than moving a folder.
- The web project still references Infrastructure (it is the composition root), so the compiler can't stop a controller from using `TaskFlowDbContext`: `ModuleRulesTests` still does.

## Alternatives considered

- **Stay with one project + tests** (Chapter 11): cheaper, but every rule depends on our home-made test.
- **Clean Architecture with a project per feature, MediatR, and an interface per use case:** more ceremony than a one-team app with ~3,000 lines needs (see "Recognising architecture overengineering" in Chapter 12).
