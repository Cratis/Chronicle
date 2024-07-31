// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Orleans.Aggregates;

namespace Orleans;

public class StatelessOrder : AggregateRoot, IOrder
{
    public async Task DoStuff()
    {
        await Apply(new ItemAddedToCart(
            new(Guid.NewGuid()),
            new(Guid.NewGuid()),
            1,
            null,
            null));
    }

    public async Task DoOtherStuff()
    {
        await Apply(new ItemRemovedFromCart(
            new(Guid.NewGuid()),
            new(Guid.NewGuid())));
    }

    public void On(ItemAddedToCart @event)
    {
        Console.WriteLine("Added");
    }

    public void On(ItemRemovedFromCart @event, EventContext context)
    {
        Console.WriteLine("Removed");
    }
}
