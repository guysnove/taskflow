# ADR 0004 — Run one copy of TaskFlow for now; cache only per-user counts, in memory

- **Status:** accepted
- **Date:** 2026-11-22 (Course 2, Chapter 27)
- **Evidence:** [scalability.md](../scalability.md)

## Context

One copy of TaskFlow serves about 125 requests/s on its heaviest page with 2,000 tasks, on 2 CPUs. Our users need a few
requests a second. Running two copies today would need a database server, a shared cache, shared rate limits and a durable
email queue (scalability.md, "Running two copies").

## Decision

- One copy, scaled **up** (a bigger machine) if it ever needs to be.
- An in-memory cache only for data that is per user, cheap to recompute and harmless if a few seconds old: the header's due counts (30 s).
- No shared (distributed) cache until we run more than one copy.

## Consequences

- Simple to run and debug; no Redis to operate.
- A change made outside TaskFlow (another copy, a script) shows in the header counts up to 30 s late.
- The day we need two copies, the list in scalability.md is the work to do first, and this ADR is superseded.

## Alternatives considered

- Redis now, for the cache and rate limits: correct across copies, but another service to run for a load we don't have.
- No cache: simplest; the light pages would be 20–50% slower (measured).
