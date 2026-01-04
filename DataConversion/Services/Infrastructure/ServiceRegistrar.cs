using DataConversion.Services;

namespace DataConversion.Extensions;

public static class ServiceRegistrar
{
    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<IMongoDbService>(provider => 
            new MongoDbService(provider.GetRequiredService<IConfiguration>()));
        
        services.AddScoped<IDataService, DataService>();
        
        return services;
    }
}