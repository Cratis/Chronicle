```csharp
using Cratis.Chronicle.Events;

// Error CHR0084: generation 2 has no migrator for step 1->2.
[EventType("chr0084-person-registered", generation: 2)]
public record Chr0084PersonRegistered(string FirstName, string LastName);
```
