// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_preview.and_an_order_has_lines_joined_to_products;

public class with_the_product_renamed_after_the_line : given.an_order_projection_grain_with_lines_joined_to_products
{
    AppendedEvent[] _events;
    IEnumerable<ExpandoObject> _result;

    async Task Establish() => _events =
    [
        await ProductRegistered(Kettle, "Kettle"),
        await OrderPlaced("Ada"),
        await ItemAddedToOrder(Kettle, 1),
        await ProductRenamed(Kettle, "Electric kettle")
    ];

    async Task Because() => _result = await Preview(_events);

    [Fact] void should_only_have_the_order() => _result.Select(_ => ((IDictionary<string, object?>)_)["id"]?.ToString()).ShouldContainOnly(Order);
    [Fact] void should_join_the_new_product_name_onto_the_line() => LinesOf(ReadModelFor(_result, Order))[0]["ProductName"].ShouldEqual("Electric kettle");
}
