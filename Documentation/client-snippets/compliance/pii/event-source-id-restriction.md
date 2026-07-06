```csharp
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

// ❌ This will throw PIINotSupportedOnEventSourceId
[PII]
public record PiiAttrEmployeeId(Guid Value) : EventSourceId<Guid>(Value);
```
