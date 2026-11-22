# Performance

Course 2, Chapter 26. What "fast enough" means for TaskFlow, how we measure it, and what we found and changed.

## Requirements

| ID | Requirement | Measure |
|---|---|---|
| NFR-PERF-1 | The overdue summary's cost does not grow with the number of tasks. | growth test (Course 2, Chapter 5) |
| NFR-PERF-2 | The overdue summary's query uses an index on (UserId, Due). | `QueryPlanTests` |
| NFR-PERF-4 | With 2,000 tasks, `GET /tasks` p95 under 100 ms for 5 users at once. | load test, conditions below |
| NFR-PERF-5 | The task list's step counts are one query per page, never one per task. | `QueryPlanTests` |

## How we measure (the conditions matter as much as the numbers)

| | |
|---|---|
| Build | Release (`dotnet publish -c Release`), `ASPNETCORE_ENVIRONMENT=Production` |
| Data | a fresh SQLite database; one user, `tools/PerfSeed`: 2,000 tasks, 20 projects, 3 steps each, seed 26 |
| Machine | the course's recording machine: 2 CPUs, 7 GB RAM, Linux; nothing else running (the video renderer paused) |
| Load | `exercises/ch26` `Ch26.Load`: N users, each sending the next request as soon as the last answered; 3 s warm-up, then 10 s measured |
| Report | requests per second; latency p50, p90, p99, max |

```bash
dotnet run --project tools/PerfSeed -- --db <your taskflow.db> --email you@example.com --tasks 2000
dotnet run -c Release --project exercises/ch26/Ch26.Load -- http://localhost:5050/tasks --users 5 --seconds 10 --email you@example.com --password …
```

Numbers from another machine are not comparable with these. Compare before and after **on the same machine, the same day**.

## What we found (22 November 2026)

| Page | Before | After | Change |
|---|---|---|---|
| `GET /tasks`, 1 user | p50 70.8 ms, 13.5/s | p50 14.9 ms, 62.8/s | list paged (50), steps counted per page |
| `GET /tasks`, 5 users | p50 210 ms, p99 442 ms, 21.9/s | p50 34.2 ms, p99 80.2 ms, 127.8/s | |
| `GET /tasks/1000`, 1 user | p50 47.7 ms, 19.9/s | p50 8.4 ms, 111.9/s | "waits for" list: a projection |
| `GET /projects`, 5 users | 865/s | 870/s | unchanged (it was never slow) |

1. **The list rendered every task.** 2,000 rows, 2.1 MB of HTML per request. A trace (`dotnet-trace`, sampled thread time) put
   15% of the samples in loading the tasks and 9% in rendering the page. Now: 50 a page, with Previous / Next.
2. **The list loaded every task's steps** (6,000 rows) to print "2 of 3 steps done". Now `GetAllAsync` loads no steps, and
   `CountStepsAsync` counts them for the 50 tasks on the page, in one `GROUP BY` query.
3. **We tried an N+1 on purpose:** counting steps with two queries per task. 104 SQL commands per page instead of 5, and
   `GET /tasks` p50 21.7 ms instead of 17.1 ms (1 user). With SQLite, inside the process, a query costs little; with a database
   server across a network every query is a round trip (`exercises/ch26/Ch26.Db`: 501 queries took 63 ms on PostgreSQL in a
   container, 6 ms on SQLite; one JOIN took under 1 ms on both). `NFR-PERF-5` keeps it from coming back.
4. **The task page loaded every task, with its project,** for a drop-down of titles. Now it reads two columns.

Under 5 users, `dotnet-counters` showed the machine's CPUs full both before and after: what changed is the work per request.
Memory allocated per request went from about 26 MB (577 MB/s at 21.9 requests/s) to about 3.4 MB (439 MB/s at 127.8/s), and the
time paused for garbage collection from 162 to 34 ms per second.

## Not done (yet)

- Filtering and sorting still happen in memory, after loading all of a user's tasks (without steps). It's fast enough at 2,000
  tasks (NFR-PERF-4); at 20,000 it wouldn't be. Moving them into the query is a Chapter 26 exercise.
- The task page's "waits for" drop-down still lists every task (186 KB of HTML at 2,000 tasks). A search box would scale better.
- The API's rate limit (20 requests per 10 s per user, NFR-SEC-3) answers a load test with 429: test the API with many users, or raise the limit in a test environment.
