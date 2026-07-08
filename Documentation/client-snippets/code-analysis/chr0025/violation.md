```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;

[FromEvent<Chr0025Opened>]
public record Chr0025Account(
    [Key] Guid Id,

    // Info CHR0025: Location is set explicitly from Chr0025Opened, but Chr0025WorkModeSet — also
    // referenced by this projection (for WorkMode) — carries a Location that AutoMap writes on top of it.
    [SetFrom<Chr0025Opened>]
    string Location,

    [SetFrom<Chr0025WorkModeSet>]
    string WorkMode);

[EventType]
public record Chr0025Opened(string Location);

[EventType]
public record Chr0025WorkModeSet(string WorkMode, string Location);
```
