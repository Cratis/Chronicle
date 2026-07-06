```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;

[EventType]
public record TestingScenarioItemAddedToCart(string ItemId);

[EventType]
public record TestingScenarioItemQuantityAdjusted(string ItemId, int Quantity);

public static class TestingScenarioAppendMany
{
    public static async Task Run()
    {
        var cartId = EventSourceId.New();
        var itemId1 = "item-1";
        var itemId2 = "item-2";
        var scenario = new EventScenario();

        var result = await scenario.EventLog.AppendMany(cartId, [
            new TestingScenarioItemAddedToCart(itemId1),
            new TestingScenarioItemAddedToCart(itemId2),
            new TestingScenarioItemQuantityAdjusted(itemId1, 3)
        ]);
        result.ShouldBeSuccessful();
    }
}
```
