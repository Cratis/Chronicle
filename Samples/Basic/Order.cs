// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Aggregates;
using Aksio.Cratis.Events;

namespace Basic;

public class Order : AggregateRoot
{
    public void DoStuff()
    {
        Apply(new ItemAddedToCart(
            new(Guid.NewGuid()),
            new(Guid.NewGuid()),
            1));
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
