```csharp
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Specifications;

public class TestingReadModelScenarioOrderService(IReadModels readModels)
{
    public async Task<decimal> GetOrderTotal(string orderId)
    {
        var order = await readModels.GetInstanceById<TestingReadModelScenarioOrderSummary>(orderId);
        return order.Total;
    }
}

public static class TestingReadModelScenarioPreseedingInstances
{
    public static async Task Run()
    {
        var scenario = new ReadModelScenario<TestingReadModelScenarioOrderSummary>();
        await scenario.Given
            .ForEventSourceId("order-1")
            .ReadModel(new TestingReadModelScenarioOrderSummary("order-1", 99.99m));

        // Pass scenario.ReadModels to production code under test
        var sut = new TestingReadModelScenarioOrderService(scenario.ReadModels);
        var result = await sut.GetOrderTotal("order-1");

        result.ShouldEqual(99.99m);
    }
}
```
