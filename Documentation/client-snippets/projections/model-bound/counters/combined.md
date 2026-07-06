```csharp
[EventType]
public record MbCountersItemCreated(string Name, int InitialQuantity);

[EventType]
public record MbCountersItemRestocked;

[EventType]
public record MbCountersItemSold;

public record MbCountersInventoryItem(
    [Key]
    Guid ItemId,

    [SetFrom<MbCountersItemCreated>(nameof(MbCountersItemCreated.Name))]
    string Name,

    [SetFrom<MbCountersItemCreated>(nameof(MbCountersItemCreated.InitialQuantity))]
    [Increment<MbCountersItemRestocked>]
    [Decrement<MbCountersItemSold>]
    int Quantity,

    [Count<MbCountersItemRestocked>]
    int RestockCount,

    [Count<MbCountersItemSold>]
    int SalesCount);
```
