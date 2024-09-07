// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Basic;

public class OrderStateReduce // : IReducerFor<OrderState>
{
    public Task<OrderState> ItemAdded(ItemAddedToCart @event, OrderState? initial, EventContext context)
    {
        initial ??= new OrderState(0, []);
        initial = initial with { Items = initial.Items + 1 };
        return Task.FromResult(initial);
    }
}
