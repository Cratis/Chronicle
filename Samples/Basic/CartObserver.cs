// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactions;

namespace Basic;

public class CartObserver : IReaction
{
    public Task ItemAdded(ItemAddedToCart @event, EventContext context)
    {
        Console.WriteLine($"Item added to cart for person with id: {@event.PersonId} and material with id: {@event.MaterialId} - EventSequenceNumber {context.SequenceNumber}");
        return Task.CompletedTask;
    }

    public Task ItemRemoved(ItemRemovedFromCart @event, EventContext context)
    {
        Console.WriteLine($"Item removed from cart for person with id: {@event.PersonId} and material with id: {@event.MaterialId}");
        return Task.CompletedTask;
    }

    public Task QuantityAdjusted(QuantityAdjustedForItemInCart @event, EventContext context)
    {
        Console.WriteLine($"Quantity adjusted for person with id: {@event.PersonId} and material with id: {@event.MaterialId} to {@event.Quantity}");
        return Task.CompletedTask;
    }
}
