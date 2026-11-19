# Task notifications and team collaboration — requirements analysis

Course 2, Chapter 6. Status: **analysed, not built**. Version 1.0 of this document. Each change gets a line in the change log at the end.

## 1. The business problem (not the solution)

Brightside is a five-person design studio (a fictional example) that uses TaskFlow. Each person keeps their own list. Work for clients is shared by email and chat.

- Work **falls through the cracks**: last month, two client deadlines were missed. In both cases, the task sat in one person's list while that person was on leave.
- The team lead **can't see** who is working on what without asking everyone.
- People **forget due dates**, because TaskFlow only shows them when they open it.

**Goal:** fewer missed deadlines on shared work. **Measure of success:** no missed client deadline caused by "I didn't know" in the three months after release, according to the team lead's monthly review.

## 2. Stakeholders

| Stakeholder | Interest | Influence |
|---|---|---|
| Team lead (Maya) | sees all shared work; reassigns it when someone is away | high: she decides what's built |
| Team members (4) | know what's theirs and what's due, with few interruptions | high: they use it every day |
| Clients | deadlines met | indirect: they never use TaskFlow |
| Studio owner (pays) | low cost; no data leaks between clients | high: budget |
| Whoever runs TaskFlow (us) | email deliverability, security, support load | medium |

## 3. Raw statements from the interviews (as said)

> "I want to be notified about everything." (team member)
> "Notifications should be instant." (team lead)
> "Teams should work like Slack." (team member)
> "Nobody should be able to see client work they're not on." (studio owner)
> "Remind me before things are due." (team member)

Each statement is a starting point, not a requirement. Section 9 lists the questions they raise.

## 4. Functional requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-TEAM-1 | A user can create a team and invite other registered users by email. | Must |
| FR-TEAM-2 | A team task is visible to, and editable by, every member of its team, and by no one else. | Must |
| FR-TEAM-3 | A team task can be assigned to one team member, or left unassigned. | Must |
| FR-TEAM-4 | The team lead can see all of a team's open tasks, grouped by assignee. | Should |
| FR-TEAM-5 | A member can leave a team; their assigned open tasks become unassigned. | Should |
| FR-NOTIF-1 | When a task is assigned to a user, that user gets an in-app notification. | Must |
| FR-NOTIF-2 | A user gets a notification when a task assigned to them is due within N days (default 2). | Must |
| FR-NOTIF-3 | A user can see their unread notifications, and mark them as read. | Must |
| FR-NOTIF-4 | A user can choose to also receive each notification by email. | Should |
| FR-NOTIF-5 | A daily email summary of the day's due tasks. | Could |
| FR-COMM-1 | Comments on tasks. | Won't (this release) |
| FR-CHAT-1 | Real-time chat, like Slack. | Won't |

## 5. Non-functional requirements

| ID | Requirement | Measure | Priority |
|---|---|---|---|
| NFR-NOTIF-1 | An assignment notification appears in the app soon after assignment. | within 1 minute, 95% of the time | Must |
| NFR-NOTIF-2 | Notification emails are sent reliably. | under 1% fail permanently; failures are retried and logged | Should |
| NFR-SEC-TEAM-1 | No user can read or change a team's tasks unless they're a member. | integration tests for every team endpoint; 404, not 403 | Must |
| NFR-PRIV-1 | Emails contain the task title and a link, nothing else. | reviewed template | Must |
| NFR-PERF-2 | The notifications list stays fast for heavy users. | under 200 ms for 10,000 notifications (growth test) | Should |
| NFR-USE-1 | A user can turn email notifications off in two clicks or fewer. | usability check | Should |

## 6. User stories and acceptance criteria

### US-1 — Assign a team task (FR-TEAM-3, FR-NOTIF-1)

**As a** team lead, **I want to** assign a team task to a member, **so that** everyone knows who owns it.

- **Given** Maya and Sam are members of team "Brightside", **when** Maya assigns "Logo for Acme" to Sam, **then** the task shows "Assigned to Sam", **and** Sam has one unread notification: "Maya assigned you: Logo for Acme".
- **Given** Joe is not a member of "Brightside", **when** anyone tries to assign a Brightside task to Joe, **then** it is refused: "Joe is not a member of this team."
- **Given** a task assigned to Sam, **when** Maya assigns it to Lena, **then** Lena is notified, **and** Sam is not.

### US-2 — Be reminded before a due date (FR-NOTIF-2)

**As a** team member, **I want** a reminder before my tasks are due, **so that** I don't miss deadlines.

- **Given** an open task assigned to Sam, due on Thursday, with N = 2, **when** it is Tuesday, **then** Sam gets one notification: "Due Thursday: Logo for Acme".
- **Given** the same task, **when** the reminder has already been sent, **then** Sam doesn't get a second one for the same due date.
- **Given** the task is done, or its due date has passed, **then** no reminder is sent.

### US-3 — See and clear my notifications (FR-NOTIF-3)

**As a** user, **I want** to see what's new and mark it read, **so that** I know what needs my attention.

- **Given** Sam has 3 unread notifications, **then** every page shows "3" next to the notifications link.
- **When** Sam opens the notifications page and chooses "Mark all as read", **then** the count disappears.

### US-4 — Keep client work private (FR-TEAM-2, NFR-SEC-TEAM-1)

**As the** studio owner, **I want** team tasks visible only to team members, **so that** client work doesn't leak.

- **Given** Joe is not a member of "Brightside", **when** Joe requests any Brightside task by its URL, **then** he gets 404, exactly as if it didn't exist.

## 7. Use case UC-1 — Reassign work when someone is away

**Primary actor:** team lead. **Goal:** nothing due is left with someone who's away. **Precondition:** the lead is signed in; the team exists.

**Main success scenario**
1. The lead opens the team's overview (FR-TEAM-4).
2. TaskFlow lists open tasks grouped by assignee, with due dates.
3. The lead chooses a task assigned to the absent member, and picks another member.
4. TaskFlow assigns it, and notifies the new assignee (FR-NOTIF-1).
5. Repeat from step 3 until no urgent task is left with the absent member.

**Extensions**
- 3a. The new assignee has left the team in the meantime → TaskFlow refuses, and shows the current member list.
- 4a. The new assignee has chosen email notifications → an email is also sent (FR-NOTIF-4).

## 8. Scope

**In this release:** teams, membership, assignment, in-app notifications (assignment and due soon), and the optional email.
**Not in this release:** comments (FR-COMM-1), chat (FR-CHAT-1), mobile push notifications, and per-task permissions inside a team.
**Change control:** a new idea goes to the backlog with an ID. It enters this release only if something of the same size leaves.

## 9. Ambiguities found, and how they were resolved

| Statement | Question | Answer (from the team lead, 18 Nov) |
|---|---|---|
| "notified about everything" | Every edit by anyone? Every comment? | No: assignment and due soon only. "Everything" would be noise. |
| "instant" | In the browser while open? Email within seconds? | Within a minute is fine (NFR-NOTIF-1). |
| "like Slack" | Chat? Channels? Or just shared tasks? | Just shared tasks for now. Chat is out of scope. |
| "before things are due" | How long before? Weekends? | 2 days, configurable, counting calendar days (same as FR-DUE-1). |
| "a team task" | Can a task belong to two teams? | No: one team, or none (personal). |
| "notifications" | Who gets one when you assign a task to yourself? | Nobody: you already know. |

## 10. Traceability

| Requirement | User story / use case | Tests (planned) | Development tasks |
|---|---|---|---|
| FR-TEAM-1 | — | `TeamsTests.CreateAndInvite` | T1, T2 |
| FR-TEAM-2 | US-4 | `TeamsTests.NonMember_Gets404` | T2, T3 |
| FR-TEAM-3 | US-1, UC-1 | `AssignTests.*` | T4 |
| FR-NOTIF-1 | US-1, UC-1 | `NotificationTests.Assignment_NotifiesAssignee` | T5, T6 |
| FR-NOTIF-2 | US-2 | `DueReminderTests.*` | T7 |
| FR-NOTIF-3 | US-3 | `NotificationTests.MarkAllRead` | T6 |
| NFR-SEC-TEAM-1 | US-4 | one 404 test per team endpoint | T3 |

## 11. Development tasks (first slice: Musts only)

| Task | Description | Size |
|---|---|---|
| T1 | `Team` and `TeamMember` models, and a migration | S |
| T2 | Create a team; invite a registered user by email; accept the invitation | M |
| T3 | Team tasks: the `TeamId` column; access checks in the repository; 404 for non-members | M |
| T4 | Assign and unassign; refuse non-members; show the assignee on the task | S |
| T5 | `Notification` model, a migration, and a service that records one | S |
| T6 | The unread count on every page, the notifications page, and "Mark all as read" | M |
| T7 | Due-soon reminders: a background job, once per task per due date | M |
| T8 | Tests for each acceptance criterion in US-1 to US-4 | M |

## Change log

- 1.0 (Course 2, Chapter 6): first analysis.
