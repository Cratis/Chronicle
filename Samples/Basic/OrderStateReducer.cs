// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Basic;

[Reducer("5027a520-6d25-47c7-9d52-b1e9f82905d2")]
public class OrderStateReducer : IReducerFor<OrderState>
{
    public Task<OrderState> ItemAdded(ItemAddedToCart @event, OrderState? initial, EventContext context)
    {
        initial ??= new OrderState(0, []);
        initial = initial with { Items = initial.Items + 1 };
        return Task.FromResult(initial);
    }
}
