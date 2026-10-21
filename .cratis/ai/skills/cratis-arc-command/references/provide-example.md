<!-- cratis-ai-managed: skills/cratis-arc-command/references/provide-example.md -->
# Provide example

This reference holds a worked `Provide()` example that rejects with a validation result.

```csharp
[Command]
public record Open<Thing>(<ThingId> Id, <OwnerId> OwnerId)
{
    public async Task<Result<<Owner>, ValidationResult>> Provide(IReadModels readModels)
    {
        var owner = await readModels.GetInstanceById<<Owner>>((EventSourceId)OwnerId);
        return owner is null
            ? ValidationResult.Error("Owner must exist.")
            : owner;
    }

    public <ThingOpened> Handle(<Owner> owner) => new(owner.Id);
}
```
