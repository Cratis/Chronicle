```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Specifications;

[EventType]
public record TestingReadModelScenarioJoinCustomerRegistered(string Name);

[EventType]
public record TestingReadModelScenarioJoinOrderPlaced(string CustomerId, decimal Amount);

[FromEvent<TestingReadModelScenarioJoinOrderPlaced>]
public record TestingReadModelScenarioJoinOrder(
    [Key] string Id,
    string CustomerId,
    [Join<TestingReadModelScenarioJoinCustomerRegistered>(on: nameof(CustomerId), eventPropertyName: nameof(TestingReadModelScenarioJoinCustomerRegistered.Name))] string CustomerName,
    decimal Amount);

public static class TestingReadModelScenarioMultipleInstances
{
    public static async Task Run()
    {
        var scenario = new ReadModelScenario<TestingReadModelScenarioJoinOrder>();
        var customerId = EventSourceId.New();
        var orderId = EventSourceId.New();

        await scenario.Given.ForEventSource(customerId).Events(new TestingReadModelScenarioJoinCustomerRegistered("Ada"));
        await scenario.Given.ForEventSource(orderId).Events(new TestingReadModelScenarioJoinOrderPlaced(customerId.Value, 100m));

        var order = scenario.InstanceForEventSourceId(orderId);
        order!.CustomerName.ShouldEqual("Ada");
    }
}
```
