// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_multiple_read_models;

public class and_a_root_join_matches_a_context_mapping : given.an_order_joined_on_a_context_property
{
    AppendedEvent[] _events;
    IEnumerable<ExpandoObject> _result;

    async Task Establish() => _events = [await OrderPlaced("Ada"), await ProductRenamed(Order, "Electric kettle")];

    async Task Because() => _result = await _grain.Process(EventStoreNamespaceName.Default, _events);

    [Fact] void should_have_only_the_order() => _result.Count().ShouldEqual(1);
    [Fact] void should_join_the_order() => ReadModelFor(_result, Order)["ProductName"].ShouldEqual("Electric kettle");
    [Fact] void should_keep_the_typed_context_mapping() => ReadModelFor(_result, Order)["ProductId"].ShouldEqual(Guid.Parse(Order));
    [Fact] void should_apply_the_joined_every_mapping() => Convert.ToDateTime(ReadModelFor(_result, Order)["LastUpdated"]).ShouldEqual(_events[1].Context.Occurred.UtcDateTime);
}
