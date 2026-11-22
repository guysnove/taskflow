# Estimation: story points

Course 2, Chapter 22.

**Story points** measure the relative size of a story (effort, complexity and uncertainty together), **not hours**.
We use the scale **1, 2, 3, 5, 8, 13** (anything bigger is an epic: split it). We estimate together
(planning poker: everyone shows a card at once; the highest and lowest explain; estimate again).

## Reference stories (real TaskFlow work)

We compare new stories with work we have actually done. The lines changed are shown for context only: points are not lines.

| Points | Reference story | What it involved | Lines (code + tests) |
|---|---|---|---|
| 1 | Overdue filter (FR-LIST-2, PR #2) | one query rule, one link, 2 tests | +43 −4, 3 files |
| 3 | Duplicate-title warning (Ch. 2) | a new class with a clear algorithm, tests, a UI hint | +130, 6 files |
| 3 | Task list search (Ch. 3) | a data structure, query, UI | +126 −7, 5 files |
| 8 | Task dependencies with cycle detection (Ch. 4) | a migration, a graph algorithm, UI, many edge cases | +725, 15 files |
| 13 | Notifications, in-app and email (Ch. 10) | a new feature area: patterns, a worker, email, a migration | +1291, 27 files |

## Velocity

Velocity = points **done** (Definition of Done) per sprint. Use the last three sprints, as a range, never as a promise.
TaskFlow has **no sprint history yet**, so release 1.2 starts with an assumed capacity of **13 points** per sprint
(one developer, part-time) and will be re-planned with real numbers after sprint 1.
