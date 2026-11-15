// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_upserting_a_node;

public class and_it_does_not_exist : given.a_graph_sink
{
    GraphNode? _node;

    async Task Because()
    {
        await Apply(5, new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "Ada"))));
        _node = await NodeOf(Person("p1"));
    }

    [Fact] void should_create_the_node() => _node.ShouldNotBeNull();
    [Fact] void should_set_the_properties() => _node!.Properties["name"].ShouldEqual("Ada");
    [Fact] void should_store_the_sequence_number_as_watermark() => _node!.Watermark.Value.ShouldEqual(5UL);
}
