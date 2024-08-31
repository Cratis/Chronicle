// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Orleans.Aggregates;
using Orleans.TestKit;
using Xunit;

namespace Orleans;

public class Testing
{
    [Fact]
    public async Task TestStatelessAggregate()
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

    [Fact]
    public async Task TestStatefulAggregate()
    {
        var silo = new TestKitSilo();
        var statefulOrder = await silo.CreateAggregateRoot<Order, OrderState>(
            "123123",
            new OrderState(15, []));

        await statefulOrder.DoStuff();
        var result = await statefulOrder.Commit();
        result.ShouldBeSuccessful();
        result.ShouldContainEvent<ItemAddedToCart>();
    }
}
