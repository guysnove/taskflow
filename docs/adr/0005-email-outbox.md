# ADR 0005 — Emails go through an outbox table; TaskFlow stays one modular monolith

- **Status:** accepted
- **Date:** 2026-11-23 (Course 2, Chapter 28)
- **Pays:** TD-13

## Context

Since Chapter 16, notification emails waited in an in-memory queue: a restart or crash lost them. A mail server is another
system across a network: it can be slow, down, or accept an email and fail to say so. We wanted safe retries, no duplicates
caused by our own retries, and nothing lost — without new infrastructure.

## Decision

- **Outbox.** `EmailChannel` adds a row to `OutboxEmails` (in TaskFlow's database) and returns. The row's `Key`
  (user + notification) is unique: queuing the same email twice is impossible (idempotent producer).
- **Processor.** A background worker sends due rows. On failure: `Attempts + 1`, try again after an exponentially growing
  wait (30 s, 1 min, 2 min … 30 min) with up to 20% random jitter; after 6 attempts the row is set aside (`DeadAt`) and logged.
- **One sender per email.** A row is claimed with a conditional `UPDATE` (`ClaimedUntil`): of two senders, one wins.
- **At least once.** If TaskFlow dies after the mail server accepted an email but before `SentAt` is saved, it is sent again
  on restart. Rare, and a duplicate email is better than a lost one here. (Exactly once over a network isn't possible without
  the other side's help, e.g. a provider that deduplicates by a message id.)
- **Still one deployable.** Email stays a module of the monolith, not a separate service.

## Consequences

- Restarts and deploys lose no emails. Failures are visible as rows (`LastError`, `DeadAt`), not only as log lines.
- Retries at two levels: the SMTP decorator tries 3 times quickly (blips), the outbox retries over minutes (outages).
  The worst case is 6 × 3 = 18 sends attempted for one email over about an hour: bounded, and logged.
- The table grows: sent rows should be deleted after, say, 30 days (not done yet).

## Alternatives considered

- A message broker (RabbitMQ, a cloud queue): durable and scalable, but a new service to run for a few emails a day.
- A separate "notification service": a network hop and a second deployment for code that is a few hundred lines; the module
  boundary (Chapter 11) already gives us the separation. We'd revisit if another team had to own and deploy it independently.
