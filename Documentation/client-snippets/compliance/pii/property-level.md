```csharp
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

[EventType]
public record PiiAttrEmployeeRegistered(
    [PII] string FirstName,
    [PII] string LastName,
    string Department);
```
