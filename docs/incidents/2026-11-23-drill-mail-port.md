# Incident report: notification emails given up after a mail setting change (drill)

- **Severity:** SEV-2 (a main feature broken: no notification emails) · **Status:** resolved · **Type:** a **drill**, not a real outage
- **Date:** 2026-11-23 (Course 2, Chapter 29) · **Written by:** the on-call developer · **Reviewed in:** a pull request

> A planned drill on a development machine (`stage/obs_run.sh` in the course files), with short timings so it fits in a minute:
> `Email:OutboxFirstRetry` 2 s and `Email:OutboxMaxAttempts` 3 (production: 30 s and 6). Every output below is real.

## Summary

TaskFlow was started with `Email:Port=2525`, where no mail server listens. Two due-date reminder emails failed three times
each and were given up (dead). `/health/details` turned **Degraded**, which is the alert. We fixed the setting, restarted,
re-queued the two emails with the runbook's SQL, and both were delivered.

## Impact

- One user didn't receive two reminder emails until the fix (in production terms: up to about an hour of retries, then nothing).
- The site itself kept working: `/health` stayed `Healthy` the whole time, as designed (a restart wouldn't fix a mail server).

## Detection and diagnosis (from the drill's outputs)

```
$ curl -s localhost:5052/health            → Healthy
$ curl -s localhost:5052/health/details    → "status": "Degraded"
  "email-outbox": "2 email(s) given up in the last 24 h; 0 overdue by more than 10 min"
$ the outbox's log lines
  Warn  Email 1 to a***@example.com failed (attempt 1 of 3): SmtpException: Failure sending mail.; next try in 2 s
  Warn  Email 1 to a***@example.com failed (attempt 2 of 3): SmtpException: Failure sending mail.; next try in 4 s
  Erro  Email 1 to a***@example.com failed 3 times; given up (dead): SmtpException: Failure sending mail.
$ sqlite3 taskflow.db "SELECT Id, Attempts, DeadAt IS NOT NULL AS Dead, LastError FROM OutboxEmails"
  1   3   1   SmtpException: Failure sending mail.
  2   3   1   SmtpException: Failure sending mail.
```

Every attempt failed at once (a few milliseconds, `taskflow.email.send.duration`), not after the 30 s send timeout:
the connection was refused, so the address was wrong or the server was down, rather than slow. The settings showed port 2525.

## Mitigation

Corrected the mail setting (in the drill, a pickup folder stood in for the real server), restarted, then [runbook §6.3](../operations.md#63-emails-given-up-dead):

```
$ sqlite3 taskflow.db "UPDATE OutboxEmails SET DeadAt = NULL, Attempts = 0, NextAttemptAt = CreatedAt WHERE DeadAt IS NOT NULL"
2 rows re-queued
$ curl -s localhost:5052/health/details    → "status": "Healthy"
$ ls mail | wc -l                          → 2
```

## Root cause (five whys)

1. Emails were given up → every send failed.
2. Every send failed → nothing listened on the configured port.
3. The port was wrong → a settings change wasn't checked against the real mail server.
4. It wasn't checked → TaskFlow checks its settings at startup (`ValidateOnStart`), but never tries to reach the mail server.
5. Nobody would have known for an hour in production → until this chapter, a dead email was only a log line.

## What went well

- Nothing was lost: the outbox (Chapter 28) kept both emails, with the error, and sending them again was one statement.
- `/health/details` and the `Error` log line named the problem without reading code.

## What didn't

- The error text, "Failure sending mail.", hides the inner exception ("Connection refused"); the attempt's speed was the clue.
- Re-queuing needs SQL on the production database.

## Actions

| Action | Owner | When |
|---|---|---|
| Alert on `/health/details` Degraded (ticket), and page when more than 10 emails are dead | operations | done in [operations.md §5](../operations.md#5-alerts) |
| Log the inner exception's message in `LastError` ("Failure sending mail. → Connection refused") | a developer | **done** (Course 2, Chapter 30: `EmailOutboxProcessor.Describe`) |
| An admin page or command to re-send dead emails, instead of SQL | a developer | backlog |
| After a deploy, a smoke test that sends one email to a test mailbox | CI/CD | partly done (Chapter 30: `tools/smoke.sh` after every staging deploy and container build; email not yet) |
