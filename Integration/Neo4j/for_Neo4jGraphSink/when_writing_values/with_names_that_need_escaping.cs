// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.Neo4j;
using Neo4j.Driver;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_writing_values.with_names_that_need_escaping.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_writing_values;

/// <summary>
/// A label, a type or a property name with backticks in it is escaped in the statements, and ends up in the database as it was named.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class with_names_that_need_escaping(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph(fixture)
    {
        public IReadOnlyList<GraphNode> Nodes = default!;
        public GraphNode? Found;
        public GraphEdge? FoundEdge;
        public IReadOnlyList<IRecord> Stored = default!;

        async Task Because()
        {
            var definition = new GraphDefinition(
                "Odd",
                [new("We`ird Label", "ke`y", GraphDefinitions.Schema(("ke`y", "string"), ("na`me", "string")))],
                [new("RE`L", "We`ird Label", "We`ird Label", null, GraphDefinitions.Schema(("si`nce", "string")))]);
            var sink = CreateSink(definition);
            await sink.EnsureSchema();
            var first = new NodeReference("We`ird Label", "k1");
            var second = new NodeReference("We`ird Label", "k2");
            var edge = new EdgeReference("RE`L", first, second);
            await sink.ApplyGraphChanges(new GraphChangeset(Owner, [new UpsertNode(first, Props(("na`me", "One"))), new UpsertEdge(edge, Props(("si`nce", "then")))]), 1);
            Nodes = [.. (await sink.GetNodes("We`ird Label")).Elements];
            Found = await sink.FindNode(Owner, first);
            FoundEdge = await sink.FindEdge(Owner, edge);
            Stored = await Read("MATCH (n:`We``ird Label` {`ke``y`: 'k1'}) RETURN n.`na``me` AS name");
        }
    }

    [Fact] void should_create_the_constraint_and_write_the_nodes() => Context.Nodes.Count.ShouldEqual(2);
    [Fact] void should_find_the_node() => Context.Found!.Properties["na`me"].ShouldEqual("One");
    [Fact] void should_find_the_edge() => Context.FoundEdge!.Properties["si`nce"].ShouldEqual("then");
    [Fact] void should_use_the_names_as_they_are_in_the_database() => Context.Stored[0]["name"].As<string>().ShouldEqual("One");
}
