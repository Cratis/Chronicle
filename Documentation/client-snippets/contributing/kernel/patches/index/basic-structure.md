```csharp
public class PatchesBasicPatch(IStorage storage, ILogger<PatchesBasicPatch> logger) : ICanApplyPatch
{
    public SemanticVersion Version => new(1, 5, 0);

    public async Task Up()
    {
        logger.LogInformation("Applying PatchesBasicPatch");

        var eventStore = storage.GetEventStore(EventStoreName.System);
        var reactors = await eventStore.Reactors.GetAll();
        logger.LogInformation("Found {Count} reactors", reactors.Count());
    }

    public Task Down()
    {
        logger.LogInformation("Rolling back PatchesBasicPatch");
        return Task.CompletedTask;
    }
}
```
