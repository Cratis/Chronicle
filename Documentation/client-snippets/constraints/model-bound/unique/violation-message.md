```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsModelBoundUniqueMessageProjectCreated([property: Unique(message: "A project with this name already exists.")] string Name, string Description);
```
