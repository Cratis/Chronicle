```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record Chr0003ProductAddedFixed(Guid ProductId, string Name, decimal Price);

// Now valid
[FromEvent<Chr0003ProductAddedFixed>]
public class Chr0003ProductReadModelFixed
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```
