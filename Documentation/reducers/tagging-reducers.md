# Tagging Reducers

Tags provide a way to organize and categorize your reducers for better discoverability and management. By applying the `[Tag]` attribute to your reducer classes, you can assign one or more tags that describe the purpose or domain of the reducer.

> **Note**: Tags replaced the previous `[Category]` attribute. Use `[Tag]` for all new code.

## Adding Tags

You can tag reducers in multiple ways:

### Single Tag

Apply a single tag to your reducer:

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Reducers;

[Tag("Analytics")]
public class OrderAnalyticsReducer : IReducerFor<OrderAnalytics>
{
    public OrderAnalytics OnOrderPlaced(OrderPlaced @event, OrderAnalytics? current, EventContext context)
    {
        // Reducer logic
    }
}
```

### Multiple Tags (Single Attribute)

Use the params feature to specify multiple tags in a single attribute:

```csharp
[Tag("Analytics", "Reporting", "Dashboard")]
public class SalesReportReducer : IReducerFor<SalesReport>
{
    // Reducer implementation
}
```

### Multiple Tags (Multiple Attributes)

Apply multiple `[Tag]` attributes:

```csharp
[Tag("Analytics")]
[Tag("Compliance")]
[Tag("Auditing")]
public class ComplianceReportReducer : IReducerFor<ComplianceReport>
{
    // Reducer implementation
}
```

### Mixed Approach

Combine both approaches:

```csharp
[Tag("Analytics", "Reporting")]
[Tag("Executive")]
public class ExecutiveDashboardReducer : IReducerFor<ExecutiveDashboard>
{
    // Reducer implementation
}
```

## Best Practices

- **Use meaningful names**: Choose tag names that clearly describe the purpose or domain
- **Be consistent**: Establish tag naming conventions across your organization
- **Don't over-tag**: Apply only relevant tags; too many can reduce their usefulness
- **Group related reducers**: Use tags to group reducers that serve similar purposes

## Common Tag Examples

Here are some common patterns for tagging reducers:

```csharp
// By domain
[Tag("Sales")]
[Tag("Inventory")]
[Tag("Customer")]

// By purpose
[Tag("Analytics")]
[Tag("Reporting")]
[Tag("Dashboard")]
[Tag("Auditing")]

// By stakeholder
[Tag("Executive")]
[Tag("Operations")]
[Tag("Finance")]

// By data type
[Tag("Aggregates")]
[Tag("Summaries")]
[Tag("Metrics")]
```

## Querying by Tag

Tags stored in the event store definition can be used for:

- Filtering and searching for specific reducers
- Organizing reducers in administrative interfaces
- Generating documentation
- Managing reducer deployments by tag

Note: The specific querying capabilities depend on your Chronicle setup and tooling.

## See Also

- [Tagging](../concepts/tagging.md) - Comprehensive guide to tagging in Chronicle
- [Reactors Tagging](../concepts/tagging-reactors.md) - Tagging reactors

