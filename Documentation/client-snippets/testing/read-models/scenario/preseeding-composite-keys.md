```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Specifications;

public static class TestingReadModelScenarioCompositeSeeding
{
    public static async Task Run()
    {
        using var scenario = new ReadModelScenario<SeededCompositeOrder>().WithProjection(builder => builder
            .From<CompositeOrderUpdated>(from => from
                .UsingCompositeKey<SeededCompositeOrderKey>(key => key
                    .Set(part => part.TenantId).To(@event => @event.TenantId)
                    .Set(part => part.OrderNumber).To(@event => @event.OrderNumber))
                .Increment(model => model.Revision)));

        var parts = new Dictionary<string, object?>
        {
            ["tenantId"] = "tenant-a",
            ["orderNumber"] = 42
        };
        await scenario.Given.ForKey(parts).ReadModel(new(new("tenant-a", 42), 5));
        await scenario.Given.ForEventSourceId("source").Events(new CompositeOrderUpdated("tenant-a", 42));

        scenario.InstanceForKey(parts)!.Revision.ShouldEqual(6);
    }
}

[EventType]
public record CompositeOrderUpdated(string TenantId, int OrderNumber);
public record SeededCompositeOrderKey(string TenantId, int OrderNumber);
public record SeededCompositeOrder(SeededCompositeOrderKey Id, int Revision);
```
