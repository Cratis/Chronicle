// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Integration;
using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.Neo4j;
using Cratis.Chronicle.Storage.Neo4j.Integration;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSinkFactory.when_creating_a_graph.for_a_named_sink.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSinkFactory.when_creating_a_graph;

/// <summary>
/// A named sink has its own server and credentials, read from its own configuration, and the graph it creates writes there.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class for_a_named_sink(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph(fixture)
    {
        public IGraphSink Created = default!;
        public IReadOnlyDictionary<string, object>? Stored;
        public Exception? NotNeo4j;
        public Exception? Unknown;

        async Task Because()
        {
            var options = new Cratis.Chronicle.Configuration.ChronicleOptions
            {
                Sinks = new()
                {
                    Configurations = new Dictionary<string, SinkConfiguration>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["people"] = new()
                        {
                            Type = "Neo4j",
                            Server = Server.HostUri,
                            UserName = Neo4jGraphServer.UserName,
                            PasswordSecret = PasswordKey,
                            DatabaseMode = Fixture.DatabaseMode
                        },
                        ["documents"] = new() { Type = "MongoDB" }
                    }
                }
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { [PasswordKey] = Server.Password }).Build();
            await using var services = new ServiceCollection()
                .AddSingleton(Options.Create(options))
                .AddSingleton<IConfiguration>(configuration)
                .AddSingleton<INeo4jDriverFactory>(new Neo4jDriverFactory())
                .BuildServiceProvider();
            var factory = new Neo4jGraphSinkFactory(services);
            Track(Namespace);
            Created = factory.CreateGraphFor(EventStore, Namespace, Graph("people"), "people");
            await Created.EnsureSchema();
            await new GraphScenarioRun(Created).Apply(1, new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "Named"))));
            Stored = await StoredPerson("p1");
            NotNeo4j = Catch.Exception(() => _ = factory.CreateGraphFor(EventStore, Namespace, Graph("people"), "documents"));
            Unknown = Catch.Exception(() => _ = factory.CreateGraphFor(EventStore, Namespace, Graph("people"), "nobody"));
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

    [Fact] void should_connect_the_graph_to_the_server_the_sink_names() => Context.Stored!["name"].ShouldEqual("Named");
    [Fact] void should_refuse_a_sink_that_is_not_a_neo4j_sink() => Context.NotNeo4j.ShouldBeOfExactType<SinkTypeDoesNotSupportNamedConfigurations>();
    [Fact] void should_refuse_a_sink_that_is_not_configured() => Context.Unknown.ShouldBeOfExactType<SinkTypeDoesNotSupportNamedConfigurations>();
}
