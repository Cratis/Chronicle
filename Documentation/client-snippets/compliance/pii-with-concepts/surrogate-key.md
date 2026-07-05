```csharp
using Cratis.Chronicle.Events;

// ✅ Surrogate key as event source identifier
public record PiiConceptsSurrogateEmployeeId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static PiiConceptsSurrogateEmployeeId New() => new(Guid.NewGuid());
}

// ✅ Sensitive value stored in a PII-marked concept type
[EventType]
public record PiiConceptsSurrogateEmployeeRegistered(PiiConceptsNationalIdNumber NationalId, PiiConceptsPersonName Name);
```
