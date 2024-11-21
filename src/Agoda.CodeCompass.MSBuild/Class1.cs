public void ConfigureServices(IServiceCollection services)
{
    services.AddDataServices().AddBusinessServices().AddInfrastructureServices();
    // ... more method calls
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataServices(this IServiceCollection services)
    {
        services.AddSingleton<IDatabase, Database>();
        services.AddTransient<IUserRepository, UserRepository>(); // ... more registrations return services;
    }
    // ... more extension methods
}