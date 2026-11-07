```csharp
[Edge("VISITED")]
[EdgeFrom<PlaceVisited>(
    sourceKey: nameof(PlaceVisited.PersonId),
    targetKey: nameof(PlaceVisited.PlaceId),
    identity: nameof(PlaceVisited.VisitId))]
public record Visited(
    [Source<Person>] Guid PersonId,
    [Target<Place>] Guid PlaceId,
    [Key] Guid VisitId,
    [Count<PlaceVisited>] int Times);
```
