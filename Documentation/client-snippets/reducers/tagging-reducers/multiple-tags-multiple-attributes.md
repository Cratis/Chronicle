```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Reducers;

public record TaggingReducersComplianceReport(string Status);

[Tag("Analytics")]
[Tag("Compliance")]
[Tag("Auditing")]
public class TaggingReducersComplianceReportReducer : IReducerFor<TaggingReducersComplianceReport>;
```
