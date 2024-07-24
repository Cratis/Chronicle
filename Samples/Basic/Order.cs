// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Events;

namespace Basic;

public class Order : AggregateRoot<OrderState>
{
    public void DoStuff()
    {
        //Console.WriteLine($"Before : {State!.CartItems.Count()}");

        Apply(new ItemAddedToCart(
            new(Guid.NewGuid()),
            new(Guid.NewGuid()),
            1,
            null,
            null));

        //Console.WriteLine($"After : {State.CartItems.Count()}");
    }

    public void DoOtherStuff()
    {
        Apply(new ItemRemovedFromCart(
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
