// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_upserting_a_node.with_a_label_that_is_not_in_the_definition.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_upserting_a_node;

/// <summary>
/// A label that the definition of the graph does not know is refused, and the changeset it was part of is not written.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class with_a_label_that_is_not_in_the_definition(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public Exception? Error;
        public long Nodes;

        async Task Because()
        {
            Error = await Catch.Exception(() => _run.Apply(1, new UpsertNode(Person("p1"), Props(("personId", "p1"))), new UpsertNode(new("Robot", "r1"), Props())));
            Nodes = await CountNodes("Person");
        }
    }

    [Fact] void should_refuse_the_label() => Context.Error.ShouldBeOfExactType<UnknownGraphElement>();
    [Fact] void should_write_nothing_of_the_changeset() => Context.Nodes.ShouldEqual(0L);
}
