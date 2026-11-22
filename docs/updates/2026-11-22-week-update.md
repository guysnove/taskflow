# TaskFlow — engineering update, week ending 2026-11-22

Course 2, Chapter 23. A weekly update for the team and the product owner. Five minutes to read; the first
three lines are enough if that's all you read.

**TL;DR:** Release 1.2 "Teams" is planned and ready to start (8 stories, 35 points, about 3 sprints).
We decided how team tasks will be kept private (ADR 0003). No user-facing changes this week; no incidents.
**One decision needed:** can a task move from someone's personal list into a team? (see "Asks")

## Done this week

- **Overdue filter** shipped (PR #2): `/tasks?show=overdue`. Small, but the first feature through the new review process.
- **How we work, written down:** CONTRIBUTING.md (PR #1), planning docs, Definitions of Ready and Done (PR #3).
- **Documentation:** a new README, getting started (tested in a fresh clone), API reference with real examples, architecture diagrams.
- **Release 1.2 planned:** milestone "1.2 Teams", issues #4–#11.

## Decided

- Team tasks get a team id; who can see a task is decided in one query (ADR 0003, after a design review).
  We chose the simplest option that keeps the security rule in one visible place.

## Risks and problems

- **No velocity history yet**, so the 3-sprint forecast is a guess. We'll re-plan after sprint 1.
- **A drill showed a gap:** a wrong database setting takes the site down, and nothing alerts us.
  Actions are in the incident report; two are scheduled before release 1.2.

## Asks

- **Product owner:** can a task move from a personal list into a team, and back? We need the answer before T-2 (sprint 1).

## Next week

Sprint 1: the data-model spike (T-1) and creating teams (T-2), behind a feature flag.
