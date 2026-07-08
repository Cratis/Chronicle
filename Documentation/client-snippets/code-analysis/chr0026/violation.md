```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

public record Chr0026AccountId(Guid Value) : EventSourceId<Guid>(Value);

public record Chr0026Account(
    // Warning CHR0026: Chr0026AccountId derives from EventSourceId<T>, so it already is the
    // key/stream identity and compliance subject — the [Key] attribute is redundant.
    [Key] Chr0026AccountId Id,
    string Name);
```
