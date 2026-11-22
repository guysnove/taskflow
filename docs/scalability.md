# Scalability

Course 2, Chapter 27. How much load one TaskFlow can take, what would break if we ran two, what we cache, and why.

## How much one copy takes (measured)

Conditions as in [performance.md](performance.md): Release, 2,000 tasks, 5 users back to back, 2 CPUs (the course machine).

| Page | Requests/s, one copy | Limited by |
|---|---|---|
| `GET /tasks` | about 125 | CPU (loading and sorting 2,000 tasks, rendering 50 rows) |
| `GET /projects` | 400–570 | CPU |
| `GET /notifications` | 1,000–1,550 | CPU |

**Capacity plan** (`exercises/ch27` `Ch27.Caching -- capacity`): 2,000 active users, 4 requests a minute each, a peak 3× the
average → 400 requests/s at the peak. One copy at a 60% target (we never plan for 100%: latency explodes near it) serves about
77 requests/s of the heaviest page → **6 copies for the peak, 7 with one spare**. Today TaskFlow has far fewer users: one copy is plenty.

## The cache (NFR-PERF-6)

Every page shows the header's due counts: two `COUNT` queries. `CachingTaskRepository` (a decorator, `TaskFlow.Infrastructure/Caching`)
keeps them per user for `Caching:DueCountsSeconds` (30 s; 0 turns it off), and every task write through TaskFlow removes the entry.

Measured with the same build, the cache off and on, alternating (off, on, off, on), 5 users:

| Page | SQL commands (off → on) | Requests/s, cache off | Requests/s, cache on |
|---|---|---|---|
| `GET /notifications` | 4 → 2 | 1,008 · 1,054 | 1,559 · 1,526 (+50%) |
| `GET /projects` | 4 → 2 | 402 · 444 | 502 · 567 (+20–28%) |
| `GET /tasks` | 5 → 3 | 125 · 129 | 132 · 118 (no clear change: within the noise) |

The cache helps light pages, where the two counts were a big share of the work, and makes no measurable difference to the heavy one.

## Running two copies (a demonstration, not a plan)

`docker compose` with two TaskFlow containers sharing one `/data` volume, behind nginx (round robin) — see the course's Chapter 27.

| | With two copies | Why |
|---|---|---|
| Sign-in | works on both | the sign-in keys are in the shared volume (`DataProtection__KeysPath`) |
| The header's due counts | **stale on the other copy for up to 30 s** after a change | each copy has its own in-memory cache; only the copy that saw the write invalidated |
| The API rate limit (20 per 10 s) | **40 per 10 s** through the load balancer | each copy counts on its own |
| The database | works only because both copies are on one machine | SQLite is a file: it can't be shared across machines |
| Queued emails | lost if that copy stops (TD-13) | the queue is in one copy's memory |

## To scale out for real (not done; see ADR 0004)

1. The database: a server (PostgreSQL), not a file — every copy talks to it.
2. Shared state out of the process: the due-count cache and the rate limits in a shared store (Redis), or rate limits at the load balancer.
3. Sign-in keys in a shared store (the database, Redis or a cloud key vault), not a volume on one machine.
4. The email queue in a durable queue, or the database (TD-13).
5. Then: copies are stateless and interchangeable; add or remove them freely.

## Database scaling, in order of effort

Indexes and fewer, smaller queries (Chapters 14, 26) → a bigger machine → read replicas for read-heavy pages → caching shared
results → splitting data (by team or customer) only when nothing else is enough.
