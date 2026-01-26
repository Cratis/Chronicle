# Categorizing Projections

Categories provide a way to organize and tag your projections for better discoverability and management. By applying the `[Category]` attribute to your projection classes, you can assign one or more categories that describe the purpose or domain of the projection.

## Adding Categories

You can categorize projections in multiple ways:

### Single Category

Apply a single category to your projection:

```csharp
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;

[Category("Analytics")]
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

### Multiple Categories (Single Attribute)

Use the params feature to specify multiple categories in a single attribute:

```csharp
[Category("Analytics", "Reporting", "Dashboard")]
public class SalesReportProjection : IProjectionFor<SalesReport>
{
    public ProjectionId Identifier => "sales-report";

    public void Define(IProjectionBuilderFor<SalesReport> builder)
    {
        // Projection definition
    }
}
```

### Multiple Categories (Multiple Attributes)

Apply multiple `[Category]` attributes:

```csharp
[Category("Analytics")]
[Category("Compliance")]
[Category("Auditing")]
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
[Category("Analytics", "Reporting")]
[Category("Executive")]
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

Categories also work with model-bound projections:

```csharp
using Cratis.Chronicle.Projections;

[Category("Inventory", "Operations")]
public record ProductInventory(
    Guid ProductId,
    string Name,
    int QuantityInStock,
    decimal UnitPrice);
```

## Best Practices

- **Use meaningful names**: Choose category names that clearly describe the purpose or domain
- **Be consistent**: Establish category naming conventions across your organization
- **Don't over-categorize**: Apply only relevant categories; too many can reduce their usefulness
- **Group related projections**: Use categories to group projections that serve similar purposes

## Common Category Examples

Here are some common patterns for categorizing projections:

```csharp
// By domain
[Category("Sales")]
[Category("Inventory")]
[Category("Customer")]

// By purpose
[Category("Analytics")]
[Category("Reporting")]
[Category("Dashboard")]
[Category("Search")]

// By stakeholder
[Category("Executive")]
[Category("Operations")]
[Category("Finance")]

// By consistency model
[Category("Immediate")]
[Category("Eventual")]

// By data type
[Category("Aggregates")]
[Category("Lists")]
[Category("Details")]
```

## Querying by Category

Categories stored in the event store definition can be used for:

- Filtering and searching for specific projections
- Organizing projections in administrative interfaces
- Generating documentation
- Managing projection deployments by category
- Grouping projections for rebuild operations

Note: The specific querying capabilities depend on your Chronicle setup and tooling.
