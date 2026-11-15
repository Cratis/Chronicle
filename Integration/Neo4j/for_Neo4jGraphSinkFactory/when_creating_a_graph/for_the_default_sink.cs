// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.Neo4j;
using Cratis.Chronicle.Storage.Neo4j.Integration;
using Microsoft.Extensions.DependencyInjection;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSinkFactory.when_creating_a_graph.for_the_default_sink.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSinkFactory.when_creating_a_graph;

/// <summary>
/// The factory creates a sink for the graph a read model declares, on the connections the services hold, and the sink works against a real server.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class for_the_default_sink(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph(fixture)
    {
        public IGraphSink Created = default!;
        public IReadOnlyDictionary<string, object>? Stored;
        public GraphNode? Found;

        async Task Because()
        {
            await using var services = new ServiceCollection().AddSingleton<INeo4jConnections>(Connections).BuildServiceProvider();
            Track(Namespace);
            Created = new Neo4jGraphSinkFactory(services).CreateGraphFor(EventStore, Namespace, Graph("people"));
            await Created.EnsureSchema();
            await new GraphScenarioRun(Created).Apply(1, new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "Ada"))));
            Stored = await StoredPerson("p1");
            Found = await Created.FindNode(Owner, Person("p1"));
        }

        static ReadModelDefinition Graph(string name) => new(
            name,
            name,
            name,
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            name,
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.Neo4j),
            new Dictionary<ReadModelGeneration, Cratis.Chronicle.Schemas.JsonSchema> { [ReadModelGeneration.First] = GraphDefinitions.Organization.ToSchema() },
            []);
    }

    [Fact] void should_create_a_neo4j_graph_sink() => Context.Created.ShouldBeOfExactType<Neo4jGraphSink>();
    [Fact] void should_be_of_the_neo4j_sink_type() => Context.Created.TypeId.ShouldEqual(WellKnownSinkTypes.Neo4j);
    [Fact] void should_write_what_the_read_model_declares_to_the_server() => Context.Stored!["name"].ShouldEqual("Ada");
    [Fact] void should_read_it_back_through_the_sink() => Context.Found.ShouldNotBeNull();
}
