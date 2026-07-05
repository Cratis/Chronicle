```csharp
using Microsoft.Extensions.Hosting;

public static class MigrationsValidationEnableValidationRegistration
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
