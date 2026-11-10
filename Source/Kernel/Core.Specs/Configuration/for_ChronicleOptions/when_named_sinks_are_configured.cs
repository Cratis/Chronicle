// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions;

public class when_named_sinks_are_configured : Specification
{
    ServiceProvider _services;
    Exception? _error;
    SinksOptions _sinks;

    void Establish()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Cratis:Chronicle:Storage:Type", "inmemory" },
            { "Cratis:Chronicle:Sinks:ReadModels", "primary" },
            { "Cratis:Chronicle:Sinks:Graphs", "people" },
            { "Cratis:Chronicle:Sinks:Configurations:primary:Type", "MongoDB" },
            { "Cratis:Chronicle:Sinks:Configurations:primary:ConnectionDetails", "mongodb://mongo:27017" },
            { "Cratis:Chronicle:Sinks:Configurations:reporting:Type", "SQL" },
            { "Cratis:Chronicle:Sinks:Configurations:reporting:ConnectionDetails", "Host=pg;Database=reporting" },
            { "Cratis:Chronicle:Sinks:Configurations:people:Type", "Neo4j" },
            { "Cratis:Chronicle:Sinks:Configurations:people:Server", "bolt://neo4j:7687" },
            { "Cratis:Chronicle:Sinks:Configurations:people:DatabaseMode", "Single" }
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        ChronicleOptions.AddConfiguration(services, configuration);
        _services = services.BuildServiceProvider();
    }

    void Because()
    {
        _error = Catch.Exception(() => _services.GetRequiredService<IStartupValidator>().Validate());
        _sinks = _services.GetRequiredService<IOptions<ChronicleOptions>>().Value.Sinks;
    }

    [Fact] void should_accept_startup() => _error.ShouldBeNull();
    [Fact] void should_bind_the_read_model_default() => _sinks.ReadModels.ShouldEqual("primary");
    [Fact] void should_bind_the_graph_default() => _sinks.Graphs.ShouldEqual("people");
    [Fact] void should_bind_every_configuration() => _sinks.Configurations.Count.ShouldEqual(3);
    [Fact] void should_bind_the_neo4j_database_mode() => _sinks.Configurations["people"].DatabaseMode.ShouldEqual(Neo4jDatabaseMode.Single);
    [Fact] void should_find_configurations_without_regard_to_case() => _sinks.TryGet("REPORTING", out _, out _).ShouldBeTrue();

    void Destroy() => _services.Dispose();
}
