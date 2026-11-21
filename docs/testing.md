# How TaskFlow is tested

Course 2, Chapter 17. Read this before adding a feature: every rule you add needs a test here.

## 1. Three kinds of test, three folders

| | `TaskFlow.Tests/Unit/` | `TaskFlow.Tests/Integration/` | `TaskFlow.Tests/EndToEnd/` (Chapter 18) |
|---|---|---|---|
| Tests | one class (a rule, a use case, a component) on its own | several pieces together: the web app, EF Core, SQLite, migrations | a person's journey, in a real browser |
| Collaborators | test doubles (fakes, stubs, spies, NSubstitute mocks) | the real ones (an in-memory or temp-file SQLite database, `TaskFlowFactory`) | everything real; Chromium driven by Playwright, through a real port |
| Speed | milliseconds | 10–1000 ms | seconds |
| Run | `dotnet test --filter "FullyQualifiedName~TaskFlow.Tests.Unit"` | `dotnet test --filter "Category!=Growth&Category!=E2E"` | `dotnet test --filter "Category=E2E"` (needs Chromium) |

**Rule:** if a test needs a database, a web server, a file or the network, it belongs in `Integration/`.
(`AccountServiceTests` moved there in Chapter 17: it used SQLite.)

## 2. What every unit test looks like

- **Name:** `Situation_Result` — `AFullQueue_DropsTheEmail_AndLogsAnError`. Read it as a sentence.
- **Arrange, Act, Assert**, in that order, separated by a blank line (or comments while you learn).
- **One behaviour per test.** Several asserts are fine if they describe that one behaviour.
- **Expected values are plain values** (`Assert.Equal(75m, quote.Total)`), never recomputed with the code's own formula.
- **The most specific assertion:** `Assert.Equal`, `Assert.Single`, `Assert.Contains`, `Assert.ThrowsAsync<T>`, not `Assert.True(a == b)`.
- **Boundaries as `[Theory]`:** just below, at, and just above every limit (4 and 5 wrong passwords; expiry yesterday/today).
- **Time** comes from a `FakeTimeProvider`; never `DateTime.Now`.
- **Requirements:** `[Trait("Req", "FR-…")]` so `tools/Traceability` can find the test.

## 3. Test doubles: which one?

| Need | Use | In TaskFlow |
|---|---|---|
| a parameter that must not be used | dummy | — |
| a fixed answer | stub (hand-written, or NSubstitute `.Returns`) | `IProjectRepository` in `ProjectExists_*` |
| a working, simpler version | fake | `FakeAccountStore`, `InMemoryTaskRepository` |
| "what did it receive?" — state afterwards | spy | `SpyChannel`, `SpyQueue`, `RecordingObserver` |
| "was it called, with what?" | mock (NSubstitute `.Received`) | `DidNotReceive().FindAsync(...)` |
| what was logged | `FakeLogger<T>` (Microsoft.Extensions.Diagnostics.Testing) | `AFullQueue_DropsTheEmail_AndLogsAnError` |

Prefer checking **results and state** over checking calls. Check calls only when the call *is* the behaviour (an email queued, the database *not* asked).

## 4. Running tests in VS Code

- **Ctrl+Shift+P → "Tasks: Run Test Task"** runs `test: unit` (`.vscode/tasks.json`). **Terminal → Run Task…** lists all of them: `build`, `test: unit`, `test: all`, `test: coverage of the business logic`.
- With the **C# Dev Kit** extension (where its licence allows), the **Testing** view lists every test, runs or debugs one with a click, and shows ▶/✓/✗ next to each `[Fact]`.
- From the terminal: `dotnet test`, `dotnet test --filter "FullyQualifiedName~SignInRulesTests"`, `dotnet test --filter "Req=FR-ACC-1"`.

## 5. Coverage: a guide, not a goal

`./tools/coverage.sh` runs the unit tests and reports which lines of `TaskFlow.Domain` and `TaskFlow.Application` they ran
(`TestResults/coverage/report/index.html` shows them line by line).

| | Line | Branch |
|---|---|---|
| Start of Chapter 17 | 70.9% | 83% |
| End of Chapter 17 | 81.4% | 96.1% |

Use it to **find untested rules** (Chapter 17 found `ProjectExistsRule`, the email channel and the "unblocked" observer at 0%).
Don't chase a number: an uncovered line can be harmless (`IsWarning => false`), and 100% coverage with weak assertions proves little.

## 6. Integration and end-to-end tests (Chapter 18)

- **One app per test class:** `IClassFixture<TaskFlowFactory>` (or `WorkflowFactory`). Starting the app takes a second; sharing it is fine because…
- **…every test makes its own data.** `SignedInClientAsync()` registers a new account with a unique email, so tests share a database but never each other's rows. Never assume an empty database, an id, or a count of *all* rows: count *your* rows.
- **Check the database, not only the answer.** `factory.DbAsync(db => …)` looks at what a request really stored (owner, dates, blank fields). That is how the blank-notes bug was found.
- **The environment is a decision.** `TaskFlowFactory` runs in Development; `WorkflowFactory` runs in **Testing**: no sample tasks (in Development the first account adopts them — that made a test depend on which test registered first), production error handling, static files from the build (`UseStaticWebAssets`).
- **Only the clock is fake** in workflow tests (`FakeTimeProvider`), so "created today" has one right answer.
- **The API needs the anti-forgery token** in `X-CSRF-TOKEN` when it is called with the sign-in cookie, just like the forms.
- **End-to-end tests are few:** one per important journey (register → add a task). They find what HTTP-level tests can't — the screenshot of the first run showed a page with no CSS. Playwright's assertions wait; never `Thread.Sleep`.
- **If a test fails only sometimes,** it is a bug in the test or the app: find the shared state, the timing, or the order dependency. Don't re-run until green.

## 7. Test-driven development and quality gates (Chapter 19)

- **New rules are written test-first** when the rule can be stated with examples. FR-PRIO-1 (`PriorityRules`) was built in 14 small red–green–refactor commits; `PriorityRulesTests` reads in the order it was written.
- **Red means red for the right reason:** a compile error counts as red, but then make it compile and watch the *assertion* fail.
- **Refactor only on green**, and only the code *or* the tests, not both at once.
- **The golden master** (`TaskListGoldenMasterTests`) didn't notice FR-PRIO-1: its data has no Low or Medium task due soon. A regression test only protects what its data exercises.
- **Quality gate:** CI runs `./tools/coverage.sh`, which fails if unit-test coverage of Domain + Application drops below **80% of lines** or **90% of branches** (today: 82% / 95.2%). It's a floor against erosion, not a target. Try it: `MIN_LINE=85 ./tools/coverage.sh`.
- **Every pull request** uses `.github/pull_request_template.md`: how it was tested, and whether each new test was seen to fail.

