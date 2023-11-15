// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using Aksio.Cratis.Aggregates;
using Aksio.Cratis.Events;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Reducers;

namespace Basic;

public class Order : AggregateRoot<OrderState>
{
    public async Task DoStuff()
    {
        Console.WriteLine($"Before : {State.CartItems.Count()}");

        await Apply(new ItemAddedToCart(
            new(Guid.NewGuid()),
            new(Guid.NewGuid()),
            1));

        Console.WriteLine($"After : {State.CartItems.Count()}");
    }

    public async Task DoOtherStuff()
    {
        await Apply(new ItemRemovedFromCart(
            new(Guid.NewGuid()),
            new(Guid.NewGuid())));
    }

    void On(ItemAddedToCart @event)
    {
        Console.WriteLine("Added");
    }

    void On(ItemRemovedFromCart @event, EventContext context)
    {
        Console.WriteLine("Removed");
    }
}


public record OrderState(int Items, IEnumerable<CartItem> CartItems);

// [Reducer("5027a520-6d25-47c7-9d52-b1e9f82905d2")]
// public class OrderStateReducer : IReducerFor<OrderState>
// {
//     public Task<OrderState> ItemAdded(ItemAddedToCart @event, OrderState? initial, EventContext context)
//     {
//         initial ??= new OrderState(0, Enumerable.Empty<CartItem>());
//         initial = initial with { Items = initial.Items + 1 };
//         return Task.FromResult(initial);
//     }
// }


public class OrderStateProjection : IImmediateProjectionFor<OrderState>
{
    public ProjectionId Identifier => "4c6f7eac-d74d-425b-b2fd-e32e8e365b32";

    public void Define(IProjectionBuilderFor<OrderState> builder) => builder
        .Children(_ => _.CartItems, cb => cb
            .IdentifiedBy(m => m.MaterialId)
            .From<ItemAddedToCart>(_ => _
                .UsingKey(e => e.MaterialId)
                .Set(m => m.Quantity).To(e => e.Quantity))
            .RemovedWith<ItemRemovedFromCart>()
            .From<QuantityAdjustedForItemInCart>(_ => _
                .UsingKey(e => e.MaterialId)
                .Set(m => m.Quantity).To(e => e.Quantity)));
}
