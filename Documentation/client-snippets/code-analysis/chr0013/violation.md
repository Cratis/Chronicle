```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventStore("identity-service")]
[Reactor(eventSequence: "custom-sequence")]
public class Chr0013UserSyncReactor : IReactor;
```
