# Proposal 0001 — How a task belongs to a team

- **Author:** the developer on T-1 (issue #4) · **Status:** accepted → [ADR 0003](../adr/0003-team-tasks-have-a-team-id.md)
- **Written:** 2026-11-22 (Course 2, Chapter 23) · **Review:** the design review below
- **Time box:** 2 days (a spike, release 1.2 "Teams")

## Summary (read this if nothing else)

Give a task an optional **team**. A task is visible to its owner, and, if it has a team, to the team's members.
We put that rule in **one** query that every repository uses, and test it at every endpoint before Teams is switched on.
Cost: about 3 points of work inside T-2/T-3; one migration; no change to how personal tasks behave.

## Problem

Today a task has exactly one owner (`TaskItem.UserId`), and "can this user see this task?" is answered by
`… .UserId == userId` in **six places** in three repositories (tasks, steps, dependencies), counted with
`grep -rn "UserId ==" TaskFlow.Infrastructure`. Release 1.2 adds team tasks (FR-TEAM-2): visible to every
member, and to no one else. If we add the team rule six times, one copy will be wrong, and a wrong copy leaks a
client's work (the threat model's most serious risk).

## Goals and non-goals

- **Goals:** team tasks visible and editable by members only (FR-TEAM-2); one place that decides visibility;
  personal tasks unchanged; 404 for others (NFR-SEC-TEAM-1).
- **Non-goals:** sharing one task with an individual; roles beyond "lead" and "member"; tasks in several teams.

## Options

| | A. `TeamId` on the task | B. An access table | C. An EF Core global query filter |
|---|---|---|---|
| Model | `TaskItem.TeamId` (nullable) + `TeamMember(TeamId, UserId)` | `TaskAccess(TaskId, UserId)` rows per person | either A or B, filtered automatically on every query |
| Visibility rule | `t.UserId == me \|\| myTeamIds.Contains(t.TeamId)` | `access.Any(a => a.TaskId == t.Id && a.UserId == me)` | written once in `OnModelCreating` |
| Joining / leaving a team | add or remove one `TeamMember` row | add or remove one row **per task** | as A or B |
| Future: share with one person | not supported (non-goal) | supported | as A or B |
| Risk of a leak | low, **if** every repository uses the one query | low, but many rows to keep right | hidden: easy to bypass with `IgnoreQueryFilters()`, invisible when reading a repository |
| Effort | small: 1 migration, 1 query | medium: 1 migration, backfill, sync on join/leave | small, but hard to test where it's not applied |

## Recommendation

**Option A**, with the rule in one method, `EfTaskRepository.Visible()`, which the step and dependency
repositories reuse instead of their own `UserId ==` checks. B solves a problem we don't have (sharing with
individuals: a non-goal) at the cost of keeping many rows in step. C hides the security rule where reviewers
won't see it.

## Risks and how we'll know

- A repository bypasses `Visible()` → an architecture test: no `UserId ==` on tasks outside `Visible()`.
- A wrong rule → T-3's acceptance criteria as integration tests for **every** task, step and dependency endpoint, red first.
- Performance: `myTeamIds` is a small list per user; the existing `(UserId, Due)` index still serves personal lists. Re-check the query plan test (Chapter 14).

## Rollout

Behind the `Teams` feature flag. T-2 creates teams (flag off in production); T-3 switches visibility to `Visible()`
with its tests; the flag turns on only after T-3 is done (release plan).

## Design review (2026-11-22)

Written for the course: one person played both author and reviewer. The questions are the ones a reviewer should ask.

| Question | Answer | Outcome |
|---|---|---|
| Why not C? One place sounds like what we want. | One place, yes, but invisible: a reviewer reading `EfStepRepository` can't see the rule, and `IgnoreQueryFilters()` silently removes it. A is one *visible* place. | keep A |
| What happens to a member's tasks when they leave? | Personal tasks stay theirs; team tasks stay with the team, unassigned (FR-TEAM-5). | added to T-6's criteria |
| Can a task move from personal to a team? | Yes, by setting `TeamId`; only its owner can. Moving it out of a team: only the lead. | added to T-2 as an open question for the product owner |
| How do we stop the old `UserId ==` checks coming back? | An architecture test (see Risks). | new task in T-3 |

**Decision:** accept option A. Recorded as [ADR 0003](../adr/0003-team-tasks-have-a-team-id.md).
