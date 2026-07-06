```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Reducers;

public record TaggingReducersExecutiveDashboard(int MetricCount);

[Tag("Analytics", "Reporting")]
[Tag("Executive")]
public class TaggingReducersExecutiveDashboardReducer : IReducerFor<TaggingReducersExecutiveDashboard>;
```
