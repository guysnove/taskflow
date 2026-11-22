# Incident report: TaskFlow wouldn't start after a settings change (drill)

- **Severity:** SEV-1 (TaskFlow down for every user) · **Status:** resolved · **Type:** a **drill**, not a real outage
- **Date:** 2026-11-22 (Course 2, Chapter 23) · **Written by:** the on-call developer · **Reviewed in:** a pull request

> This was a planned drill ("game day"), run on a development machine with `tools/incident-drill.sh`.
> Everything in the timeline really happened, in a few seconds rather than the minutes or hours a real one takes.

## Summary

A deploy changed the database setting to `/var/lib/taskflow/data/taskflow.db`. That folder didn't exist, so
TaskFlow crashed on startup while applying migrations (`SQLite Error 14: unable to open database file`).
Rolling the setting back restored service. No data was lost.

## Impact

- Every user: the site didn't answer at all (health check: no response) from the deploy until the rollback.
- In the drill: about 30 seconds. In production, with systemd restarting it, TaskFlow would crash-loop until someone noticed.
- No data lost: the real database was never touched.

## Timeline (UTC, from the drill's own output)

```
13:07:21  version 1 running (database: /tmp/ch23-drill/taskflow.db)
13:07:33  GET /health → 200
13:07:33  DEPLOY: new settings, database 'Data Source=/var/lib/taskflow/data/taskflow.db' (that folder doesn't exist here)
13:07:47  GET /health → 000
13:07:52  GET /health → 000
13:07:52  process still running? no
13:07:52  the log says:
        "Message":"An error occurred using the connection to database \u0027main\u0027 on server \u0027/var/lib/taskflow/data/taskflow.db\u0027.
        Unhandled exception. Microsoft.Data.Sqlite.SqliteException (0x80004005): SQLite Error 14: 'unable to open database file'.
           at Microsoft.Data.Sqlite.SqliteException.ThrowExceptionForRC(Int32 rc, sqlite3 db)
13:07:52  MITIGATE: roll back to the previous settings and restart
13:08:06  GET /health → 200
13:08:08  drill over
```

## Root cause

The new setting pointed at a folder that doesn't exist on the server. Nothing checked it before the deploy, and
TaskFlow only finds out when it opens the database, at startup.

Why didn't anything catch it earlier? (five whys, short)
1. The app crashed → the database folder didn't exist.
2. The folder didn't exist → the setting was written for a different server layout.
3. Nobody noticed before users did → there's no smoke test after a deploy, and no alert on `/health`.

## What went well

- The log said exactly what was wrong (`unable to open database file`, and the path).
- Rolling back one setting fixed it; the database was untouched.

## What didn't

- **Detection:** only a person running `curl /health` noticed. Nothing alerts on it.
- **The message:** a raw stack trace from `SampleData.PrepareAsync`; an operator has to read 30 lines to find the path.
- **Deploy:** settings changes go straight to production, with no check and no automatic rollback.

## Actions

| Action | Owner | When |
|---|---|---|
| After every deploy, check `/health`; if it fails, roll back automatically | CI/CD (Chapter 24) | before release 1.2 |
| Alert when `/health` fails for 1 minute | operations (Chapter 29) | before release 1.2 |
| At startup, check that the database folder exists, and fail with one clear sentence naming the setting | a developer | next sprint (add to the backlog) |
