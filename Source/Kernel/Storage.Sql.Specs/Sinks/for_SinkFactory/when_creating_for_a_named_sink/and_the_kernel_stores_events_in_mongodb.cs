// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Sinks;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StorageSinks = Cratis.Chronicle.Storage.Sinks.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SinkFactory.when_creating_for_a_named_sink;

public class and_the_kernel_stores_events_in_mongodb : Specification
{
    string _directory;
    string _reporting;
    ServiceProvider _services;
    StorageSinks _sinks;
    ReadModelDefinition _readModel;
    ISink _sink;
    ReadModelInstances _instances;
    Exception _error;

    async Task Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"chronicle-named-sink-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _reporting = new SqliteConnectionStringBuilder { DataSource = Path.Combine(_directory, "reporting.db"), Pooling = false }.ConnectionString;

        // The kernel keeps its events in MongoDB, so none of the SQL storage services are registered: the named sink has to bring its own.
        var options = new ChronicleOptions
        {
            Storage = new() { Type = StorageType.MongoDB, ConnectionDetails = "mongodb://localhost:27017" },
            Sinks = new()
            {
                Configurations = new Dictionary<string, SinkConfiguration>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Reporting"] = new() { Type = "SQL", ConnectionDetails = _reporting }
                }
            }
        };
        _services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(Options.Create(options))
            .BuildServiceProvider();
        var factory = new SinkFactory(_services, new ExpandoObjectConverter(new TypeFormats()));
        _sinks = new StorageSinks("store", "namespace", new KnownInstancesOf<ISinkFactory>([factory]), new SinkConfigurations(Options.Create(options)));
        _readModel = new(
            "orders",
            "orders",
            "Orders",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "orders",
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB) { Name = "reporting" },
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"total":{"type":"integer"}}}""") },
            []);
    }

    async Task Because()
    {
        _error = await Catch.Exception(async () =>
        {
            _sink = await _sinks.GetFor(_readModel);
            _instances = await _sink.GetInstances(null, 0, 10, ReadModelInstanceVisibility.InitializedOnly);
        });
    }

    async Task Destroy()
    {
        await _services.DisposeAsync();
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort: the temporary directory is removed by the operating system otherwise.
        }
    }

    [Fact] void should_create_the_sink_without_the_kernel_storage() => _error.ShouldBeNull();
    [Fact] void should_store_to_the_sql_sink() => _sink.ShouldBeOfExactType<Sink>();
    [Fact] void should_start_with_an_empty_read_model() => _instances.TotalCount.ShouldEqual(0);
    [Fact] void should_create_the_database_of_the_named_sink() => Directory.EnumerateFiles(_directory, "*.db").ShouldNotBeEmpty();
}
