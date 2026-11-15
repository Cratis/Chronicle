```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[EventType]
[Public]
public record AudiencesInvitationIssued([property: Audience] string Lobby, string Email);
```
