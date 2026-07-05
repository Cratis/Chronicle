```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record MbCountersUserLoggedIn;

public record MbCountersUserStatistics(
    [Key]
    Guid UserId,

    [Increment<MbCountersUserLoggedIn>]
    int LoginCount);
```
