// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_multiple_read_models;

public class and_the_second_order_removes_a_shared_product : given.two_orders_with_the_same_product_line
{
    IEnumerable<ExpandoObject> _result;

    async Task Establish() => _events = [.. _events, await Append(SecondOrder, "ItemRemovedFromOrder", new { ProductId = Guid.Parse(Kettle) })];

    async Task Because() => _result = await _grain.Process(EventStoreNamespaceName.Default, _events);

    [Fact] void should_remove_the_second_order_line() => LinesOf(ReadModelFor(_result, SecondOrder)).ShouldBeEmpty();
    [Fact] void should_keep_the_first_order_line() => LinesOf(ReadModelFor(_result, Order)).Length.ShouldEqual(1);
}
