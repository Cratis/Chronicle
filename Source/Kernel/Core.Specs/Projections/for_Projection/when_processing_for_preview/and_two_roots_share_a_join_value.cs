// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_preview;

public class and_two_roots_share_a_join_value : given.two_orders_joined_to_a_product
{
    IEnumerable<ExpandoObject> _result;

    async Task Because() => _result = await Preview(_events);

    [Fact] void should_have_only_the_orders() => _result.Count().ShouldEqual(2);
    [Fact] void should_join_the_first_order() => ReadModelFor(_result, Order)["ProductName"].ShouldEqual("Electric kettle");
    [Fact] void should_join_the_second_order() => ReadModelFor(_result, SecondOrder)["ProductName"].ShouldEqual("Electric kettle");
    [Fact] void should_apply_the_joined_every_mapping_to_the_first_order() => Convert.ToDateTime(ReadModelFor(_result, Order)["LastUpdated"]).ShouldEqual(_events[2].Context.Occurred.UtcDateTime);
    [Fact] void should_apply_the_joined_every_mapping_to_the_second_order() => Convert.ToDateTime(ReadModelFor(_result, SecondOrder)["LastUpdated"]).ShouldEqual(_events[2].Context.Occurred.UtcDateTime);
}
