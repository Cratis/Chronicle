// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.when_applying_a_changeset;

public class and_it_refers_to_an_unknown_label : given.a_graph_sink
{
    Exception? _exception;
    GraphNode? _person;

    async Task Because()
    {
        _exception = await Catch.Exception(() => Apply(
            1,
            new UpsertNode(Person("p1"), Props(("name", "Ada"))),
            new UpsertNode(new("Robot", "r1"), Props())));
        _person = await NodeOf(Person("p1"));
    }

    [Fact] void should_refuse_the_changeset() => _exception.ShouldBeOfExactType<UnknownGraphElement>();
    [Fact] void should_undo_the_operations_before_the_failure() => _person.ShouldBeNull();
}
