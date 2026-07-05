```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

public static class AspNetCoreMongoRegistration
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IMongoClient>(new MongoClient("mongodb://localhost:27017"));
        builder.Services.AddSingleton(provider => provider.GetRequiredService<IMongoClient>().GetDatabase("Quickstart"));
        builder.Services.AddTransient(provider => provider.GetRequiredService<IMongoDatabase>().GetCollection<GetStartedBook>("Books"));
    }
}
```
