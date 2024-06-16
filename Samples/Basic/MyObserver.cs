// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Basic;

[Observer("4067d8f6-9e10-4f4b-9921-b0b703f106b5")]
public class MyObserver
{
    public Task ItemAdded(ItemAddedToCart @event, EventContext context)
    {
        Console.WriteLine($"Item added to cart: {@event.MaterialId} - {@event.Quantity}");
        return Task.CompletedTask;
    }
}
