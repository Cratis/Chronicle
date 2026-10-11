// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_a_single_read_model;

public class and_an_order_line_is_removed : given.an_order_projection_grain_with_lines_joined_to_products
{
    AppendedEvent[] _events;
    ExpandoObject _result;

    async Task Establish() => _events =
    [
        await OrderPlaced("Ada"),
        await ItemAddedToOrder(Kettle, 1),
        await ItemAddedToOrder(Mug, 2),
        await ItemRemovedFromOrder(Mug)
    ];

    async Task Because() => _result = await _grain.ProcessForSingleReadModel(EventStoreNamespaceName.Default, new ExpandoObject(), _events);

    [Fact] void should_keep_only_the_remaining_line() => LinesOf(_result).Select(_ => _["ProductId"]?.ToString()).ShouldContainOnly(Kettle);
}
