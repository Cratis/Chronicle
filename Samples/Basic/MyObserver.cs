// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactions;

namespace Basic;

public class MyObserver : IReaction
{
    public Task ItemAdded(ItemAddedToCart @event, EventContext context)
    {
        Console.WriteLine($"Item added to cart: {@event.MaterialId} - {@event.Quantity}");
        return Task.CompletedTask;
    }
}
