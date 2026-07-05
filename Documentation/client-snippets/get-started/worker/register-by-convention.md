```csharp
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class GetStartedWorkerConventionRegistration
{
    public static void ConfigureServices(IHostApplicationBuilder builder)
    {
        builder.Services
            .AddBindingsByConvention()
            .AddSelfBindings();
    }
}
```
