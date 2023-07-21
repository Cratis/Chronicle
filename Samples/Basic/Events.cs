// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Reducers;

namespace Basic;

public record PersonId(Guid Value) : EventTypeId(Value);

public record MaterialId(Guid Value) : EventTypeId(Value);

[EventType("cf3a0eef-42a6-44e9-9d1c-3d48c22cafc0")]
public record MyEvent();

[EventType("147077c9-3954-4931-9a29-ea750bff97c1")]
public record ItemAddedToCart(PersonId PersonId, MaterialId MaterialId, int Quantity);

[EventType("b2581e78-eff8-4609-b166-6d0387d0f149")]
public record ItemRemovedFromCart(PersonId PersonId, MaterialId MaterialId);

[EventType("f3927a33-7028-4242-bc06-a06f8ad62b68")]
public record QuantityAdjustedForItemInCart(PersonId PersonId, MaterialId MaterialId, int Quantity);

public record CartItem(MaterialId MaterialId, int Quantity);

public record Cart(IEnumerable<CartItem> Items);

public record EventKeyContext<TEvent>(TEvent Event, EventContext EventContext);

[Reducer("ff449077-0adb-4c5c-90e6-15631cd9e2b1")]
public class CartReducer : IReducerFor<Cart>
{
    public Task<Cart> On(ItemAddedToCart @event, Cart? initial)
    {
        initial ??= new Cart(Array.Empty<CartItem>());
        return Task.FromResult(initial with
        {
            Items = initial.Items.Append(new CartItem(@event.MaterialId, @event.Quantity))
        });
    }

    public Task<Cart> On(ItemRemovedFromCart @event, Cart? initial)
    {
        initial ??= new Cart(Array.Empty<CartItem>());
        return Task.FromResult(initial with
        {
            Items = initial.Items.Where(_ => _.MaterialId != @event.MaterialId).ToArray()
        });
    }

  public Task<Cart> On(QuantityAdjustedForItemInCart @event, Cart? initial)
    {
        initial ??= new Cart(Array.Empty<CartItem>());
        return Task.FromResult(initial with
        {
            Items = initial.Items.Select(item => item.MaterialId == @event.MaterialId ? new CartItem(item.MaterialId, @event.Quantity) : item).ToArray()
        });
    }}

/*
Client
- If no explicit keys are defined, use event source id as key

Kernel:
- Key definitions are for all observer types
- Support Bulk operations for all observers (Optionally implemented by observers)
- Support running reducers in parallel - fan out based on key (partitioning)
    - This involves changing partitioning for observers
*/
