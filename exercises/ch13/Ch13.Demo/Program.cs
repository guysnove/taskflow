// Chapter 13 demo: dependency injection, lifetimes, and three real mistakes.
//   dotnet run -- coupling | lifetimes | captive | singleton-state | root-transient | validate
using Ch13.DI.Coupling.Loose;
using Ch13.DI.Lifetimes;
using Ch13.DI.Mistakes;
using Microsoft.Extensions.DependencyInjection;

switch (args.FirstOrDefault())
{
    case "coupling":
        {
            // The composition root decides: a console "mailer" and the real clock.
            var services = new ServiceCollection();
            services.AddSingleton<IMailer, ConsoleMailer>();
            services.AddSingleton(TimeProvider.System);
            services.AddTransient<ReminderService>();
            using var provider = services.BuildServiceProvider();
            bool sent = provider.GetRequiredService<ReminderService>().SendDailyReminder("ana@example.com");
            Console.WriteLine($"Reminder sent today ({DateTime.Now:dddd}): {sent}");
            break;
        }
    case "lifetimes":
        {
            var services = new ServiceCollection();
            services.AddTransient<TransientThing>();
            services.AddScoped<ScopedThing>();
            services.AddSingleton<SingletonThing>();
            services.AddTransient<Consumer>();
            using var provider = services.BuildServiceProvider();
            for (int request = 1; request <= 2; request++)
            {
                using var scope = provider.CreateScope();              // ASP.NET Core: one scope per request
                Console.WriteLine($"Request {request}:");
                for (int ask = 1; ask <= 2; ask++)
                {
                    Console.WriteLine($"  ask {ask}: {scope.ServiceProvider.GetRequiredService<Consumer>().Describe()}");
                }
            }
            break;
        }
    case "captive":
        {
            var services = new ServiceCollection();
            services.AddScoped<ShoppingBasket>();
            services.AddSingleton<CheckoutService>();                  // the mistake
            using var provider = services.BuildServiceProvider();      // no validation (like Production)
            foreach (var (user, item) in new[] { ("Ana", "Book"), ("Ben", "Lamp") })
            {
                using var scope = provider.CreateScope();
                var checkout = scope.ServiceProvider.GetRequiredService<CheckoutService>();
                checkout.Add(item);
                Console.WriteLine($"{user}'s basket: {string.Join(", ", checkout.Items)}");
            }
            break;
        }
    case "validate":
        {
            var services = new ServiceCollection();
            services.AddScoped<ShoppingBasket>();
            services.AddSingleton<CheckoutService>();                  // the same mistake
            try
            {
                using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
                Console.WriteLine("Built without complaint.");
            }
            catch (AggregateException ex)
            {
                Console.WriteLine(ex.InnerExceptions[0].Message);
            }
            break;
        }
    case "singleton-state":
        {
            var services = new ServiceCollection();
            services.AddSingleton<CurrentUserHolder>();                 // the mistake
            services.AddScoped<GreetingService>();
            using var provider = services.BuildServiceProvider();
            async Task<string> Request(string user, int ms)
            {
                using var scope = provider.CreateScope();
                return await scope.ServiceProvider.GetRequiredService<GreetingService>().GreetAsync(user, TimeSpan.FromMilliseconds(ms));
            }
            var answers = await Task.WhenAll(Request("Ana", 200), Request("Ben", 50));   // two users, at the same time
            Console.WriteLine($"Ana's page says: {answers[0]}");
            Console.WriteLine($"Ben's page says: {answers[1]}");
            break;
        }
    case "root-transient":
        {
            var services = new ServiceCollection();
            services.AddTransient<ReportFile>();
            using (var provider = services.BuildServiceProvider())
            {
                for (int i = 0; i < 1000; i++) provider.GetRequiredService<ReportFile>();      // from the ROOT
                Console.WriteLine($"Resolved from the root 1,000 times: {ReportFile.Open} still open");
                for (int i = 0; i < 1000; i++)
                {
                    using var scope = provider.CreateScope();
                    scope.ServiceProvider.GetRequiredService<ReportFile>();                    // from a scope
                }
                Console.WriteLine($"Then 1,000 times from short scopes: {ReportFile.Open} still open (the same 1,000)");
            }
            Console.WriteLine($"After the root provider is disposed: {ReportFile.Open} open");
            break;
        }
    default:
        Console.WriteLine("Usage: coupling | lifetimes | captive | validate | singleton-state | root-transient");
        break;
}
