# Release 1.2 — "Teams": release plan

Course 2, Chapter 22. Status: **planned, not started**. Source: `docs/requirements/notifications-and-teams.md` (FR-TEAM-*).

## Release goal

> A small studio can share its client work in TaskFlow: a team lead sees every open team task and who owns it,
> and no one outside the team can see it.

Measure (from the requirements): no missed client deadline caused by "I didn't know" in the three months after release.

## The product backlog, in order

The order is the product owner's: **dependencies and risk first**, then value. (Sorting by value per point alone
put "assign a task" before "teams exist": see the chapter.) Data: [backlog.csv](backlog.csv).

| # | Story | Pts | MoSCoW | Requirement |
|---|---|---|---|---|
| T-1 | **Spike**: how a task belongs to a team — write ADR 0003 (time-boxed: 2 days) | 2 | Must | FR-TEAM-2 |
| T-2 | Create a team and invite members | 8 | Must | FR-TEAM-1 |
| T-3 | Team tasks are visible only to members | 8 | Must | FR-TEAM-2, NFR-SEC-TEAM-1 |
| T-4 | Assign a team task to a member (US-1) | 5 | Must | FR-TEAM-3, FR-NOTIF-1 |
| T-5 | Team lead sees open tasks by assignee | 5 | Should | FR-TEAM-4 |
| T-6 | Leave a team (assigned tasks become unassigned) | 3 | Should | FR-TEAM-5 |
| T-7 | Filter the task list to my team's tasks | 2 | Could | — |
| T-8 | Decline an invitation | 2 | Could | FR-TEAM-1 |
| T-9 | Comments on tasks | 8 | **Won't** (this release) | FR-COMM-1 |

Total for the release: **35 points** (Must 23, Should 8, Could 4).

### Example: T-3 with its acceptance criteria

**As a** studio owner, **I want** team tasks to be visible only to the team's members, **so that** no client's work leaks to people who aren't on it.

- **Given** Maya and Sam are in team "Brightside" and Joe is not, **when** Joe asks for a Brightside task by its id (page or API), **then** he gets 404 Not Found, exactly as for a task that doesn't exist.
- **Given** the same, **when** Joe searches for the task's title, **then** it isn't in his results.
- **Given** Sam leaves "Brightside", **when** he opens a Brightside task he used to see, **then** he gets 404.
- **Given** Maya, **when** she opens her task list, **then** she sees her own tasks **and** Brightside's.

(Every story's criteria are in its GitHub issue, milestone "1.2 Teams".)

## Sprint plan (capacity: 13 points per sprint, assumed)

The Chapter 22 planning tool's first draft (`plan docs/planning/backlog.csv --capacity 13 --as-ordered`) filled sprint 1 to
13 of 13 by pulling T-6 forward. We changed one thing: **T-6 moves to sprint 3**, so sprint 1, with no history yet, has slack.

| Sprint | Goal | Stories | Points |
|---|---|---|---|
| 1 | A team exists, behind a feature flag, and the data model is decided | T-1, T-2 | 10 / 13 |
| 2 | Team tasks are private to the team, and can be assigned | T-3, T-4 | 13 / 13 |
| 3 | The team lead's view, leaving a team; release | T-5, T-6, T-7, T-8 | 12 / 13 |

**Release:** end of sprint 3, if the Must and Should stories are done; T-7 and T-8 (Could) move to 1.3 if needed.
Teams stay behind the `Teams` feature flag until T-3 is done (an unsecured team feature must never be on).

## Forecast, honestly

- **No velocity history**: the 13-point capacity is a guess. After sprint 1 we use real numbers and re-plan.
- With the Chapter 22 forecast tool, *if* our sprints turn out like the example team's (13–18 points), 35 points take
  3 sprints (50%) and 3 sprints (85%); if we only finish 8–10 points per sprint, it's 4–5 sprints. We'll know which after sprint 1.

## Risks

| Risk | Effect | What we do |
|---|---|---|
| Visibility rules leak a team task (T-3) | a client's work seen by the wrong person | security tests for every endpoint first (NFR-SEC-TEAM-1); threat model update |
| The data model is wrong (T-1) | rework in every later story | a time-boxed spike and an ADR before T-2 |
| Invitations by email are abused | spam from TaskFlow's domain | rate limit; only registered users can be invited (FR-TEAM-1) |
