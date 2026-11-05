// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_upserting_a_node;

public class and_it_exists : given.a_graph_sink
{
    GraphNode? _node;

    async Task Because()
    {
        await Apply(5, new UpsertNode(Person("p1"), Props(("name", "Ada"), ("visits", 1))));
        await Apply(6, new UpsertNode(Person("p1"), Props(("name", "Ada L."), ("visits", null))));
        _node = await NodeOf(Person("p1"));
    }

    [Fact] void should_update_the_property() => _node!.Properties["name"].ShouldEqual("Ada L.");
    [Fact] void should_remove_a_property_set_to_null() => _node!.Properties.ContainsKey("visits").ShouldBeFalse();
    [Fact] void should_advance_the_watermark() => _node!.Watermark.Value.ShouldEqual(6UL);
}
