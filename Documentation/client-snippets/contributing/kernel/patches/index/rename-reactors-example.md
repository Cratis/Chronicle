```csharp
internal static partial class RenameReactorsLogMessages
{
    [LoggerMessage(LogLevel.Information, "Starting patch to rename reactors")]
    internal static partial void StartingPatch(this ILogger<RenameReactors> logger);

    [LoggerMessage(LogLevel.Information, "Found {Count} reactors to rename")]
    internal static partial void FoundReactorsToRename(this ILogger<RenameReactors> logger, int count);

    [LoggerMessage(LogLevel.Information, "Renaming reactor from {CurrentId} to {NewId}")]
    internal static partial void RenamingReactor(this ILogger<RenameReactors> logger, string currentId, string newId);

    [LoggerMessage(LogLevel.Information, "Patch completed")]
    internal static partial void PatchCompleted(this ILogger<RenameReactors> logger);

    [LoggerMessage(LogLevel.Information, "Starting rollback")]
    internal static partial void StartingRollback(this ILogger<RenameReactors> logger);

    [LoggerMessage(LogLevel.Information, "Rollback completed")]
    internal static partial void RollbackCompleted(this ILogger<RenameReactors> logger);
}

public class RenameReactors(IStorage storage, ILogger<RenameReactors> logger) : ICanApplyPatch
{
    public SemanticVersion Version => new(15, 3, 0);

    public async Task Up()
    {
        logger.StartingPatch();

        var systemEventStore = storage.GetEventStore(EventStoreName.System);
        var reactors = await systemEventStore.Reactors.GetAll();

        var reactorsToRename = reactors
            .Where(r => r.Identifier.Value.Contains("Grains", StringComparison.OrdinalIgnoreCase))
            .ToList();

        logger.FoundReactorsToRename(reactorsToRename.Count);

        foreach (var reactor in reactorsToRename)
        {
            var currentId = reactor.Identifier;
            var newIdValue = currentId.Value.Replace("Grains", string.Empty, StringComparison.OrdinalIgnoreCase);

            logger.RenamingReactor(currentId.Value, newIdValue);
            await systemEventStore.Reactors.Rename(currentId, newIdValue);
        }

        logger.PatchCompleted();
    }

    public Task Down()
    {
        logger.StartingRollback();
        logger.RollbackCompleted();
        return Task.CompletedTask;
    }
}
```
