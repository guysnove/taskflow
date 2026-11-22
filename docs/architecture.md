# TaskFlow architecture

Course 2, Chapter 23. How TaskFlow is put together, in four diagrams, from the outside in.
The diagrams are **text** (Mermaid): GitHub draws them, they're reviewed in pull requests like code,
and `git log` shows how they changed. Details: [design/modules.md](design/modules.md); decisions: [adr/](adr/).

## 1. Context: who uses TaskFlow, and what it talks to

```mermaid
flowchart LR
    user["Person<br/>(a team member)"] -- "pages, in a browser" --> tf["TaskFlow"]
    client["Script or app<br/>(uses the JSON API)"] -- "HTTPS + JSON" --> tf
    tf -- "notification emails (SMTP)" --> smtp["Mail server"]
    smtp --> inbox["The person's inbox"]
```

TaskFlow has two front doors, the web pages and the JSON API, and one outside dependency: a mail server.

## 2. Containers: what runs where

```mermaid
flowchart LR
    browser["Browser"] -- "HTTPS :443" --> nginx["nginx<br/>TLS, forwarded headers"]
    nginx -- "HTTP :5000" --> app["TaskFlow (ASP.NET Core 10)<br/>MVC pages · JSON API · background services"]
    app -- "EF Core" --> db[("SQLite file<br/>taskflow.db")]
    app -- "SMTP" --> mail["Mail server"]
    systemd["systemd<br/>taskflow.service"] -. "starts, restarts,<br/>loads secrets" .-> app
```

One process does everything, including the background services (the email worker and the due-date reminders).
That's deliberate for a small product: one thing to deploy, one log, one database file to back up.
Chapter 27 revisits it when we look at scaling.

## 3. Inside the process: four projects, one direction

```mermaid
flowchart TB
    web["TaskFlow (web)<br/>controllers, views, API, Program.cs"] --> app["TaskFlow.Application<br/>use cases, rules, notifications, ports"]
    infra["TaskFlow.Infrastructure<br/>EF Core, migrations, SMTP, workers"] --> app
    app --> domain["TaskFlow.Domain<br/>entities, pure logic"]
    web --> infra
```

Dependencies point inwards: the Domain knows nothing else; the Application defines interfaces (ports) such as
`ITaskRepository` and `IEmailSender`; Infrastructure implements them. Only `Program.cs`, the composition root,
knows all four ([ADR 0001](adr/0001-separate-projects-for-layers.md)). Architecture tests fail the build if a
reference points the wrong way.

## 4. A flow: marking a task done

```mermaid
sequenceDiagram
    actor Ana
    participant C as TasksController (or TasksApiController)
    participant U as CompleteTask (use case)
    participant R as ITaskRepository (EF Core)
    participant E as TaskEvents
    participant O as UnblockedTasksObserver
    participant N as Notifier
    participant Q as Outgoing email queue
    participant W as EmailWorker (background)
    Ana->>C: POST /tasks/7/done
    C->>U: RunAsync(7)
    U->>R: MarkDoneAsync(7) (only if it's Ana's)
    R-->>U: true
    U->>E: TaskCompletedAsync(7)
    E->>O: OnTaskCompletedAsync(7)
    O->>N: "Ready to start" for each task that waited only for 7
    N->>Q: in-app notification saved, email queued
    C-->>Ana: redirect to the task list
    W->>Q: (later) take the next email
    W->>W: send, with retries and a timeout
```

The request returns before any email is sent: slow mail servers never slow down the page (Chapter 16).
An observer that fails is logged and skipped, so completing a task never fails because of a notification.

## What's not here (yet)

- Teams (release 1.2): see [planning/release-1.2-teams.md](planning/release-1.2-teams.md) and [proposals/0001-team-ownership.md](proposals/0001-team-ownership.md).
- More than one server: SQLite and the in-memory email queue assume one process (tech debt TD-13, [tech-debt.md](tech-debt.md)).
