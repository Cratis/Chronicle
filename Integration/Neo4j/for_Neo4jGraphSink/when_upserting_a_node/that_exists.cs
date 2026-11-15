// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_upserting_a_node.that_exists.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_upserting_a_node;

/// <summary>
/// Upserting a node that exists updates the properties it is given, keeps the others, and moves the watermark.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class that_exists(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public IReadOnlyDictionary<string, object> Stored = default!;
        public long Nodes;

        async Task Because()
        {
            await _run.Apply(1, new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "Ada"), ("visits", 1))));
            await _run.Apply(2, new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "Grace"))));
            Stored = (await StoredPerson("p1"))!;
            Nodes = await CountNodes("Person");
        }
    }

    [Fact] void should_still_have_one_node() => Context.Nodes.ShouldEqual(1L);
    [Fact] void should_update_the_properties_it_is_given() => Context.Stored["name"].ShouldEqual("Grace");
    [Fact] void should_keep_the_properties_it_is_not_given() => Context.Stored["visits"].ShouldEqual(1L);
    [Fact] void should_move_the_watermark() => Context.Stored["_watermark"].ShouldEqual(2L);
}
