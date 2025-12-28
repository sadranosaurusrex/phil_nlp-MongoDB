using DataConversion.Services;

namespace DataConversion.Extensions;

public static class ServiceRegistrar
{
    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        // Register MongoDB service as lazy to avoid connection on startup
        services.AddSingleton<IMongoDbService>(provider => 
            new MongoDbService(provider.GetRequiredService<IConfiguration>()));
        
        // Register Data service
        services.AddScoped<IDataService, DataService>();
        
        return services;
    }
}