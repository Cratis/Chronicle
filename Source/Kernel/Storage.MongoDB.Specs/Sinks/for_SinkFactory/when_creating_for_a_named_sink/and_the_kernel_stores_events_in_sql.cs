// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Sinks;
using Cratis.Chronicle.Storage.Diagnostics;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StorageSinks = Cratis.Chronicle.Storage.Sinks.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkFactory.when_creating_for_a_named_sink;

[Collection(MongoDBCollection.Name)]
public class and_the_kernel_stores_events_in_sql(MongoDBFixture fixture) : Specification
{
    ServiceProvider _services;
    StorageSinks _sinks;
    ReadModelDefinition _readModel;
    ISink _sink;
    ReadModelInstances _instances;
    Exception _error;

    async Task Establish()
    {
        // The kernel keeps its events in SQL, so none of the MongoDB storage services are registered: the named sink has to bring its own.
        var options = new ChronicleOptions
        {
            Storage = new() { Type = StorageType.Sqlite, ConnectionDetails = "Data Source=chronicle.db" },
            Sinks = new()
            {
                Configurations = new Dictionary<string, SinkConfiguration>(StringComparer.OrdinalIgnoreCase)
                {
                    ["documents"] = new() { Type = "MongoDB", ConnectionDetails = fixture.ConnectionString }
                }
            }
        };
        _services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(Options.Create(options))
            .AddSingleton<IStorageAuthorizationDiagnostics, StorageAuthorizationDiagnostics>()
            .BuildServiceProvider();
        var factory = new SinkFactory(_services, new TypeFormats());
        _sinks = new StorageSinks($"named-sink-{Guid.NewGuid():N}", "namespace", new KnownInstancesOf<ISinkFactory>([factory]), new SinkConfigurations(Options.Create(options)));
        _readModel = new(
            "orders",
            "orders",
            "Orders",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "orders",
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.SQL) { Name = "documents" },
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

    Task Destroy() => _services.DisposeAsync().AsTask();

    [Fact] void should_create_the_sink_without_the_kernel_storage() => _error.ShouldBeNull();
    [Fact] void should_store_to_the_mongodb_sink() => _sink.ShouldBeOfExactType<Sink>();
    [Fact] void should_start_with_an_empty_read_model() => _instances.TotalCount.ShouldEqual(0);
}
