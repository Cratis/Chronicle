// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;
using stock = Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.given;

namespace Cratis.Chronicle.Integration.for_Reducers.when_reading_with_protection.given;

[Reducer(isActive: false)]
public class StockReducer : IReducerFor<Stock>
{
    public Stock Set(stock.StockSet input, Stock? current, EventContext context) =>
        new(input.Quantity, context.EventType.Generation.Value, (current?.Handled ?? 0) + 1);
    public Stock? Remove(stock.StockRemoved input, Stock? current) => null;
}
