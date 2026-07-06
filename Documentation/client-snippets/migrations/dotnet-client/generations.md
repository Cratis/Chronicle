```csharp
using Cratis.Chronicle.Events;

// Generation 1 (original) — no explicit generation needed, defaults to 1
[EventType]
public record MigrationsDotnetClientAuthorRegisteredV1(string Name);

// Generation 2 — Name has been split into FirstName and LastName
[EventType("dotnet-client-author-registered", generation: 2)]
public record MigrationsDotnetClientAuthorRegistered(string FirstName, string LastName);
```
