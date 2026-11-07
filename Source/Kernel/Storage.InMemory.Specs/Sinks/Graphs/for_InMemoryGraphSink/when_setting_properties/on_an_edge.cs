// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_setting_properties;

public class on_an_edge : given.a_graph_sink
{
    GraphEdge? _edge;

    async Task Because()
    {
        await Apply(1, new UpsertEdge(MemberOf("p1", "g1"), Props(("since", "2020"))));
        await Apply(2, new SetProperties(MemberOf("p1", "g1"), Props(("since", "2021"))));
        await Apply(1, new SetProperties(MemberOf("p1", "g1"), Props(("since", "1999"))));
        _edge = await EdgeOf(MemberOf("p1", "g1"));
    }

    [Fact] void should_set_the_property() => _edge!.Properties["since"].ShouldEqual("2021");
    [Fact] void should_skip_the_write_below_the_watermark() => _edge!.Watermark.Value.ShouldEqual(2UL);
}
