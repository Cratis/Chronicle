```csharp
using Cratis.Chronicle.Events;

public record BookId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static BookId New() => new(Guid.NewGuid());
}
```
