```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Reducers;

public record TaggingReducersSalesReport(decimal TotalSales);

[Tag("Analytics", "Reporting", "Dashboard")]
public class TaggingReducersSalesReportReducer : IReducerFor<TaggingReducersSalesReport>;
```
