```csharp
using Cratis.Chronicle.Events;

[EventType("customer-registered", generation: 2)]
public record CustomerRegisteredV2(string FirstName, string LastName);

[EventTypeGenerationFor<CustomerRegisteredV2>(1)]
public record CustomerRegisteredV1(string Name);
```
