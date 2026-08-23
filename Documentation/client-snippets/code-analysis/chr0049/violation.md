```csharp
using Cratis.Chronicle.Events;

public record Chr0049ViolationCustomerRegisteredV2(string FirstName, string LastName);

// Error CHR0049: 'Chr0049ViolationCustomerRegisteredV1' declares itself as a generation for
// 'Chr0049ViolationCustomerRegisteredV2', but 'Chr0049ViolationCustomerRegisteredV2' is not
// marked with [EventType].
[EventTypeGenerationFor<Chr0049ViolationCustomerRegisteredV2>(1)]
public record Chr0049ViolationCustomerRegisteredV1(string Name);
```
