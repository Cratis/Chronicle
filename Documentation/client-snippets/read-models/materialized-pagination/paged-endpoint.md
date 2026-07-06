```csharp
using Cratis.Chronicle;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("orders")]
public class MaterializedPaginationOrdersController(IEventStore eventStore) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<MaterializedPaginationOrder>> GetOrders(
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20)
    {
        return await eventStore.ReadModels.Materialized.GetInstances<MaterializedPaginationOrder>(
            skip: page * pageSize,
            take: pageSize);
    }
}
```
