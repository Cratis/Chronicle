// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_preview;

public class and_the_second_order_adjusts_a_shared_product : given.two_orders_with_the_same_product_line
{
    IEnumerable<ExpandoObject> _result;

    async Task Establish() => _events = [.. _events, await Append(SecondOrder, "ItemQuantityAdjusted", new { ProductId = Guid.Parse(Kettle), Quantity = 3 })];

    async Task Because() => _result = await Preview(_events);

    [Fact] void should_keep_one_line_in_the_second_order() => LinesOf(ReadModelFor(_result, SecondOrder)).Length.ShouldEqual(1);
    [Fact] void should_adjust_the_second_order_line() => Convert.ToInt32(LinesOf(ReadModelFor(_result, SecondOrder))[0]["Quantity"]).ShouldEqual(3);
    [Fact] void should_leave_the_first_order_line_unchanged() => Convert.ToInt32(LinesOf(ReadModelFor(_result, Order))[0]["Quantity"]).ShouldEqual(1);
}
