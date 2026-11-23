# TaskFlow — technical debt register

Course 2, Chapter 9. Every item is something we know would make future changes slower or riskier.
**Principal** is the cost to fix it now. **Interest** is what it costs us while it stays.
We pay first where interest is high and principal is low; we write down, on purpose, what we accept.

Sizes: S = under an hour, M = half a day, L = days. Last reviewed: milestone/ch09.

| ID | Debt | Where | Interest (why it hurts) | Principal | Decision |
|---|---|---|---|---|---|
| ~~TD-1~~ | ~~The "project must exist" rule is written twice: `ProjectExistsRule` and `TasksApiController.ProjectIsMineAsync`, with different messages.~~ | ~~API~~ | ~~A rule change needs two edits; the form and the API already disagree on the wording.~~ | ~~S~~ | **Paid in Chapter 15** (the API uses `ProjectExistsRule`; one message). |
| TD-2 | The task list passes `Show`, `Sort`, `Project`, `Q` to the view through `ViewData` strings, cast back in `Index.cshtml`. | Tasks list | No compiler check: a typo in a key is a runtime error. | M | Accept for now. Chapter 12 left it: a presentation detail inside one layer, not a boundary problem. |
| TD-4 | The API rate limit (20 per 10 s) is hard-coded, while the sign-in limit is a setting. | `Program.cs` | Can't tune it per environment without a release. | S | Accept until someone needs a different value. |
| TD-5 | Each task's page loads **all** the user's tasks to offer dependency choices. | `TasksController.DependencyChoicesAsync` | Grows with the number of tasks (Chapter 5); fine at today's sizes. | M | Accept; measure in Chapter 26 (performance). |
| TD-8 | Notification times are shown in UTC (`CreatedAt.ToString`), not the user's local time. | Notifications page | Confusing for anyone not in UTC. | S | Pay with time-zone settings (users have none yet). Added in Chapter 10. |
| TD-9 | "Overdue" is written twice: `TaskItem.IsOverdue` (C#, Domain) and `t.Due < today` in `EfTaskRepository.CountDueAsync` (SQL). | Domain / Infrastructure | A change to the rule needs two edits. EF Core can't translate a C# method into SQL. | S | Accept; a test (`DueCountsTests`) checks both agree. Added in Chapter 12. |
| ~~TD-3~~ | ~~`Program.cs` is ~190 lines: services, security, rate limits, pipeline and endpoints in one file.~~ | ~~Startup~~ | ~~Hard to see what's registered.~~ | ~~M~~ | **Paid in Chapter 13**: each layer registers its own services (`AddTaskFlowWeb`, `AddTaskFlowApplication`, `AddTaskFlowInfrastructure`); `Program.cs` is 93 lines, mostly the pipeline. |
| TD-10 | Adding a step computes `MAX(Position) + 1`, then inserts: two steps added at the same moment get the same position. | Steps (EfStepRepository) | Rare; steps may show in an unstable order. | S | Accept for now; Chapter 14 Guided 3 fixes it (a unique index, or a transaction). Added in Chapter 14. |
| TD-11 | Saved idempotency answers (`IdempotencyRecords`) are never deleted. | API | The table grows with every keyed POST. | S | Accept for now; a clean-up of records older than 24 hours belongs with the background jobs (Chapter 28). Added in Chapter 15. |
| TD-12 | The API's create and update bodies are `TaskInput`, the form's model: it includes `allowDuplicate`, which only the form uses. | API | The contract shows a field programs should never send; form changes change the API. | M | Accept; a separate request DTO is a contract change: do it with the next API version. Added in Chapter 15. |
| TD-13 | ~~Queued emails live in memory (`OutgoingEmails`): emails still waiting when the app stops or crashes are lost.~~ **Paid (Course 2, Chapter 28):** the queue is the `OutboxEmails` table; retries with backoff; nothing lost on stop. | Notifications | — | M | Done: ADR 0005. Added in Chapter 16. |
| TD-14 | `RequestTimingMiddleware` logs every request (method, path, status, ms) at Information. Since Chapter 29, `http.server.request.duration` measures the same thing, and the trace has each request. | Middleware | One log line per request: log volume (cost) grows with traffic; the numbers are better as metrics. | S | Pay soon: log only slow (> 1 s) or failed requests. Added in Chapter 29. |
| ~~TD-6~~ | ~~No tests for the edit form (save, conflict, unknown project).~~ | ~~Tasks~~ | ~~The riskiest form had no safety net.~~ | ~~S~~ | **Paid in Chapter 9** (`EditFormTests`). |
| ~~TD-7~~ | ~~`TasksController.Index` filtered and sorted in the controller (complexity 8); the project check was duplicated in the controller.~~ | ~~Tasks~~ | ~~Untestable without HTTP; two copies of a rule.~~ | ~~M~~ | **Paid in Chapter 9** (`TaskListQuery`, `ProjectExistsRule`). |

## Questions found while paying debt (not debt: behaviour to decide)

- **Q-1** Search matches whole words: "report" doesn't find "archive old reports" (seen in the golden master, `Approved/TaskList.approved.txt`). This is what FR-SRCH-1 says, but is it what users expect? Ask before changing — it's a behaviour change, not a refactoring.

## How to add an item

Name the code, say what it costs us *now* (interest), estimate the fix (principal), and record a decision with a reason. "Accept" is a valid decision when it's written down.
