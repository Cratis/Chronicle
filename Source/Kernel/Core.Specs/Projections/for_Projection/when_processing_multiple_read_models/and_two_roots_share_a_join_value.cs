// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_multiple_read_models;

public class and_two_roots_share_a_join_value : given.two_orders_joined_to_a_product
{
    IEnumerable<ExpandoObject> _result;

    async Task Because() => _result = await _grain.Process(EventStoreNamespaceName.Default, _events);

    [Fact] void should_have_only_the_orders() => _result.Count().ShouldEqual(2);
    [Fact] void should_join_the_first_order() => ReadModelFor(_result, Order)["ProductName"].ShouldEqual("Electric kettle");
    [Fact] void should_join_the_second_order() => ReadModelFor(_result, SecondOrder)["ProductName"].ShouldEqual("Electric kettle");
    [Fact] void should_not_apply_direct_properties_to_the_first_order() => DateTimeOffset.Parse(ReadModelFor(_result, Order)["LastUpdated"]!.ToString()!).ShouldEqual(_events[0].Context.Occurred);
    [Fact] void should_not_apply_direct_properties_to_the_second_order() => DateTimeOffset.Parse(ReadModelFor(_result, SecondOrder)["LastUpdated"]!.ToString()!).ShouldEqual(_events[1].Context.Occurred);
}
