```csharp
using Microsoft.Extensions.Hosting;

public static class ChronicleOptionsEnableValidationRegistration
{
    public static void Configure(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddCratisChronicle(configureOptions: options =>
        {
            options.EnableEventTypeGenerationValidation = true;
        });
    }
}
```
