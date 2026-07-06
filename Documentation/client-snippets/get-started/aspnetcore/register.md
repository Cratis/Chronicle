```csharp
using Microsoft.AspNetCore.Builder;

public static class AspNetCoreRegistration
{
    public static void ConfigureApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args)
            .AddCratisChronicle(options => options.EventStore = "Quickstart");

        var app = builder.Build();
        app.UseCratisChronicle();
    }
}
```
