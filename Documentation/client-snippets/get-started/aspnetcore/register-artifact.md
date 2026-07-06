```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

public static class AspNetCoreExplicitRegistration
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<GetStartedBookReturnedNotifier>();
    }
}
```
