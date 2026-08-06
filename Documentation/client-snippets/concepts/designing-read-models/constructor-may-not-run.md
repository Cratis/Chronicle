```csharp
public record LineItem(string ProductName, int Quantity, decimal Price);

// Don't rely on this. It may never execute.
public record OrderSummary(OrderId Id, IEnumerable<LineItem> Lines)
{
    public IEnumerable<LineItem> Lines { get; init; } = Lines ?? [];
}
```
