```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventStore("identity-service")]
[Reducer(eventSequence: "custom-sequence")]
public class Chr0014UserSummaryReducer : IReducer;
```
