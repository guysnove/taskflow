# How we plan TaskFlow

Course 2, Chapter 22. A small team (or one person) can use this as it is. Short on purpose; change it in a pull request.

## The process: Scrum-lite, with a Kanban board

| | |
|---|---|
| **Sprint** | 2 weeks. A **sprint goal** in one sentence. |
| **Product owner** | decides *what* and in which order (the backlog). For the course: the person who wrote the requirements (Maya, the team lead, in `docs/requirements/notifications-and-teams.md`). |
| **Developers** | decide *how*, and how much fits in a sprint. |
| **Sprint planning** (≤ 2 h) | pick the goal; pull stories from the top of the backlog until the capacity is used; split anything over 8 points. |
| **Daily** (15 min) | what's blocked? What's closest to done? Not a status report for a manager. |
| **Review** (1 h) | show working software, from `main`, to the product owner. Update the backlog with what we learned. |
| **Retrospective** (1 h) | what to keep, what to change, **one or two** actions with an owner (`retrospectives/`). |
| **Refinement** (weekly, 1 h) | write acceptance criteria and estimate the next stories, so planning is quick. |

## The board (GitHub issues, milestone per release)

| Column | Meaning | WIP limit |
|---|---|---|
| Backlog | an issue with the `story` label, in the release milestone | — |
| Ready | meets the Definition of Ready | — |
| In progress | someone is working on it; a branch exists | **3** |
| In review | a pull request is open | **2** |
| Done | merged, and meets the Definition of Done | — |

WIP limits: when a column is full, **finish or help** before starting something new. Stop starting, start finishing.

## Documents

- [definition-of-done.md](definition-of-done.md): the Definitions of Ready and Done.
- [estimation.md](estimation.md): story points, and TaskFlow's reference stories.
- [backlog.csv](backlog.csv) and [release-1.2-teams.md](release-1.2-teams.md): the next release.
- [retrospectives/](retrospectives/): one file per retrospective.
