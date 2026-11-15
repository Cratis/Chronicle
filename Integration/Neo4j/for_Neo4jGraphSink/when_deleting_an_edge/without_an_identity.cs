// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_deleting_an_edge.without_an_identity.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_deleting_an_edge;

/// <summary>
/// Deleting an edge deletes the edge and leaves its end nodes.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class without_an_identity(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public IReadOnlyList<IReadOnlyDictionary<string, object>> Edges = default!;
        public IReadOnlyDictionary<string, object>? Person1;
        public IReadOnlyDictionary<string, object>? Group1;
        public GraphEdge? Found;

        async Task Because()
        {
            await _run.Apply(1, new UpsertEdge(MemberOf("p1", "g1"), Props()));
            await _run.Apply(2, new DeleteEdge(MemberOf("p1", "g1")));
            Edges = await StoredEdges("MEMBER_OF");
            Person1 = await StoredPerson("p1");
            Group1 = await StoredGroup("g1");
            Found = await _sink.FindEdge(Owner, MemberOf("p1", "g1"));
        }
    }

    [Fact] void should_delete_the_edge() => Context.Edges.ShouldBeEmpty();
    [Fact] void should_not_find_the_edge() => Context.Found.ShouldBeNull();
    [Fact] void should_keep_the_source_node() => Context.Person1.ShouldNotBeNull();
    [Fact] void should_keep_the_target_node() => Context.Group1.ShouldNotBeNull();
}
