```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsModelBoundUniqueProjectCreated([property: Unique] string Name, string Description);
```
