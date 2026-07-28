```csharp
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[PII]
public record Chr0038AdvisorName(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator Chr0038AdvisorName(string value) => new(value);
}

[EventType]
public record Chr0038AdvisorNamed(Chr0038AdvisorName DisplayName);

// Error CHR0038: The [Join<Chr0038AdvisorNamed>] on 'AdvisorName' copies the [PII] value
// 'Chr0038AdvisorNamed.DisplayName' out of the stream identified by 'AdvisorId', which is not
// this read model's compliance subject.
public record Chr0038RequestSummary(
    [Key] Guid Id,
    Guid AdvisorId,
    [Join<Chr0038AdvisorNamed>(on: "AdvisorId", eventPropertyName: "DisplayName")] Chr0038AdvisorName AdvisorName);
```
