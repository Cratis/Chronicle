// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StorageSinks = Cratis.Chronicle.Storage.Sinks.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_reading_without_provisioning;

public class and_a_registered_occurrence_is_resolved_on_a_cold_indexed_sink : given.a_database_without_read_model_storage
{
    ISink _sink;

    async Task Because()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"value":{"type":"integer"}}}""");
        var definition = new ReadModelDefinition(
            "indexed",
            "indexed_model",
            "Indexed",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "indexed",
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.SQL),
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
            [new IndexDefinition("value")]);

        await using var services = new ServiceCollection()
            .AddSingleton<IDatabase>(_database)
            .AddSingleton<ReplayingTables>()
            .AddSingleton<ILogger<Sink>>(NullLogger<Sink>.Instance)
            .BuildServiceProvider();
        var factory = new SinkFactory(services, new ExpandoObjectConverter(new TypeFormats()));
        var sinks = new StorageSinks("store", "namespace", new KnownInstancesOf<ISinkFactory>([factory]));

        _sink = await sinks.GetWithoutProvisioning(definition);
        await _sink.GetInstances("indexed_model-20240101000000-deadbeef", 0, 10, ReadModelInstanceVisibility.InitializedOnly);
    }

    [Fact] void should_resolve_the_sink() => _sink.ShouldNotBeNull();
    [Fact] void should_not_create_any_database_file() => string.Join(',', FilesOnDisk).ShouldEqual("cluster.db");
}
