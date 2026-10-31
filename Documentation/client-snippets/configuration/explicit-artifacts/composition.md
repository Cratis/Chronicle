```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events.Constraints;

namespace ExplicitCompositionExample
{
    public record OrderPlaced(decimal Total);
    public record OrderTotal(decimal Total);

    public class TotalReducer
    {
        public OrderTotal Reduce(OrderPlaced @event, OrderTotal? current) =>
            new((current?.Total ?? 0m) + @event.Total);
    }

    public class OrderNotifier
    {
        public void On(OrderPlaced @event) => Console.WriteLine($"Order total: {@event.Total}");
    }

    public class OneOrderPerSource
    {
        public void Define(IConstraintBuilder builder) => builder.Unique<OrderPlaced>();
    }

    public class Customers;

    public static class Composition
    {
        public static ChronicleOptions CreateOptions()
        {
            var options = new ChronicleOptions();
            options.ExplicitArtifacts
                .RegisterEventType<OrderPlaced>(_ => _.WithId("order-placed").Public())
                .RegisterReactor<OrderNotifier>("order-notifier")
                .RegisterReducer<TotalReducer>("order-total")
                .RegisterConstraint<OneOrderPerSource>()
                .RegisterEventSource<Customers>("customers");
            return options;
        }
    }
}
```
