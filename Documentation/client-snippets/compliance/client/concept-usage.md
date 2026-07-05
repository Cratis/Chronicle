```csharp
using Cratis.Chronicle.Events;

[EventType]
public record ComplianceClientEmployeeRegisteredWithConcept(ComplianceClientPersonName Name, string Department);

[EventType]
public record ComplianceClientEmployeeNameChanged(ComplianceClientPersonName NewName);  // also encrypted
```
