using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Orleans.Aggregates;
using Orleans.TestKit;
using Xunit;

namespace Orleans;

public class Testing
{
    [Fact]
    public async Task ShouldDoStuff()
    {
        var silo = new TestKitSilo();
        var order = await silo.CreateAggregateRoot<StatelessOrder>(
            "123123",
            new ItemAddedToCart(
                new(Guid.NewGuid()),
                new(Guid.NewGuid()),
                1,
                null,
                null));
        await order.DoStuff();
        var result = await order.Commit();
        result.ShouldBeSuccessful();
        result.ShouldContainEvent<ItemAddedToCart>();
    }
}
