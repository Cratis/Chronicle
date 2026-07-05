```csharp
using Microsoft.Extensions.DependencyInjection;

public static class ComplianceClientRegistration
{
    public static void Configure(IServiceCollection services) => services.AddCompliance();
}
```
