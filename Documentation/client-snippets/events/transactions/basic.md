```csharp
using Cratis.Chronicle;
using Cratis.Execution;

public static class TransactionalOrderWorkflow
{
    public static async Task CommitOrder(IEventStore store)
    {
        var unitOfWork = store.UnitOfWorkManager.Begin(CorrelationId.New());

        try
        {
            await store.EventLog.Transactional.Append(
                "order-123",
                new TransactionalOrderPlaced("order-123", 99.95m));

            await store.EventLog.Transactional.Append(
                "inventory-widget",
                new TransactionalInventoryReserved("widget", 1));

            await unitOfWork.Commit();
        }
        catch
        {
            await unitOfWork.Rollback();
            throw;
        }
    }
}

public record TransactionalOrderPlaced(string OrderId, decimal TotalAmount);
public record TransactionalInventoryReserved(string Sku, int Quantity);
```
