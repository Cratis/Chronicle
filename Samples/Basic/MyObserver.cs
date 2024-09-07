// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Basic;

public class MyObserver : IReactor
{
    public Task ItemAdded(ItemAddedToCart @event, EventContext context)
    {
        Console.WriteLine($"Item added to cart: {@event.MaterialId} - {@event.Quantity}");
        return Task.CompletedTask;
    }
}
