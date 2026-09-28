```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;

[FromEvent<Chr0025FixedOpened>]
public record Chr0025FixedAccount(
    [Key] Guid Id,

    // Only the explicit setter writes Location; WorkMode still comes from Chr0025FixedWorkModeSet.
    [SetFrom<Chr0025FixedOpened>]
    [NoAutoMap]
    string Location,

    [SetFrom<Chr0025FixedWorkModeSet>]
    string WorkMode);

[EventType]
public record Chr0025FixedOpened(string Location);

[EventType]
public record Chr0025FixedWorkModeSet(string WorkMode, string Location);
```
