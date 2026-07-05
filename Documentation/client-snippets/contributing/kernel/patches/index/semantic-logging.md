```csharp
internal static partial class PatchesLoggingPatchLogMessages
{
    [LoggerMessage(LogLevel.Information, "Starting PatchesLoggingPatch migration")]
    internal static partial void StartingMigration(this ILogger<PatchesLoggingPatch> logger);

    [LoggerMessage(LogLevel.Information, "Migrated {Count} items")]
    internal static partial void MigratedItems(this ILogger<PatchesLoggingPatch> logger, int count);
}

public class PatchesLoggingPatch(IStorage storage, ILogger<PatchesLoggingPatch> logger) : ICanApplyPatch
{
    public SemanticVersion Version => new(1, 6, 0);

    public async Task Up()
    {
        logger.StartingMigration();

        var count = (await storage.GetEventStores()).Count();
        logger.MigratedItems(count);
    }

    public Task Down() => Task.CompletedTask;
}
```
