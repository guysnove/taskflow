# TaskFlow — module and layer design

Version 2.1 (Course 2, Chapter 13). Checked by the architecture tests in `TaskFlow.Tests/Architecture/`:
`LayerRulesTests` (project references and packages) and `ModuleRulesTests` (modules inside the projects).
`dotnet run --project tools/Modules` prints what the code actually does.

## 1. Layers (projects)

| Project | Layer | Responsibility | May reference |
|---|---|---|---|
| `TaskFlow.Domain` | Domain | Entities and pure domain logic (`TaskItem.IsOverdue`, `DependencyGraph`, `TitleMatcher`). | nothing — no packages either |
| `TaskFlow.Application` | Application | Use cases (`CompleteTask`), rules (`NewTaskChecker`), notifications, accounts — and the **ports** they need (`ITaskRepository`, `INotificationQueries`, `IAccountStore`, `IEmailSender`, …). | Domain; small abstractions packages (logging, options, password hashing) — **no EF Core, no ASP.NET Core** |
| `TaskFlow.Infrastructure` | Infrastructure | **Adapters** for the ports: EF Core + SQLite, migrations, SMTP, the background job. | Application, Domain, ASP.NET Core |
| `TaskFlow` | Presentation + composition root | Controllers, API, views; `Program.cs` wires every layer together. | all three |

```
          TaskFlow (web)  ──────────────┐
             │                          ▼
             │                TaskFlow.Infrastructure
             ▼                          │ implements the ports
     TaskFlow.Application  ◄────────────┘
             │
             ▼
       TaskFlow.Domain
```

Every arrow points inwards, towards the Domain. Infrastructure depends on Application (to implement its interfaces), **not** the other way round: that's the dependency inversion principle (Chapter 8) at project scale.

## 2. Modules (folders) inside the projects

| Module | Project | May use |
|---|---|---|
| Controllers, ViewComponents | Web | Models, Services, UseCases, Notifications, Settings, Common |
| Api | Web | Models, Services, UseCases, Notifications, Common |
| TagHelpers | Web | Models, Common |
| Middleware | Web | Common |
| Security | Web | Services, Common |
| UseCases | Application | Models, Services, Notifications, Common |
| Services | Application | Models, Settings, Common |
| Notifications | Application (+ SMTP and the job in Infrastructure) | Models, Services, Settings, Common |
| Data | Infrastructure | Models, Services, Notifications, Common |
| Models | Domain (+ input records in Application) | Common |
| Settings, Common | Application, Domain | nothing |

Rules: no cycles; the web never uses Data (only `Program.cs`, the composition root, may); Application never uses Data.

## 3. Service lifetimes (Course 2, Chapter 13)

| Lifetime | Use for | TaskFlow | Never |
|---|---|---|---|
| Singleton | stateless, thread-safe objects; one per app | `TimeProvider`, `IEmailSender` (+ decorator), options | hold a scoped object; keep per-user state |
| Scoped | one per request: the unit of work and everything that uses it | `TaskFlowDbContext`, repositories, `ICurrentUser`, `Notifier`, use cases, rules | be resolved from the root provider |
| Transient | cheap, stateless helpers | (none today) | be disposable and resolved from the root |

The whole graph is validated at startup in every environment (`ValidateScopes`, `ValidateOnBuild`), and `ServiceGraphTests` checks it on every build. A class registered under two service types is registered once and **forwarded** (`ProjectExistsRule`), so a request gets one object.

## 4. Decisions and trade-offs

- **Four projects (ADR 0001).** The compiler now refuses an outward reference. Cost: four project files, slower to reshape. The boundaries had stopped moving (Chapter 11), so the cost is worth it.
- **Namespaces kept** (`TaskFlow.Models`, `TaskFlow.Services`, …) instead of `TaskFlow.Domain.Models`, … — the move changed project files, not hundreds of `using` lines and every view. Revisit if two projects ever need the same folder name for different things.
- **Ports only where Application needed the database**: `INotificationQueries`, `IAccountStore`. The existing repository interfaces were already ports.
- **No interface for each use case.** `CompleteTask` is a class; controllers depend on it directly. Extract an interface if a second implementation appears.
- **The domain model is simple.** Entities with a little behaviour (`IsOverdue`, `IsDueSoon`, `StepProgress`) and public setters for EF Core. A richer domain model (private setters, methods for every change) is not worth it at TaskFlow's size; revisit if rules start appearing in several services.
- **Infrastructure references ASP.NET Core** (`FrameworkReference`) for the background service and configuration. Acceptable: it's the outer layer.

## 5. Change log

- 2.1 (Course 2, Chapter 13): service lifetimes section; validation in every environment (ADR 0002); `AddTaskFlowWeb`.
- 2.0 (Course 2, Chapter 12): four projects. `INotificationQueries` and `IAccountStore` ports with EF Core adapters; `CompleteTask` use case; each layer registers its own services. Migrations moved to `TaskFlow.Infrastructure`.
- 1.0 (Course 2, Chapter 11): first version. Found and fixed: a cycle Data ↔ Services (`TimeProviderExtensions` moved to Common), and Controllers → Data in `AdminController` and `NotificationsController` (now `AccountService`).
