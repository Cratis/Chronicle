```csharp
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

public static class AspNetCoreConventionRegistration
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services
            .AddBindingsByConvention()
            .AddSelfBindings();
    }
}
```
