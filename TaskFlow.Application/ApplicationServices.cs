using Microsoft.Extensions.DependencyInjection;
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
        services.AddScoped<INewTaskRule, ProjectExistsRule>();
        services.AddScoped<INewTaskRule, DuplicateTitleRule>();
        services.AddScoped<NewTaskChecker>();
        services.AddScoped<ProjectExistsRule>();                      // Chapter 9: the edit form uses it alone

        // Course 2, Chapter 10: notifications (Strategy, Observer).
        services.AddScoped<INotificationChannel, InAppChannel>();
        services.AddScoped<INotificationChannel, EmailChannel>();
        services.AddScoped<Notifier>();
        services.AddScoped<ITaskCompletedObserver, UnblockedTasksObserver>();
        services.AddScoped<TaskEvents>();
        services.AddScoped<DueReminders>();

        // Use cases and account rules.
        services.AddScoped<CompleteTask>();
        services.AddScoped<AccountService>();
        return services;
    }
}
