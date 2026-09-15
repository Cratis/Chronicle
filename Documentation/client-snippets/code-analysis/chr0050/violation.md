```csharp
using Cratis.Chronicle.Events;

// Error CHR0050: 'Chr0050ViolationUserRegistered' is marked with both [EventType] and
// [EventTypeGenerationFor<T>].
[EventType("user-registered")]
[EventTypeGenerationFor<Chr0050ViolationUserRegisteredV2>(1)]
public record Chr0050ViolationUserRegistered(string Name, string Email);

[EventType("user-registered", generation: 2)]
public record Chr0050ViolationUserRegisteredV2(string FirstName, string LastName, string Email);
```
