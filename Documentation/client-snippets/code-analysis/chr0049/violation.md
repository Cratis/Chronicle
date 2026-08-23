```csharp
using Cratis.Chronicle.Events;

public record CustomerRegisteredV2(string FirstName, string LastName);

// Error CHR0049: 'CustomerRegisteredV1' declares itself as a generation for
// 'CustomerRegisteredV2', but 'CustomerRegisteredV2' is not marked with [EventType].
[EventTypeGenerationFor<CustomerRegisteredV2>(1)]
public record CustomerRegisteredV1(string Name);
```
