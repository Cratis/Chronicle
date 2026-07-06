```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

public static class GetStartedWorkerMongoRegistration
{
    public static void ConfigureServices(IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IMongoClient>(new MongoClient("mongodb://localhost:27017"));
        builder.Services.AddSingleton(provider => provider.GetRequiredService<IMongoClient>().GetDatabase("Quickstart"));
        builder.Services.AddTransient(provider => provider.GetRequiredService<IMongoDatabase>().GetCollection<GetStartedBook>("Books"));
    }
}
```
