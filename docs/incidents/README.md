# Incidents

Course 2, Chapter 23. What to do when TaskFlow is broken for users, and how we write it up afterwards.

## During an incident

1. **Say it out loud**: post "investigating: <what users see>" where the team talks. One person leads; others help.
2. **Mitigate first, understand later**: roll back the last change, restart, or switch a feature flag off. Users first.
3. **Write a timeline as you go** (UTC times, what was seen, what was done). Memory is unreliable an hour later.
4. **Say when it's over**, and what users should do, if anything.

| Severity | Meaning | Example |
|---|---|---|
| SEV-1 | TaskFlow is down, or data is lost or exposed | the app won't start; a user can see another user's tasks |
| SEV-2 | a main feature is broken for many users | no emails sent for an hour |
| SEV-3 | something minor is broken, or a workaround exists | one page shows a wrong count |

## Afterwards: the incident report

Within a few days, for every SEV-1 and SEV-2: one page, **blameless** (about the system and the process, not a
person), using the sections in the example below: summary, impact, timeline, root cause, what went well,
what didn't, and actions with owners. Reports are reviewed like code, in a pull request.

| Report | Severity | Summary |
|---|---|---|
| [2026-11-22-drill-database-path.md](2026-11-22-drill-database-path.md) | SEV-1 (drill) | a settings change pointed the database at a missing folder; TaskFlow wouldn't start |
| [2026-11-23-drill-mail-port.md](2026-11-23-drill-mail-port.md) | SEV-2 (drill) | a wrong mail port: emails given up; `/health/details` Degraded; fixed and re-sent |
