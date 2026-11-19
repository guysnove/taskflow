# TaskFlow — technical debt register

Course 2, Chapter 9. Every item is something we know would make future changes slower or riskier.
**Principal** is the cost to fix it now. **Interest** is what it costs us while it stays.
We pay first where interest is high and principal is low; we write down, on purpose, what we accept.

Sizes: S = under an hour, M = half a day, L = days. Last reviewed: milestone/ch09.

| ID | Debt | Where | Interest (why it hurts) | Principal | Decision |
|---|---|---|---|---|---|
| TD-1 | The "project must exist" rule is written twice: `ProjectExistsRule` and `TasksApiController.ProjectIsMineAsync`, with different messages. | API | A rule change needs two edits; the form and the API already disagree on the wording. | S | **Pay next** time the API is touched (Chapter 8, Guided 3 shows how). |
| TD-2 | The task list passes `Show`, `Sort`, `Project`, `Q` to the view through `ViewData` strings, cast back in `Index.cshtml`. | Tasks list | No compiler check: a typo in a key is a runtime error. | M | Accept for now; revisit with view models in Chapter 12. |
| TD-3 | `Program.cs` is ~190 lines: services, security, rate limits, pipeline and endpoints in one file. | Startup | Hard to see what's registered; but it changes rarely and is well commented. | M | Accept; split into extension methods in Chapter 13 (dependency injection). |
| TD-4 | The API rate limit (20 per 10 s) is hard-coded, while the sign-in limit is a setting. | `Program.cs` | Can't tune it per environment without a release. | S | Accept until someone needs a different value. |
| TD-5 | Each task's page loads **all** the user's tasks to offer dependency choices. | `TasksController.DependencyChoicesAsync` | Grows with the number of tasks (Chapter 5); fine at today's sizes. | M | Accept; measure in Chapter 26 (performance). |
| ~~TD-6~~ | ~~No tests for the edit form (save, conflict, unknown project).~~ | ~~Tasks~~ | ~~The riskiest form had no safety net.~~ | ~~S~~ | **Paid in Chapter 9** (`EditFormTests`). |
| ~~TD-7~~ | ~~`TasksController.Index` filtered and sorted in the controller (complexity 8); the project check was duplicated in the controller.~~ | ~~Tasks~~ | ~~Untestable without HTTP; two copies of a rule.~~ | ~~M~~ | **Paid in Chapter 9** (`TaskListQuery`, `ProjectExistsRule`). |

## Questions found while paying debt (not debt: behaviour to decide)

- **Q-1** Search matches whole words: "report" doesn't find "archive old reports" (seen in the golden master, `Approved/TaskList.approved.txt`). This is what FR-SRCH-1 says, but is it what users expect? Ask before changing — it's a behaviour change, not a refactoring.

## How to add an item

Name the code, say what it costs us *now* (interest), estimate the fix (principal), and record a decision with a reason. "Accept" is a valid decision when it's written down.
