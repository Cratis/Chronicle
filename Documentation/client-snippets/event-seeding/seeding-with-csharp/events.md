```csharp
using Cratis.Chronicle.Events;

[EventType]
public record EvtSeedingUserRegistered(string Email, string DisplayName);

[EventType]
public record EvtSeedingEmailVerified(string Email);

[EventType]
public record EvtSeedingProfileUpdated(string DisplayName);

[EventType]
public record EvtSeedingOrderPlaced(string UserId, decimal Amount);
```
