```csharp
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

// ❌ This will throw PIINotSupportedOnEventSourceId at startup
[PII]
public record ComplianceClientCustomerId(Guid Value) : EventSourceId<Guid>(Value);
```
