```csharp
using Cratis.Chronicle.Reducers;

public record PassiveReducersSwitchableReadModel(int Value);

// Was active, now passive
[Reducer(isActive: false)]
public class PassiveReducersSwitchableReducer : IReducerFor<PassiveReducersSwitchableReadModel>;
```
