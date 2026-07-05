```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record ArchitectureModelBoundItemAdded(string Category);

[FromEvent<ArchitectureModelBoundItemAdded>(key: nameof(ArchitectureModelBoundItemAdded.Category))]
public record ArchitectureModelBoundSummary(
    [Key] string Category,
    [Count<ArchitectureModelBoundItemAdded>] int Count);
```
