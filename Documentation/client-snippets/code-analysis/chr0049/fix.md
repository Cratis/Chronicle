```csharp
using Cratis.Chronicle.Events;

[EventType("customer-registered", generation: 2)]
public record Chr0049FixCustomerRegisteredV2(string FirstName, string LastName);

[EventTypeGenerationFor<Chr0049FixCustomerRegisteredV2>(1)]
public record Chr0049FixCustomerRegisteredV1(string Name);
```
