```csharp
using Cratis.Chronicle.Reducers;

public record PassiveReducersExperimentalMetrics(int SampleCount);

#if DEBUG
[Reducer(isActive: false)]
#endif
public class PassiveReducersExperimentalMetricsReducer : IReducerFor<PassiveReducersExperimentalMetrics>;
```
