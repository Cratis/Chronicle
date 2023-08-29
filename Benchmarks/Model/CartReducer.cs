// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Keys;
using Aksio.Cratis.Reducers;

namespace Benchmark.Model;

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
