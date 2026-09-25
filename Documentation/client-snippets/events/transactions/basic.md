```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
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

            // Commit reports a rejected batch through the unit of work; it does not throw.
            if (!unitOfWork.IsSuccess)
            {
                throw new OrderWasNotCommitted(unitOfWork.GetConstraintViolations(), unitOfWork.GetAppendErrors());
            }
        }
        catch
        {
            // Commit completes the unit of work even when the batch is rejected, and a completed
            // unit of work cannot be rolled back. Only roll back what was never committed.
            if (!unitOfWork.IsCompleted)
            {
                await unitOfWork.Rollback();
            }

            throw;
        }
    }
}

[EventType]
public record TransactionalOrderPlaced(string OrderId, decimal TotalAmount);

[EventType]
public record TransactionalInventoryReserved(string Sku, int Quantity);

public class OrderWasNotCommitted(IEnumerable<object> violations, IEnumerable<object> errors)
    : Exception($"The order was not committed: {string.Join("; ", violations.Concat(errors))}");
```
