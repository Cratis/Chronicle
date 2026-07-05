```csharp
using Microsoft.Extensions.Hosting;

public static class CamelCasingWorkerHostRegistration
{
    public static void Configure(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddCratisChronicle(
            configure: chronicleBuilder => chronicleBuilder.WithCamelCaseNamingPolicy());

        var host = builder.Build();
    }
}
```
