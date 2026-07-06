```csharp
public class PatchesIdempotentPatch(IStorage storage, ILogger<PatchesIdempotentPatch> logger) : ICanApplyPatch
{
    public SemanticVersion Version => new(1, 8, 0);

    public async Task Up()
    {
        var eventStore = storage.GetEventStore(EventStoreName.System);
        var reactors = await eventStore.Reactors.GetAll();

        // Filter to only process items that need migration
        var reactorsToMigrate = reactors
            .Where(r => r.Identifier.Value.Contains("OldPattern"))
            .ToList();

        if (reactorsToMigrate.Count == 0)
        {
            logger.LogInformation("Nothing to migrate");
            return;
        }

        // Proceed with migration
    }

    public Task Down() => Task.CompletedTask;
}
```
