// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Events;
using Shared.Carts;

namespace Shared.Orders;

public class Order : AggregateRoot<OrderState>
{
    public async Task DoStuff()
    {
        Console.WriteLine($"Before : {State?.CartItems?.Count()}");

        await Apply(new ItemAddedToCart(
            new(Guid.NewGuid()),
            new(Guid.NewGuid()),
            1,
            null,
            null));

        Console.WriteLine($"After : {State?.CartItems?.Count()}");
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
