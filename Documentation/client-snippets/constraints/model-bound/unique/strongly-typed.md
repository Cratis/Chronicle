```csharp
using Cratis.Concepts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

public record ConstraintsModelBoundUniqueEmailAddress(string Value) : ConceptAs<string>(Value);

[EventType]
public record ConstraintsModelBoundUniqueAuthorRegistered([property: Unique(name: "UniqueAuthorEmail")] ConstraintsModelBoundUniqueEmailAddress Email);
```
