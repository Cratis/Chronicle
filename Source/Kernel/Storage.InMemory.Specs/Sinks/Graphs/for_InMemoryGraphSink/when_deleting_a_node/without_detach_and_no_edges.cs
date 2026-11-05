// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_deleting_a_node;

public class without_detach_and_no_edges : given.a_graph_sink
{
    GraphNode? _node;

    async Task Because()
    {
        await Apply(1, new UpsertNode(Person("p1"), Props(("name", "Ada"))));
        await Apply(1, new DeleteNode(Person("p1"), false));
        await Apply(2, new DeleteNode(Person("p1"), false));
        _node = await NodeOf(Person("p1"));
    }

    [Fact] void should_skip_the_delete_at_the_watermark_and_apply_the_later_one() => _node.ShouldBeNull();
}
