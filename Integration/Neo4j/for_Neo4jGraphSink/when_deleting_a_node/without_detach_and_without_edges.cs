// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_deleting_a_node.without_detach_and_without_edges.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_deleting_a_node;

/// <summary>
/// Deleting a node that has no edges deletes it, with or without detach.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class without_detach_and_without_edges(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public IReadOnlyDictionary<string, object>? Stored;
        public GraphNode? Found;

        async Task Because()
        {
            await _run.Apply(1, new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "Ada"))));
            await _run.Apply(2, new DeleteNode(Person("p1"), false));
            Stored = await StoredPerson("p1");
            Found = await _sink.FindNode(Owner, Person("p1"));
        }
    }

    [Fact] void should_delete_the_node() => Context.Stored.ShouldBeNull();
    [Fact] void should_not_find_the_node() => Context.Found.ShouldBeNull();
}
