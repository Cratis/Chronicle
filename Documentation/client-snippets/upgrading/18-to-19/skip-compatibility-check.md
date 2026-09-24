```csharp
using Microsoft.Extensions.DependencyInjection;

public static class Upgrading18To19SkipCompatibilityCheck
{
    public static void Before(IServiceCollection services)
    {
        // 18.x
        services.AddCratisChronicleConnection(
            skipCompatibilityCheck: false);
    }

    public static void After(IServiceCollection services)
    {
        // 19.x - the parameter is now nullable, so "unspecified" is distinguishable from "false"
        services.AddCratisChronicleConnection(
            skipCompatibilityCheck: null);
    }
}
```
