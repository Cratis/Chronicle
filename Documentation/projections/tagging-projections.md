# Tagging Projections

Tags provide a way to organize and tag your projections for better discoverability and management. By applying the `[Tag]` attribute to your projection classes, you can assign one or more tags that describe the purpose or domain of the projection.

## Adding Tags

You can tag projections in multiple ways:

### Single Tag

Apply a single tag to your projection:

```csharp
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;

[Tag("Analytics")]
public class OrderAnalyticsProjection : IProjectionFor<OrderAnalytics>
{
    public ProjectionId Identifier => "order-analytics";

    public void Define(IProjectionBuilderFor<OrderAnalytics> builder)
    {
        builder
            .From<OrderPlaced>(_ => _
                .Set(m => m.OrderId).To(e => e.OrderId))
            .From<ItemAddedToOrder>(_ => _
                .Add(m => m.TotalAmount).With(e => e.Price * e.Quantity));
    }
}
```

### Multiple Tags (Single Attribute)

Use the params feature to specify multiple tags in a single attribute:

```csharp
[Tag("Analytics", "Reporting", "Dashboard")]
public class SalesReportProjection : IProjectionFor<SalesReport>
{
    public ProjectionId Identifier => "sales-report";

    public void Define(IProjectionBuilderFor<SalesReport> builder)
    {
        // Projection definition
    }
}
```

### Multiple Tags (Multiple Attributes)

Apply multiple `[Tag]` attributes:

```csharp
[Tag("Analytics")]
[Tag("Compliance")]
[Tag("Auditing")]
public class ComplianceReportProjection : IProjectionFor<ComplianceReport>
{
    public ProjectionId Identifier => "compliance-report";

    public void Define(IProjectionBuilderFor<ComplianceReport> builder)
    {
        // Projection definition
    }
}
```

### Mixed Approach

Combine both approaches:

```csharp
[Tag("Analytics", "Reporting")]
[Tag("Executive")]
public class ExecutiveDashboardProjection : IProjectionFor<ExecutiveDashboard>
{
    public ProjectionId Identifier => "executive-dashboard";

    public void Define(IProjectionBuilderFor<ExecutiveDashboard> builder)
    {
        // Projection definition
    }
}
```

## Model-Bound Projections

Tags also work with model-bound projections:

```csharp
using Cratis.Chronicle.Projections;

[Tag("Inventory", "Operations")]
public record ProductInventory(
    Guid ProductId,
    string Name,
    int QuantityInStock,
    decimal UnitPrice);
```

## Best Practices

- **Use meaningful names**: Choose tag names that clearly describe the purpose or domain
- **Be consistent**: Establish tag naming conventions across your organization
- **Don't over-tag**: Apply only relevant tags; too many can reduce their usefulness
- **Group related projections**: Use tags to group projections that serve similar purposes

## Common Tag Examples

Here are some common patterns for tagging projections:

```csharp
// By domain
[Tag("Sales")]
[Tag("Inventory")]
[Tag("Customer")]

// By purpose
[Tag("Analytics")]
[Tag("Reporting")]
[Tag("Dashboard")]
[Tag("Search")]

// By stakeholder
[Tag("Executive")]
[Tag("Operations")]
[Tag("Finance")]

// By consistency model
[Tag("Immediate")]
[Tag("Eventual")]

// By data type
[Tag("Aggregates")]
[Tag("Lists")]
[Tag("Details")]
```

## Querying by Tag

Tags stored in the event store definition can be used for:

- Filtering and searching for specific projections
- Organizing projections in administrative interfaces
- Generating documentation
- Managing projection deployments by tag
- Grouping projections for rebuild operations

Note: The specific querying capabilities depend on your Chronicle setup and tooling.

## See Also

- [Tagging](../concepts/tagging.md) - Comprehensive guide to tagging in Chronicle
- [Reactors Tagging](../concepts/tagging-reactors.md) - Tagging reactors
- [Reducers Tagging](../reducers/tagging-reducers.md) - Tagging reducers
