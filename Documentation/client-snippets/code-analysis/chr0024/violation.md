```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[FromEvent<Chr0024AccountRegistered>]
public record Chr0024Account(
    [Key] Guid Id,

    // Warning CHR0024: Name has no mapping attribute, and Chr0024AccountRegistered — the only
    // subscribed event — carries no same-named property, so AutoMap can never populate it.
    string Name);

[EventType]
public record Chr0024AccountRegistered(Guid Reference);
```
