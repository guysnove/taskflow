# TaskFlow — current requirements (reverse-engineered)

Written down in Course 2, Chapter 6, from the features built in Courses 1 and 2, so new work can be traced against them.

## Functional requirements

| ID | Requirement | Source | Verified by |
|---|---|---|---|
| FR-ACC-1 | A visitor can register with an email and password, and sign in. | Course 1, Ch. 25 | tests |
| FR-OWN-1 | A user can see and change only their own tasks, projects, steps and dependencies. | Course 1, Ch. 25 | tests |
| FR-TASK-1 | A task has a title of 3 to 80 characters, and a priority of High, Medium or Low; otherwise the form explains what's wrong. | Course 1 | tests |
| FR-TASK-2 | A new task can't be due in the past (the form and the API). | Course 1; API since Course 2, Ch. 8 | tests |
| FR-TASK-3 | A new task can't be due more than five years ahead: almost always a typo. | Course 2, Ch. 8 | tests |
| FR-STEP-1 | A task can have an ordered checklist of steps; the task shows "2 of 5 done". | Course 1, Ch. 30 | tests |
| FR-LIST-1 | The task list can show all or only open tasks, one project's tasks, and sort by due date, priority or title; unknown values fall back to all tasks, by due date. | Course 1; written down in Course 2, Ch. 9 | tests (golden master) |
| FR-DUP-1 | Adding a task whose title matches an open task's title (ignoring case and spaces) shows a warning; the user may add it anyway. | Course 2, Ch. 2 | tests |
| FR-SRCH-1 | The task list can be filtered to tasks whose titles contain all the search words, in any order, ignoring case. | Course 2, Ch. 3 | tests |
| FR-DEP-1 | A task can wait for other tasks; a dependency that would create a loop is refused, and the loop is named. | Course 2, Ch. 4 | tests |
| FR-NOTIF-2 | A user gets one in-app notification for each open task due within N days (default 2), once per due date. | Course 2, Ch. 10 | tests |
| FR-NOTIF-3 | A user sees their notifications and unread count, and can mark them all as read. | Course 2, Ch. 10 | tests |
| FR-NOTIF-4 | A user can choose to also receive each notification by email. | Course 2, Ch. 10 | tests |
| FR-NOTIF-6 | Finishing a task notifies the owner of each task that was waiting only for it. | Course 2, Ch. 10 | tests |
| FR-DUE-1 | Every page shows how many open tasks are overdue, and how many are due within the next N days (default 2). | Course 1 | tests |

## Non-functional requirements

| ID | Requirement | Measure | Verified by |
|---|---|---|---|
| NFR-CONC-1 | Saving an edited task never silently overwrites a save made since the form was opened: the visitor sees a conflict, with both versions. | second save from a stale form | tests |
| NFR-CONC-2 | Two requests changing dependencies at the same moment never leave a loop. | opposite dependencies added simultaneously → one saved, one refused | tests |
| NFR-PERF-2 | The overdue summary's query uses an index on (UserId, Due), never a scan of a user's tasks. | EXPLAIN QUERY PLAN | tests |
| NFR-NOTIF-2 | Notification emails are retried and failures logged; a failing channel doesn't stop the others. | 3 attempts; logged | tests |
| NFR-PRIV-1 | Notification texts and emails contain the task title and a link, nothing else. | reviewed factory; test | tests |
| NFR-SEC-1 | Every response carries security headers, including a Content Security Policy and `nosniff`. | checked on the home page | tests |
| NFR-SEC-2 | State-changing requests require an anti-forgery token. | POST without token → 400 | tests |
| NFR-SEC-3 | The API is rate limited per signed-in user. | more than 20 requests in 10 s → 429; other users unaffected | tests |
| NFR-SEC-4 | Sign-in and registration are rate limited per IP address. | 6th try within a minute → 429 | not yet tested |
| NFR-OPS-1 | `/health` reports whether the app and its database are working. | 200 "Healthy" | tests |
| NFR-PERF-1 | The overdue summary's cost does not grow with the number of tasks. | `/tasks/new` stays flat from 100 to 8,000 tasks | growth test (measured, not pass/fail) |
