// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_upserting_a_node;

public class and_an_older_event_arrives_late : given.a_graph_sink
{
    GraphNode? _node;

    async Task Because()
    {
        await Apply(9, new UpsertNode(Person("p1"), Props(("name", "Newer"))));
        await Apply(4, new UpsertNode(Person("p1"), Props(("name", "Older"))));
        _node = await NodeOf(Person("p1"));
    }

    [Fact] void should_keep_the_newer_value() => _node!.Properties["name"].ShouldEqual("Newer");
    [Fact] void should_keep_the_watermark() => _node!.Watermark.Value.ShouldEqual(9UL);
}
