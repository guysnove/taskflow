using Ch13.DI.Lifetimes;
using Ch13.DI.Mistakes;
using Microsoft.Extensions.DependencyInjection;

namespace Ch13.DI.Tests;

public class LifetimeTests
{
    private static ServiceProvider Build(Action<ServiceCollection> register, bool validate = false)
    {
        var services = new ServiceCollection();
        register(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = validate, ValidateOnBuild = validate });
    }

    [Fact]
    public void Transient_Scoped_Singleton_BehaveAsDocumented()
    {
        using var provider = Build(s => { s.AddTransient<TransientThing>(); s.AddScoped<ScopedThing>(); s.AddSingleton<SingletonThing>(); });
        using var a = provider.CreateScope();
        using var b = provider.CreateScope();
        Assert.NotSame(a.ServiceProvider.GetRequiredService<TransientThing>(), a.ServiceProvider.GetRequiredService<TransientThing>());
        Assert.Same(a.ServiceProvider.GetRequiredService<ScopedThing>(), a.ServiceProvider.GetRequiredService<ScopedThing>());
        Assert.NotSame(a.ServiceProvider.GetRequiredService<ScopedThing>(), b.ServiceProvider.GetRequiredService<ScopedThing>());
        Assert.Same(a.ServiceProvider.GetRequiredService<SingletonThing>(), b.ServiceProvider.GetRequiredService<SingletonThing>());
    }

    [Fact]
    public void ACaptiveDependency_IsSharedByEveryone_WithoutValidation()
    {
        using var provider = Build(s => { s.AddScoped<ShoppingBasket>(); s.AddSingleton<CheckoutService>(); });
        using (var ana = provider.CreateScope()) ana.ServiceProvider.GetRequiredService<CheckoutService>().Add("Book");
        using var ben = provider.CreateScope();
        Assert.Equal(["Book"], ben.ServiceProvider.GetRequiredService<CheckoutService>().Items);   // Ben sees Ana's basket
    }

    [Fact]
    public void Validation_RefusesTheCaptiveDependency_AtStartup()
    {
        var ex = Assert.Throws<AggregateException>(() => Build(s => { s.AddScoped<ShoppingBasket>(); s.AddSingleton<CheckoutService>(); }, validate: true));
        Assert.Contains("Cannot consume scoped service", ex.InnerExceptions[0].Message);
    }

    [Fact]
    public void ScopedFromTheRoot_IsRefused_WhenScopesAreValidated()
    {
        using var provider = Build(s => s.AddScoped<ScopedThing>(), validate: true);
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ScopedThing>());
    }
}
