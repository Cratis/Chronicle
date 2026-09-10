```csharp
using Cratis.Chronicle.Events;

// The previous generation carries only [EventTypeGenerationFor<T>].
[EventTypeGenerationFor<Chr0050FixUserRegisteredV2>(1)]
public record Chr0050FixUserRegisteredV1(string Name, string Email);

// The current generation carries only [EventType].
[EventType("user-registered", generation: 2)]
public record Chr0050FixUserRegisteredV2(string FirstName, string LastName, string Email);
```
