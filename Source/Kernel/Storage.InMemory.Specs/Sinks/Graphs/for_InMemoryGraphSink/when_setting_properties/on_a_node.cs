// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_setting_properties;

public class on_a_node : given.a_graph_sink
{
    GraphNode? _node;
    GraphNode? _missing;

    async Task Because()
    {
        await Apply(1, new UpsertNode(Person("p1"), Props(("name", "Ada"))));
        await Apply(2, new SetProperties(Person("p1"), Props(("visits", 3))), new SetProperties(Person("nobody"), Props(("name", "X"))));
        _node = await NodeOf(Person("p1"));
        _missing = await NodeOf(Person("nobody"));
    }

    [Fact] void should_set_the_property() => _node!.Properties["visits"].ShouldEqual(3L);
    [Fact] void should_keep_the_other_properties() => _node!.Properties["name"].ShouldEqual("Ada");
    [Fact] void should_not_create_a_missing_node() => _missing.ShouldBeNull();
}
