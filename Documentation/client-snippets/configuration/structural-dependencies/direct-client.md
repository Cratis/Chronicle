```csharp
using Microsoft.Extensions.Logging;

public static class StructuralDependenciesDirectClient
{
    public static ChronicleClient Create(ChronicleOptions options, IClientArtifactsProvider myProvider) =>
        new(
            options,
            artifactsProvider: myProvider,
            loggerFactory: LoggerFactory.Create(b => b.AddConsole()));
}
```
