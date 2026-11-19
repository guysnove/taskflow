# TaskFlow requirements

Course 2, Chapter 6. These files record **what** TaskFlow must do and **why**, before anyone decides **how**.

| File | What it holds |
|---|---|
| `current.md` | TaskFlow's existing features, written down as requirements (reverse-engineered from Courses 1–2) |
| `notifications-and-teams.md` | The analysis for the next big feature: task notifications and team collaboration |

## How a requirement is written

- **One ID per requirement**, never reused: `FR-` functional, `NFR-` non-functional, `US-` user story, `UC-` use case.
- **One requirement per row.** If a row contains "and", consider splitting it.
- **Testable.** Each one says how we'll know it's met: an acceptance criterion, a number, or a test.
- **Traceable.** Tests that prove a requirement carry its ID: `[Trait("Req", "FR-DUP-1")]`.
  `dotnet run --project tools/Traceability` prints which requirements have tests and which don't yet.

## Priorities (MoSCoW)

**Must**: without it, the release is pointless or unsafe. **Should**: important, but there's a workaround. **Could**: nice to have, if time allows. **Won't (this time)**: agreed and written down as out of scope.
