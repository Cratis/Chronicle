```csharp
using Cratis.Chronicle.Sinks;

[Sink("reporting")]
public record OrderTotals(string CustomerId, decimal Total);
```
