// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Reducers;

namespace Basic;

[Reducer("ff449077-0adb-4c5c-90e6-15631cd9e2b1")]
public class CartReducer : IReducerFor<Cart>
{
    public Key<ItemAddedToCart> Key => _ => _.UsingKey(_ => _.PersonId);

    public Task<Cart> ItemAdded(ItemAddedToCart @event, Cart? initial, EventContext context)
    {
        initial ??= new Cart(context.EventSourceId, []);
        return Task.FromResult(initial with
        {
            Items = initial.Items?.Append(new CartItem(@event.MaterialId, @event.Quantity)) ??
                [new CartItem(@event.MaterialId, @event.Quantity)]
        });
    }

    public Task<Cart> ItemRemoved(ItemRemovedFromCart @event, Cart? initial, EventContext context)
    {
        initial ??= new Cart(context.EventSourceId, []);
        return Task.FromResult(initial with
        {
            Items = initial.Items?.Where(_ => _.MaterialId != @event.MaterialId).ToArray() ??
                []
        });
    }

    public Task<Cart> QuantityAdjusted(QuantityAdjustedForItemInCart @event, Cart? initial, EventContext context)
    {
        initial ??= new Cart(context.EventSourceId, []);
        return Task.FromResult(initial with
        {
            Items = initial.Items.Select(item => item.MaterialId == @event.MaterialId ? new CartItem(item.MaterialId, @event.Quantity) : item).ToArray()
        });
    }
}
