```csharp
using Cratis.Chronicle.Events;

// Generation 2 (current) — Name has been split into FirstName and LastName
[EventType("dotnet-client-author-registered", generation: 2)]
public record MigrationsDotnetClientAuthorRegistered(string FirstName, string LastName);

// Generation 1 (original) — marked as a previous generation of the current record above,
// instead of carrying its own [EventType]
[EventTypeGenerationFor<MigrationsDotnetClientAuthorRegistered>(1)]
public record MigrationsDotnetClientAuthorRegisteredV1(string Name);
```
