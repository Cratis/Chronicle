```csharp
using Cratis.Chronicle.Projections;
using System.Text.Json;

public class PdlOrderQueryService(IProjections projections)
{
    public async Task<IEnumerable<PdlOrderSummary>> GetOrderSummaries()
    {
        var result = await projections.Query("""
            projection OrderSummary
              from OrderPlaced
            """);

        return result.ReadModelEntries
            .Select(json => JsonSerializer.Deserialize<PdlOrderSummary>(json)!);
    }
}

public record PdlOrderSummary(string OrderId);
```
