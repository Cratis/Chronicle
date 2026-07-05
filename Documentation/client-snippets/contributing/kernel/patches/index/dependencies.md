```csharp
public class PatchesComplexPatch(
    IStorage storage,
    IEventTypes eventTypes,
    ILogger<PatchesComplexPatch> logger) : ICanApplyPatch
{
    public SemanticVersion Version => new(2, 0, 0);

    public async Task Up()
    {
        if (await storage.HasEventStore(EventStoreName.System))
        {
            await eventTypes.DiscoverAndRegister(EventStoreName.System);
            logger.LogInformation("Discovered and registered event types");
        }
    }

    public Task Down() => Task.CompletedTask;
}
```
