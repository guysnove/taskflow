using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Common;
using TaskFlow.Notifications;
using TaskFlow.Services;
using TaskFlow.UseCases;

namespace TaskFlow;

// Course 2, Chapter 12: the Application layer registers its own classes. It names no database,
// no SMTP and no web types: those are Infrastructure's and the Web's business.
public static class ApplicationServices
{
    public static IServiceCollection AddTaskFlowApplication(this IServiceCollection services)
    {
        // Course 2, Chapter 8: the rules for a new task, checked in this order.
        services.AddScoped<INewTaskRule, DueNotInPastRule>();
        services.AddScoped<INewTaskRule, DueWithinFiveYearsRule>();   // FR-TASK-3
        services.AddScoped<INewTaskRule>(sp => sp.GetRequiredService<ProjectExistsRule>());   // Chapter 13: forward, don't duplicate
        services.AddScoped<INewTaskRule, DuplicateTitleRule>();
        services.AddScoped<NewTaskChecker>();
        services.AddScoped<ProjectExistsRule>();                      // Chapter 9: the edit form uses it alone; the one object per request

        // Course 2, Chapter 10: notifications (Strategy, Observer).
        services.AddScoped<INotificationChannel, InAppChannel>();
        services.AddScoped<INotificationChannel, EmailChannel>();
        services.AddScoped<Notifier>();
        services.AddScoped<ITaskCompletedObserver, UnblockedTasksObserver>();
        services.AddScoped<TaskEvents>();
        services.AddScoped<DueReminders>();

        // Use cases and account rules.
        services.AddScoped<CompleteTask>();
        services.AddSingleton<TaskFlowMetrics>();               // Course 2, Chapter 29: counters and gauges, one set per app
        services.AddScoped<AccountService>();
        return services;
    }
}
