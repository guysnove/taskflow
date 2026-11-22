# Course 2 chapter exercises

Small programs used in Course 2 chapters live in `exercises/chNN/`, separate from TaskFlow itself.

| Folder | Chapter | What it is |
|---|---|---|
| `exercises/ch01/BrightCart.Orders` | 1 | A deliberately flawed legacy console app (fictitious shop) to review. Run with `dotnet run` in its folder. |
| `exercises/ch01/MoneyDemo` | 1 | `money.cs`: double vs decimal for money. Run with `dotnet run money.cs` (.NET 10 file-based app). |
| `exercises/ch02` | 2 | `Ch02.slnx`: duplicate finding (pairs vs groups), discount rules, scheduling strategies, with 23 tests. `Ch02.Demo`: `dotnet run -c Release -- duplicates` / `dotnet run -- schedule`. `ZuneBug/zune.cs`: the case study's loop. |
| `exercises/ch03` | 3 | `Ch03.slnx`: array/list, linked list, stack, ring queue and hash map built from scratch, plus a task search (List vs Dictionary), with 29 tests. `Ch03.Demo`: `capacity`, `locality`, `search`, `hash`, `hashcode`, `modify`. |
| `exercises/ch04` | 4 | `Ch04.slnx`: recursion, a binary search tree, a min-heap, a graph (BFS, cycle detection, topological sort), searching and sorting, fewest coins, with 29 tests. `Ch04.Demo`: `overflow N`, `bst`, `sort`, `search`, `plan`, `queue`, `fib`, `stable`, `tree PATH`, `range`. |
| `exercises/ch05` | 5 | `Ch05.slnx`: step counters, de-duplication, six searches, snippets to analyse, space and cases, with 32 tests. `Ch05.Demo`: `dedupe`, `steps`, `doubling`, `cases`, `concat`, `space`, `jit`, `setup`. `Ch05.Benchmarks` (BenchmarkDotNet): `dotnet run -c Release -- --filter "*"`. |
| `exercises/ch06` | 6 | `Ch06.slnx`: US-2's acceptance criteria as tests (`ReminderPolicy`) and a tiny requirement linter, with 11 tests. `Ch06.Demo`: `interviews`, `ours`. |
| `exercises/ch07` | 7 | `Ch07.slnx`: a messy `DigestService` and its refactored `DailyDigest`, with characterisation tests (6); `CsvImport` to refactor (Independent B). `Ch07.Demo`: `before`, `after`, `clockbug`. |
| `exercises/ch17` | 17 | `Ch17.slnx`: a small shop's `PriceCalculator` with 17 unit tests (AAA, theories, the five test doubles, isolation); `Ch17.Mistakes.Tests` fails **on purpose** (2 of 4); `Ch17.Legacy/LateFees.cs` to make testable (Guided 2). `.vscode/tasks.json`: run tests as VS Code tasks. |
| `exercises/ch18` | 18 | `Ch18.slnx`: a tiny notes app (web page + JSON API + SQLite) with integration tests (`WebApplicationFactory`, a test sign-in scheme, real cookie sign-in), end-to-end tests with Playwright (`Category=E2E`, needs Chromium) and a deliberately flaky test (`Category=Flaky`). |
| `exercises/ch19` | 19 | `Ch19.slnx`: a shipping-cost kata built test-first (14 recorded steps), with 13 tests. Tasks: `test`, `test: watch` (`dotnet watch test`), `test: mutation (Stryker)` (`dotnet tool restore` first). |
| `exercises/ch20` | 20 | `Ch20.slnx`: a notes service with four classic vulnerabilities (`/bad/...`) next to their fixes (`/good/...`), 12 attack tests, `demo.sh` (curl attacks on localhost), and `Ch20.Audit` (a deliberately vulnerable package, to see NuGet audit; **not** part of the build gate). Never deploy the `/bad` endpoints. |
| `exercises/ch21` | 21 | `setup.sh` builds a small team on your computer: `team/origin.git` (a stand-in for GitHub), `team/ana` and `team/ben`. `start/` is the invoice library they share; `steps/` holds each developer's edits, the conflict resolution, the history with a hidden bug, and `bisect.sh` for `git bisect run`. |
| `exercises/ch22` | 22 | `Ch22.slnx`: a small planning tool (`dotnet run --project Ch22.Planning -- prioritise|plan|velocity|forecast|burndown|flow …`) with 10 tests, and example data in `data/` (an invented backlog, sprint history, burndown and Kanban board). |


Each folder builds and tests on its own; `exercises/Directory.Build.props` keeps TaskFlow's build rules out of them.
