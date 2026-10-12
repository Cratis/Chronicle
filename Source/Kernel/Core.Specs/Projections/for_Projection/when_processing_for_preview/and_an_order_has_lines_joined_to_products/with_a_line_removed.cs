// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_preview.and_an_order_has_lines_joined_to_products;

public class with_a_line_removed : given.an_order_projection_grain_with_lines_joined_to_products
{
    AppendedEvent[] _events;
    IEnumerable<ExpandoObject> _result;

    async Task Establish() => _events =
    [
        await OrderPlaced("Ada"),
        await ItemAddedToOrder(Kettle, 1),
        await ItemAddedToOrder(Mug, 2),
        await ItemRemovedFromOrder(Mug)
    ];

    async Task Because() => _result = await Preview(_events);

    [Fact] void should_keep_only_the_remaining_line() => LinesOf(ReadModelFor(_result, Order)).Select(_ => _["ProductId"]?.ToString()).ShouldContainOnly(Kettle);
    [Fact] void should_count_the_remaining_line() => Convert.ToInt32(ReadModelFor(_result, Order)["ItemCount"]).ShouldEqual(1);
}
