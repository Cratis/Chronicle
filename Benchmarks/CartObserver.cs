// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;

namespace Benchmark.Model;

[Observer(Identifier)]
public class CartObserver
{
    public const string Identifier = "744d9b53-1b59-487b-bda2-377680f366cd";

    public Task ItemAdded(ItemAddedToCart @event, EventContext context)
    {
        return Task.CompletedTask;
    }

    public Task ItemRemoved(ItemRemovedFromCart @event, EventContext context)
    {
        return Task.CompletedTask;
    }

    public Task QuantityAdjusted(QuantityAdjustedForItemInCart @event, EventContext context)
    {
        return Task.CompletedTask;
    }
}
