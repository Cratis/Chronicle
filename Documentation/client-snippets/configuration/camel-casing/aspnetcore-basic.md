```csharp
using Microsoft.AspNetCore.Builder;

public static class CamelCasingAspNetCoreBasicRegistration
{
    public static void Configure(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddCratisChronicle(
            configure: chronicleBuilder => chronicleBuilder.WithCamelCaseNamingPolicy());

        var app = builder.Build();
    }
}
```
