# Tagging Projections

Tags provide a way to organize and categorize your projections for better discoverability and management. By applying the `[Tag]` attribute to your projection classes or read models, you can assign one or more tags that describe the purpose or domain of the projection.

## Adding Tags

You can tag projections in multiple ways:

### Single Tag

Apply a single tag to your projection:

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Projections;

[Tag("CustomerDashboard")]
public class CustomerOrdersProjection : IProjectionFor<CustomerOrders>
{
    public ProjectionBuilder<CustomerOrders> For { get; init; }

    public CustomerOrdersProjection()
    {
        For
            .From<OrderPlaced>(_ => _
                .Set(m => m.TotalOrders).To(ctx => ctx.State.TotalOrders + 1))
            .From<OrderCompleted>(_ => _
                .Set(m => m.CompletedOrders).To(ctx => ctx.State.CompletedOrders + 1));
    }
}
```

### Multiple Tags (Single Attribute)

Use the params feature to specify multiple tags in a single attribute:

```csharp
[Tag("Reporting", "Analytics", "Executive")]
public class SalesDashboardProjection : IProjectionFor<SalesDashboard>
{
    // Projection implementation
}
```

### Multiple Tags (Multiple Attributes)

Apply multiple `[Tag]` attributes:

```csharp
[Tag("Compliance")]
[Tag("Auditing")]
[Tag("Reporting")]
public class AuditTrailProjection : IProjectionFor<AuditTrail>
{
    // Projection implementation
}
```

### Mixed Approach

Combine both approaches:

```csharp
[Tag("Inventory", "RealTime")]
[Tag("Operations")]
public class StockLevelsProjection : IProjectionFor<StockLevels>
{
    // Projection implementation
}
```

## Tagging Model-Bound Projections

For model-bound projections (read models that use attributes), apply tags directly to the read model class:

```csharp
[Tag("Reporting", "Sales")]
public class MonthlySalesReport
{
    public string Id { get; set; }
    
    [FromEvent<OrderPlaced>("TotalAmount")]
    public decimal Revenue { get; set; }
    
    [FromEvent<OrderPlaced>]
    public int OrderCount { get; set; }
}
```

## Best Practices

- **Use meaningful names**: Choose tag names that clearly describe the purpose or domain
- **Be consistent**: Establish tag naming conventions across your organization
- **Don't over-tag**: Apply only relevant tags; too many can reduce their usefulness
- **Group related projections**: Use tags to group projections that serve similar purposes
- **Consider read model purpose**: Tag based on how the data will be consumed

## Common Tag Examples

Here are some common patterns for tagging projections:

```csharp
// By purpose
[Tag("Dashboard")]
[Tag("Reporting")]
[Tag("Analytics")]
[Tag("Search")]

// By domain
[Tag("Sales")]
[Tag("Inventory")]
[Tag("Customer")]
[Tag("Orders")]

// By stakeholder
[Tag("Executive")]
[Tag("Operations")]
[Tag("CustomerService")]
[Tag("Finance")]

// By update frequency
[Tag("RealTime")]
[Tag("Batch")]
[Tag("Hourly")]
[Tag("Daily")]

// By data type
[Tag("Summary")]
[Tag("Detail")]
[Tag("Aggregate")]
[Tag("Snapshot")]
```

## Querying by Tag

Tags stored in the event store definition can be used for:

- Filtering and searching for specific projections
- Organizing projections in administrative interfaces
- Generating documentation
- Managing projection deployments by tag
- Controlling projection activation by tag

Note: The specific querying capabilities depend on your Chronicle setup and tooling.

## See Also

- [Tagging](../concepts/tagging.md) - Comprehensive guide to tagging in Chronicle
- [Reactors Tagging](../concepts/tagging-reactors.md) - Tagging reactors
- [Reducers Tagging](../reducers/tagging-reducers.md) - Tagging reducers
