# Categorizing Reducers

Categories provide a way to organize and tag your reducers for better discoverability and management. By applying the `[Category]` attribute to your reducer classes, you can assign one or more categories that describe the purpose or domain of the reducer.

## Adding Categories

You can categorize reducers in multiple ways:

### Single Category

Apply a single category to your reducer:

```csharp
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;

[Category("Analytics")]
public class OrderAnalyticsReducer : IReducerFor<OrderAnalytics>
{
    public OrderAnalytics OnOrderPlaced(OrderPlaced @event, OrderAnalytics? current, EventContext context)
    {
        // Reducer logic
    }
}
```

### Multiple Categories (Single Attribute)

Use the params feature to specify multiple categories in a single attribute:

```csharp
[Category("Analytics", "Reporting", "Dashboard")]
public class SalesReportReducer : IReducerFor<SalesReport>
{
    // Reducer implementation
}
```

### Multiple Categories (Multiple Attributes)

Apply multiple `[Category]` attributes:

```csharp
[Category("Analytics")]
[Category("Compliance")]
[Category("Auditing")]
public class ComplianceReportReducer : IReducerFor<ComplianceReport>
{
    // Reducer implementation
}
```

### Mixed Approach

Combine both approaches:

```csharp
[Category("Analytics", "Reporting")]
[Category("Executive")]
public class ExecutiveDashboardReducer : IReducerFor<ExecutiveDashboard>
{
    // Reducer implementation
}
```

## Best Practices

- **Use meaningful names**: Choose category names that clearly describe the purpose or domain
- **Be consistent**: Establish category naming conventions across your organization
- **Don't over-categorize**: Apply only relevant categories; too many can reduce their usefulness
- **Group related reducers**: Use categories to group reducers that serve similar purposes

## Common Category Examples

Here are some common patterns for categorizing reducers:

```csharp
// By domain
[Category("Sales")]
[Category("Inventory")]
[Category("Customer")]

// By purpose
[Category("Analytics")]
[Category("Reporting")]
[Category("Dashboard")]
[Category("Auditing")]

// By stakeholder
[Category("Executive")]
[Category("Operations")]
[Category("Finance")]

// By data type
[Category("Aggregates")]
[Category("Summaries")]
[Category("Metrics")]
```

## Querying by Category

Categories stored in the event store definition can be used for:

- Filtering and searching for specific reducers
- Organizing reducers in administrative interfaces
- Generating documentation
- Managing reducer deployments by category

Note: The specific querying capabilities depend on your Chronicle setup and tooling.
