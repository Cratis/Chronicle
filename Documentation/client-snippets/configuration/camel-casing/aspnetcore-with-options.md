```csharp
using Microsoft.AspNetCore.Builder;

public static class CamelCasingAspNetCoreWithOptionsRegistration
{
    public static void Configure(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddCratisChronicle(
            configureOptions: options => options.EventStore = "MyEventStore",
            configure: chronicleBuilder => chronicleBuilder.WithCamelCaseNamingPolicy());

        var app = builder.Build();
    }
}
```
