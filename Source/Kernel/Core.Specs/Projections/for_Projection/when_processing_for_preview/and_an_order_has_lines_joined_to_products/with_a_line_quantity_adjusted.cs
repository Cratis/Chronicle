// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_preview.and_an_order_has_lines_joined_to_products;

public class with_a_line_quantity_adjusted : given.an_order_projection_grain_with_lines_joined_to_products
{
    AppendedEvent[] _events;
    IEnumerable<ExpandoObject> _result;

    async Task Establish() => _events =
    [
        await OrderPlaced("Ada"),
        await ItemAddedToOrder(Kettle, 1),
        await ItemQuantityAdjusted(Kettle, 3)
    ];

    async Task Because() => _result = await Preview(_events);

    [Fact] void should_have_one_line() => LinesOf(ReadModelFor(_result, Order)).Length.ShouldEqual(1);
    [Fact] void should_update_the_quantity_of_the_line() => Convert.ToInt32(LinesOf(ReadModelFor(_result, Order))[0]["Quantity"]).ShouldEqual(3);
}
