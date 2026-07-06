```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record MbSetValueOrderPlaced(string CustomerName);

[EventType]
public record MbSetValueOrderCanceled;

public record MbSetValueOrder(
    [Key]
    Guid Id,

    [SetFrom<MbSetValueOrderPlaced>(nameof(MbSetValueOrderPlaced.CustomerName))]
    string CustomerName,

    [SetValue<MbSetValueOrderPlaced>("active")]
    [SetValue<MbSetValueOrderCanceled>("canceled")]
    string Status);
```
