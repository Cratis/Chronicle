```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Specifications;

[EventType]
public record TestingReadModelScenarioFluentProductCreated(string Name);

[EventType]
public record TestingReadModelScenarioFluentStockAdjusted(int NewStock);

public record TestingReadModelScenarioFluentProductView(string Name, int Stock);

public class TestingReadModelScenarioFluentProductViewProjection : IProjectionFor<TestingReadModelScenarioFluentProductView>
{
    public void Define(IProjectionBuilderFor<TestingReadModelScenarioFluentProductView> builder) =>
        builder
            .From<TestingReadModelScenarioFluentProductCreated>(_ => _
                .Set(m => m.Name).To(e => e.Name))
            .From<TestingReadModelScenarioFluentStockAdjusted>(_ => _
                .Set(m => m.Stock).To(e => e.NewStock));
}

public static class TestingReadModelScenarioFluentProjectionExample
{
    public static async Task Run()
    {
        var productId = "product-1";
        var scenario = new ReadModelScenario<TestingReadModelScenarioFluentProductView>();
        await scenario.Given
            .ForEventSource(productId)
            .Events(
                new TestingReadModelScenarioFluentProductCreated("Widget"),
                new TestingReadModelScenarioFluentStockAdjusted(100));

        scenario.Instance!.Name.ShouldEqual("Widget");
        scenario.Instance!.Stock.ShouldEqual(100);
    }
}
```
