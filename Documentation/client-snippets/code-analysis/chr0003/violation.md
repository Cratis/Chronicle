```csharp
using Cratis.Chronicle.Projections.ModelBound;

// Missing [EventType] attribute
public record Chr0003ProductAdded(Guid ProductId, string Name, decimal Price);

// CHR0003: Type 'Chr0003ProductAdded' must be marked with [EventType] attribute
[FromEvent<Chr0003ProductAdded>]
public class Chr0003ProductReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```
