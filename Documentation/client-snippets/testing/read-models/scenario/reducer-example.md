```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Specifications;

[EventType]
public record TestingReadModelScenarioReducerOrderCreated(string OrderId);

[EventType]
public record TestingReadModelScenarioReducerItemAdded(decimal Price);

public record TestingReadModelScenarioReducerOrderSummary(string OrderId, decimal Total);

public class TestingReadModelScenarioReducerOrderSummaryReducer : IReducerFor<TestingReadModelScenarioReducerOrderSummary>
{
    public TestingReadModelScenarioReducerOrderSummary OnOrderCreated(TestingReadModelScenarioReducerOrderCreated @event, TestingReadModelScenarioReducerOrderSummary? current, EventContext context) =>
        new(@event.OrderId, 0m);

    public TestingReadModelScenarioReducerOrderSummary OnItemAdded(TestingReadModelScenarioReducerItemAdded @event, TestingReadModelScenarioReducerOrderSummary current, EventContext context) =>
        current with { Total = current.Total + @event.Price };
}

public static class TestingReadModelScenarioReducerExample
{
    public static async Task Run()
    {
        var orderId = "order-1";
        var scenario = new ReadModelScenario<TestingReadModelScenarioReducerOrderSummary>();
        await scenario.Given
            .ForEventSource(orderId)
            .Events(
                new TestingReadModelScenarioReducerOrderCreated("order-1"),
                new TestingReadModelScenarioReducerItemAdded(9.99m),
                new TestingReadModelScenarioReducerItemAdded(4.50m));

        scenario.Instance!.Total.ShouldEqual(14.49m);
    }
}
```
