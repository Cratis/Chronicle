// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Graphs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jGraphSinkFactory.when_creating_for_named_sinks;

public class and_two_graphs_name_different_servers : Specification
{
    readonly List<Uri> _servers = [];
    ServiceProvider _services;
    IGraphSinkFactory _factory;
    IAsyncSession _session;

    void Establish()
    {
        _session = Substitute.For<IAsyncSession>();
        var driver = Substitute.For<IDriver>();
        driver.AsyncSession(Arg.Any<Action<SessionConfigBuilder>>()).Returns(_session);
        var driverFactory = Substitute.For<INeo4jDriverFactory>();
        driverFactory.Create(Arg.Do<Uri>(_servers.Add), Arg.Any<IAuthTokenManager>()).Returns(driver);
        var options = new ChronicleOptions
        {
            Neo4j = new() { Server = "bolt://kernel-default:7687" },
            Sinks = new()
            {
                Configurations = new Dictionary<string, SinkConfiguration>(StringComparer.OrdinalIgnoreCase)
                {
                    ["people"] = new() { Type = "Neo4j", Server = "bolt://people-graph:7687" },
                    ["organization"] = new() { Type = "Neo4j", Server = "bolt://organization-graph:7687", DatabaseMode = Neo4jDatabaseMode.Single }
                }
            }
        };
        _services = new ServiceCollection()
            .AddSingleton(Options.Create(options))
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddSingleton(driverFactory)
            .BuildServiceProvider();
        _factory = new Neo4jGraphSinkFactory(_services);
    }

    async Task Because()
    {
        await _factory.CreateGraphFor("Orders", "acme", Graph("People"), "people").ApplyGraphChanges(Changes(), 1);
        await _factory.CreateGraphFor("Orders", "acme", Graph("Organization"), "organization").ApplyGraphChanges(Changes(), 1);
    }

    Task Destroy() => _services.DisposeAsync().AsTask();

    [Fact] void should_connect_the_first_graph_to_its_server() => _servers.ShouldContain(new Uri("bolt://people-graph:7687"));
    [Fact] void should_connect_the_second_graph_to_its_server() => _servers.ShouldContain(new Uri("bolt://organization-graph:7687"));
    [Fact] void should_not_connect_to_the_kernels_default_server() => _servers.ShouldNotContain(new Uri("bolt://kernel-default:7687"));

    static GraphChangeset Changes() => new("owner", [new DeleteEdge(new EdgeReference("MEMBER_OF", new NodeReference("Person", "p1"), new NodeReference("Group", "g1")))]);

    static ReadModelDefinition Graph(string name) => new(
        name,
        name,
        name,
        ReadModelOwner.Client,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        name,
        new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.Neo4j),
        new Dictionary<ReadModelGeneration, Schemas.JsonSchema> { [ReadModelGeneration.First] = GraphDefinitions.Organization.ToSchema() },
        []);
}
