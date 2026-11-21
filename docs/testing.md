# How TaskFlow is tested

Course 2, Chapter 17. Read this before adding a feature: every rule you add needs a test here.

## 1. Two kinds of test, two folders

| | `TaskFlow.Tests/Unit/` | `TaskFlow.Tests/Integration/` |
|---|---|---|
| Tests | one class (a rule, a use case, a component) on its own | several pieces together: the web app, EF Core, SQLite, migrations |
| Collaborators | test doubles (fakes, stubs, spies, NSubstitute mocks) | the real ones (an in-memory or temp-file SQLite database, `TaskFlowFactory`) |
| Speed | milliseconds | 10–1000 ms |
| Run | `dotnet test --filter "FullyQualifiedName~TaskFlow.Tests.Unit"` | `dotnet test --filter "Category!=Growth"` (everything) |

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
