```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Reducers;

public record TaggingReducersCategoryExamples(Guid Id);

// By domain
[Tag("Sales", "Inventory", "Customer")]
// By purpose
[Tag("Analytics", "Reporting", "Dashboard", "Auditing")]
// By stakeholder
[Tag("Executive", "Operations", "Finance")]
// By data type
[Tag("Aggregates", "Summaries", "Metrics")]
public class TaggingReducersCategoryExamplesReducer : IReducerFor<TaggingReducersCategoryExamples>;
```
