```csharp
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

// Error CHR0034: 'CustomerId' is typed as 'CustomerId' (derives from EventSourceId<T>);
// [PII] cannot be applied to an event source id — Chronicle throws
// PIINotSupportedOnEventSourceId at runtime. Remove [PII].
[EventType]
public record CustomerRegistered([PII] Chr0034CustomerId CustomerId, string Name);

public record Chr0034CustomerId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static implicit operator Chr0034CustomerId(Guid value) => new(value);
}
```
