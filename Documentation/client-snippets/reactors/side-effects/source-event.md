```csharp
using Cratis.Chronicle.Events;

public readonly record struct MemberId(string Value)
{
    public static implicit operator EventSourceId(MemberId id) => new(id.Value);
}

public readonly record struct Isbn(string Value)
{
    public static implicit operator EventSourceId(Isbn id) => new(id.Value);
}

[EventType]
public record BookReserved(MemberId MemberId, Isbn Isbn);
```
