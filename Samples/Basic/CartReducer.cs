using System;
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Concepts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;

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


[Reducer("ff449077-0adb-4c5c-90e6-15631cd9e2b1")]
public class CartReducer : IReducerFor<Cart>
{
    public Key<ItemAddedToCart> Key => _ => _.UsingKey(_ => _.PersonId);

    public Task<Cart> ItemAdded(ItemAddedToCart @event, Cart? initial, EventContext context)
    {
        initial ??= new Cart(context.EventSourceId, Array.Empty<CartItem>());
        return Task.FromResult(initial with
        {
            Items = initial.Items?.Append(new CartItem(@event.MaterialId, @event.Quantity)) ??
                new[] { new CartItem(@event.MaterialId, @event.Quantity) }
        });
    }

    public Task<Cart> ItemRemoved(ItemRemovedFromCart @event, Cart? initial, EventContext context)
    {
        initial ??= new Cart(context.EventSourceId, Array.Empty<CartItem>());
        return Task.FromResult(initial with
        {
            Items = initial.Items?.Where(_ => _.MaterialId != @event.MaterialId).ToArray() ??
                Array.Empty<CartItem>()
        });
    }

    public Task<Cart> QuantityAdjusted(QuantityAdjustedForItemInCart @event, Cart? initial, EventContext context)
    {
        initial ??= new Cart(context.EventSourceId, Array.Empty<CartItem>());
        return Task.FromResult(initial with
        {
            Items = initial.Items.Select(item => item.MaterialId == @event.MaterialId ? new CartItem(item.MaterialId, @event.Quantity) : item).ToArray()
        });
    }
}

/*
Client
- If no explicit keys are defined, use event source id as key

Kernel:
- Key definitions are for all observer types
- Support Bulk operations for all observers (Optionally implemented by observers)
- Support running reducers in parallel - fan out based on key (partitioning)
    - This involves changing partitioning for observers
*/
