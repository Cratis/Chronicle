```csharp
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

// ❌ Throws PIINotSupportedOnEventSourceId
[PII]
public record PiiConceptsEmployeeId(Guid Value) : EventSourceId<Guid>(Value);
```
