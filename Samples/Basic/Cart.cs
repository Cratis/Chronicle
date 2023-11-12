// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Aggregates;
using Aksio.Cratis.Events;

namespace Basic;

public record Cart(CartId Id, IEnumerable<CartItem> Items);


public class Order : AggregateRoot
{
    public void DoStuff()
    {
        Apply(new ItemAddedToCart(
            new(Guid.NewGuid()),
            new(Guid.NewGuid()),
            1));
    }

    void On(ItemAddedToCart @event)
    {

    }

    void On(ItemRemovedFromCart @event, EventContext context)
    {

    }
}
