// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;
using MongoDB.Driver;

namespace Benchmark.Model;

[Observer(Identifier)]
public class CartObserver
{
    public const string Identifier = "744d9b53-1b59-487b-bda2-377680f366cd";
    readonly IMongoCollection<Cart> _collection;

    public CartObserver(IMongoCollection<Cart> collection)
    {
        _collection = collection;
    }

    public async Task ItemAdded(ItemAddedToCart @event, EventContext context)
    {
        var initial = (await _collection.FindAsync(_ => _.Id == context.EventSourceId)).SingleOrDefault();
        initial ??= new Cart(context.EventSourceId, Array.Empty<CartItem>());
        initial = initial with
        {
            Items = initial.Items?.Append(new CartItem(@event.MaterialId, @event.Quantity)) ??
                new[] { new CartItem(@event.MaterialId, @event.Quantity) }
        };
        await _collection.ReplaceOneAsync(_ => _.Id == context.EventSourceId, initial, new ReplaceOptions { IsUpsert = true });
    }

    public async Task ItemRemoved(ItemRemovedFromCart @event, EventContext context)
    {
        var initial = (await _collection.FindAsync(_ => _.Id == context.EventSourceId)).SingleOrDefault();
        initial ??= new Cart(context.EventSourceId, Array.Empty<CartItem>());
        initial = initial with
        {
            Items = initial.Items?.Where(_ => _.MaterialId != @event.MaterialId).ToArray() ??
                Array.Empty<CartItem>()
        };
        await _collection.ReplaceOneAsync(_ => _.Id == context.EventSourceId, initial, new ReplaceOptions { IsUpsert = true });
    }

    public async Task QuantityAdjusted(QuantityAdjustedForItemInCart @event, EventContext context)
    {
        var initial = (await _collection.FindAsync(_ => _.Id == context.EventSourceId)).SingleOrDefault();
        initial ??= new Cart(context.EventSourceId, Array.Empty<CartItem>());
        initial = initial with
        {
            Items = initial.Items.Select(item => item.MaterialId == @event.MaterialId ? new CartItem(item.MaterialId, @event.Quantity) : item).ToArray()
        };
        await _collection.ReplaceOneAsync(_ => _.Id == context.EventSourceId, initial, new ReplaceOptions { IsUpsert = true });
    }
}
