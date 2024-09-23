// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Shared.Carts;

namespace Shared.Orders;

public class OrderReactor : IReactor
{
    public Task On(ItemAddedToCart @event, EventContext context)
    {
        Console.WriteLine($"Added : {context.SequenceNumber}");
        return Task.CompletedTask;
    }

    public Task On(ItemRemovedFromCart @event, EventContext context)
    {
        Console.WriteLine($"Removed : {context.SequenceNumber}");
        return Task.CompletedTask;
    }
}
