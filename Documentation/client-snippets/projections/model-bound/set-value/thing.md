```csharp
[EventType]
public record MbSetValueThingHappened;

public record MbSetValueThing(
    [Key]
    Guid Id,

    [SetValue<MbSetValueThingHappened>("pending")]
    string StatusLabel,

    [SetValue<MbSetValueThingHappened>(42)]
    int Priority,

    [SetValue<MbSetValueThingHappened>(true)]
    bool IsActive,

    [SetValue<MbSetValueThingHappened>(3.14)]
    double Score);
```
