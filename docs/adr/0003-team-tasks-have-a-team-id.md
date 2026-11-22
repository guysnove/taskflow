# ADR 0003 — Team tasks have a TeamId; visibility is decided in one query

- **Status:** accepted (for release 1.2; not built yet)
- **Date:** 2026-11-22 (Course 2, Chapter 23)
- **Proposal and review:** [proposals/0001-team-ownership.md](../proposals/0001-team-ownership.md)

## Context

Release 1.2 adds team tasks, visible to members only (FR-TEAM-2, NFR-SEC-TEAM-1). Today "who can see a task"
is written as `UserId == userId` in six places across three repositories. Adding a second rule in six places
is how a leak happens.

## Decision

- `TaskItem` gets a nullable `TeamId`; a `TeamMember(TeamId, UserId, IsLead)` table records membership.
- `EfTaskRepository.Visible()` is the **only** definition of "tasks this user may see":
  the user's own tasks, plus tasks of teams they belong to. Step and dependency repositories use it.
- An architecture test fails if task ownership is checked anywhere else.

## Consequences

- Personal tasks behave exactly as before.
- One query to review and test; every endpoint's security tests go through it.
- Sharing a single task with an individual is not possible without a new decision (a non-goal for 1.2).

## Alternatives considered

- A `TaskAccess` row per person and task: flexible, but many rows to keep in step on every join and leave.
- An EF Core global query filter: one place, but invisible in the repositories and easy to bypass.
