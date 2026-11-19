# TaskFlow — module design

Course 2, Chapter 11. Version 1.0. The architecture tests (`TaskFlow.Tests/Architecture/ModuleRulesTests.cs`) check the code against the **allowed dependencies** table below; the tool `dotnet run --project tools/Modules` prints what the code actually does.

## 1. Modules (today: folders and namespaces in one project)

| Module | Responsibility (one sentence) | Public face (what others may use) | Hides |
|---|---|---|---|
| **Web**: Controllers, Api, ViewComponents, TagHelpers, Middleware | Turns HTTP into calls, and results into pages or JSON. | routes and pages | model binding, view data, status codes |
| **Services** (tasks, projects, steps, dependencies, rules, accounts) | The application's rules and use cases. | `ITaskRepository`, `IProjectRepository`, `NewTaskChecker`, `AccountService`, … | EF Core queries; "only my data"; password hashing |
| **Notifications** | Telling people about things, by any channel. | `Notifier`, `TaskEvents`, `INotificationRepository` | channels, email delivery, retries, keys |
| **Data** | Storing everything in SQLite. | `TaskFlowDbContext` (to Services and Notifications only) | the schema, migrations, sample data |
| **Models** | The shapes of TaskFlow's data. | entities and input records | — |
| **Settings** | Typed, validated configuration. | `TaskFlowOptions` | configuration sources |
| **Common** | Tiny helpers that depend on nothing in TaskFlow. | `TimeProvider.Today()` | — |

## 2. Allowed dependencies (arrows point "uses")

```
Web ──► Services ──► Data ──► Models ──► Common
  │        ▲           ▲         ▲
  └──► Notifications ──┘─────────┘        (Settings and Common: anyone)
```

| Module | May use |
|---|---|
| Controllers, ViewComponents | Models, Services, Notifications, Settings, Common |
| Api | Models, Services, Notifications, Common |
| TagHelpers | Models, Common |
| Middleware | Common |
| Services | Models, Data, Settings, Common |
| Notifications | Models, Data, Services, Settings, Common |
| Data | Models, Common |
| Models | Common |
| Settings, Common | nothing |

Rules: **no cycles**; **Web never uses Data** (every database question goes through a service, which applies "only my data"); **Data knows nothing above it**.

## 3. Decisions and trade-offs

- **One project, folders as modules** (for now). Cheaper to change while the boundaries are still moving; enforced by tests instead of the compiler. Chapter 12 revisits this (separate projects), with the costs.
- **Notifications may use Data directly** (the Notifier loads users; the observer runs one query). A repository for each of those queries would add files without hiding a rule. Revisit if a second storage appears.
- **Accounts live in Services** (`AccountService`), so the admin page and the notification settings ask it instead of the database (moved in Chapter 11).

## 4. Change log

- 1.0 (Course 2, Chapter 11): first version. Found and fixed: a cycle Data ↔ Services (`TimeProviderExtensions` moved to Common), and Controllers → Data in `AdminController` and `NotificationsController` (now `AccountService`).
