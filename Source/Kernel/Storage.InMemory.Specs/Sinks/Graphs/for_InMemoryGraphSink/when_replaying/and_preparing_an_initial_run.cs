// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_replaying;

public class and_preparing_an_initial_run : given.a_graph_sink
{
    GraphNode? _mine;
    GraphNode? _theirs;

    async Task Because()
    {
        await Apply(1, new UpsertNode(Person("mine"), Props(("name", "Ada"))));
        await Apply(OtherOwner, 2, new UpsertNode(Person("theirs"), Props(("name", "Bob"))));
        await _sink.PrepareInitialRun(Owner);
        _mine = await NodeOf(Person("mine"));
        _theirs = await NodeOf(Person("theirs"));
    }

    [Fact] void should_remove_what_the_projection_wrote() => _mine.ShouldBeNull();
    [Fact] void should_leave_what_other_projections_wrote() => _theirs.ShouldNotBeNull();
}
