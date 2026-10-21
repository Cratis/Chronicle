```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace FreshIdentifierExample
{
    public record OrderId(string Value) : EventSourceId<string>(Value);

    [EventType]
    public record OrderPlaced(string CustomerId);

    [EventType]
    public record OrderLineAdded(string Sku, int Quantity);

    public record OrderLine(string Sku, int Quantity);

    public class PlaceOrderService(IEventLog eventLog)
    {
        const int MaxAttempts = 3;

        // newOrderId mints a generated identifier, for example a TypeID such as order_01h455vb4pex5vsknk084sn02q.
        public async Task<OrderId?> PlaceOrder(string customerId, IEnumerable<OrderLine> lines, Func<OrderId> newOrderId)
        {
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var orderId = newOrderId();

                // "No event may exist for this OrderId yet" - the proof that the generated identifier is new.
                var mustBeNew = new ConcurrencyScopeBuilder()
                    .ExpectingNoMatchingEvent()
                    .WithEventSourceId(orderId)
                    .Build();

                var events = new List<object> { new OrderPlaced(customerId) };
                events.AddRange(lines.Select(line => new OrderLineAdded(line.Sku, line.Quantity)));

                var result = await eventLog.AppendMany(orderId, events, concurrencyScope: mustBeNew);

                if (result.IsSuccess)
                {
                    return orderId;
                }

                if (!result.HasConcurrencyViolations)
                {
                    throw new InvalidOperationException("The order could not be placed.");
                }

                // The identifier was taken. Nothing was appended, so mint a new one and try again.
            }

            return null;
        }
    }
}
```
