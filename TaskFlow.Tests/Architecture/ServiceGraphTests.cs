using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Data;
using TaskFlow.Notifications;
using TaskFlow.Services;
using TaskFlow.Tests.Integration;

namespace TaskFlow.Architecture;

// Course 2, Chapter 13: the dependency-injection container is part of the architecture.
// These tests check TaskFlow's real registrations, exactly as Program.cs makes them.
public class ServiceGraphTests
{
    // Starts the app once, keeps a copy of its service registrations.
    private static readonly Lazy<List<ServiceDescriptor>> Registered = new(() =>
    {
        IServiceCollection? captured = null;
        using var factory = new TaskFlowFactory().WithWebHostBuilder(b => b.ConfigureServices(s => captured = s));
        _ = factory.Services;                                         // builds the host
        return captured!.ToList();
    });

    private static IServiceCollection Copy()
    {
        IServiceCollection services = new ServiceCollection();
        foreach (var d in Registered.Value)
        {
            services.Add(d);
        }
        return services;
    }

    [Fact]
    public void TheWholeGraph_Builds_WithValidation()
    {
        // ValidateOnBuild: every registered service can be constructed.
        // ValidateScopes: no singleton depends on a scoped service (a captive dependency).
        using var provider = Copy().BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Theory]
    [InlineData(typeof(TaskFlowDbContext), ServiceLifetime.Scoped)]        // one per request: a unit of work
    [InlineData(typeof(ITaskRepository), ServiceLifetime.Scoped)]          // uses the DbContext
    [InlineData(typeof(ICurrentUser), ServiceLifetime.Scoped)]             // differs per request
    [InlineData(typeof(Notifier), ServiceLifetime.Scoped)]                 // uses repositories
    [InlineData(typeof(IEmailSender), ServiceLifetime.Singleton)]          // stateless, thread-safe
    [InlineData(typeof(TimeProvider), ServiceLifetime.Singleton)]          // one clock for the app
    public void KeyServices_HaveTheLifetimeTheDesignSays(Type service, ServiceLifetime expected)
    {
        var registration = Registered.Value.Last(d => d.ServiceType == service);   // the last one wins
        Assert.Equal(expected, registration.Lifetime);
    }

    // ProjectExistsRule is registered twice: as one of the INewTaskRules, and as itself (for the edit form).
    // In one request, both should be the SAME object — or one request has two rule objects.
    [Fact]
    public void ProjectExistsRule_IsOneObjectPerRequest()
    {
        using var factory = new TaskFlowFactory();
        using var scope = factory.Services.CreateScope();
        var asOneOfTheRules = scope.ServiceProvider.GetServices<INewTaskRule>().OfType<ProjectExistsRule>().Single();
        var asItself = scope.ServiceProvider.GetRequiredService<ProjectExistsRule>();
        Assert.Same(asOneOfTheRules, asItself);
    }
}
