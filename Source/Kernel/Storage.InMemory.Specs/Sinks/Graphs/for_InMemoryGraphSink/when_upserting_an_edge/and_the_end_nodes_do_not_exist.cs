// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_upserting_an_edge;

public class and_the_end_nodes_do_not_exist : given.a_graph_sink
{
    GraphEdge? _edge;
    GraphNode? _source;
    GraphNode? _target;

    async Task Because()
    {
        await Apply(3, new UpsertEdge(MemberOf("p1", "g1"), Props(("since", "2020"))));
        _edge = await EdgeOf(MemberOf("p1", "g1"));
        _source = await NodeOf(Person("p1"));
        _target = await NodeOf(Group("g1"));
    }

    [Fact] void should_create_the_edge() => _edge!.Properties["since"].ShouldEqual("2020");
    [Fact] void should_create_the_source_node_without_properties() => _source!.Properties.ShouldBeEmpty();
    [Fact] void should_create_the_target_node_without_properties() => _target!.Properties.ShouldBeEmpty();
    [Fact] void should_not_give_the_created_nodes_a_watermark() => _source!.Watermark.IsUnavailable.ShouldBeTrue();
}
