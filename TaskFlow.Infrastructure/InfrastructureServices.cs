using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskFlow.Data;
using TaskFlow.Models;
using TaskFlow.Notifications;
using TaskFlow.Services;

namespace TaskFlow;

// Course 2, Chapter 12: Infrastructure plugs its ADAPTERS into the Application's PORTS.
// To move from SQLite to PostgreSQL, or from SMTP to an email API, this is the file that changes.
public static class InfrastructureServices
{
    public static IServiceCollection AddTaskFlowInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // The database: SQLite, in the file named by ConnectionStrings:TaskFlow.
        services.AddDbContext<TaskFlowDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("TaskFlow")));

        // Ports implemented with EF Core.
        services.AddScoped<ITaskRepository, EfTaskRepository>();
        services.AddScoped<IProjectRepository, EfProjectRepository>();
        services.AddScoped<IStepRepository, EfStepRepository>();
        services.AddScoped<IDependencyRepository, EfDependencyRepository>();
        services.AddScoped<INotificationRepository, EfNotificationRepository>();
        services.AddScoped<INotificationQueries, EfNotificationQueries>();
        services.AddScoped<IAccountStore, EfAccountStore>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        // Email: the SMTP adapter, wrapped by the retrying decorator (Chapter 10).
        services.AddSingleton<SmtpEmailSender>();
        services.AddSingleton<IEmailSender>(sp => new RetryingEmailSender(
            sp.GetRequiredService<SmtpEmailSender>(), sp.GetRequiredService<IOptions<EmailOptions>>(),
            sp.GetRequiredService<ILogger<RetryingEmailSender>>(), sp.GetRequiredService<TimeProvider>()));

        // The background job that runs the reminders use case.
        services.AddHostedService<DueReminderService>();
        return services;
    }
}
