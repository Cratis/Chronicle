// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_multiple_read_models;

public class and_a_join_on_the_implicit_identifier_follows_the_root : given.an_order_joined_on_its_identifier
{
    AppendedEvent[] _events;
    IEnumerable<ExpandoObject> _result;

    async Task Establish() => _events = [await OrderPlaced("Ada"), await ProductRenamed(Order, "Electric kettle")];

    async Task Because() => _result = await _grain.Process(EventStoreNamespaceName.Default, _events);

    [Fact] void should_have_only_the_order() => _result.Count().ShouldEqual(1);
    [Fact] void should_join_the_order() => ReadModelFor(_result, Order)["ProductName"].ShouldEqual("Electric kettle");
    [Fact] void should_keep_the_root_mapping() => ReadModelFor(_result, Order)["CustomerName"].ShouldEqual("Ada");
}
