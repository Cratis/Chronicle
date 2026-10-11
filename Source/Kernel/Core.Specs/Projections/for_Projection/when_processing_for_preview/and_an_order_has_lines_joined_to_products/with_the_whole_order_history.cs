// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_preview.and_an_order_has_lines_joined_to_products;

public class with_the_whole_order_history : given.an_order_projection_grain_with_lines_joined_to_products
{
    AppendedEvent[] _events;
    IEnumerable<ExpandoObject> _result;

    async Task Establish() => _events =
    [
        await ProductRegistered(Kettle, "Kettle"),
        await ProductRegistered(Mug, "Mug"),
        await OrderPlaced("Ada"),
        await ItemAddedToOrder(Kettle, 1),
        await ItemAddedToOrder(Mug, 2),
        await ItemQuantityAdjusted(Kettle, 3),
        await ProductRenamed(Kettle, "Electric kettle"),
        await ItemRemovedFromOrder(Mug),
        await OrderShipped()
    ];

    async Task Because() => _result = await Preview(_events);

    [Fact] void should_only_have_the_order() => _result.Select(_ => ((IDictionary<string, object?>)_)["id"]?.ToString()).ShouldContainOnly(Order);
    [Fact] void should_have_the_customer_name() => ReadModelFor(_result, Order)["CustomerName"].ShouldEqual("Ada");
    [Fact] void should_be_shipped() => ReadModelFor(_result, Order)["IsShipped"].ShouldEqual(true);
    [Fact] void should_count_one_item() => Convert.ToInt32(ReadModelFor(_result, Order)["ItemCount"]).ShouldEqual(1);
    [Fact] void should_have_only_the_kettle_line() => LinesOf(ReadModelFor(_result, Order)).Select(_ => _["ProductId"]?.ToString()).ShouldContainOnly(Kettle);
    [Fact] void should_have_a_single_kettle_line() => LinesOf(ReadModelFor(_result, Order)).Count(_ => _["ProductId"]?.ToString() == Kettle).ShouldEqual(1);
    [Fact] void should_have_the_adjusted_quantity() => Convert.ToInt32(LinesOf(ReadModelFor(_result, Order))[0]["Quantity"]).ShouldEqual(3);
    [Fact] void should_have_the_renamed_product_name() => LinesOf(ReadModelFor(_result, Order))[0]["ProductName"].ShouldEqual("Electric kettle");
}
