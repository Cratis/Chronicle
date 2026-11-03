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

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

/// <summary>
/// Resolves a sink for a registered read model with an index, on top of the real <see cref="IDatabase"/>, without provisioning.
/// </summary>
static class ColdIndexedSink
{
    /// <summary>
    /// Resolves the sink without provisioning and asks it for an occurrence that does not exist.
    /// </summary>
    /// <param name="database">The real database.</param>
    /// <returns>The sink.</returns>
    internal static async Task<ISink> ReadAnOccurrence(IDatabase database)
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
            .AddSingleton<IDatabase>(database)
            .AddSingleton<ReplayingTables>()
            .AddSingleton<ILogger<Sink>>(NullLogger<Sink>.Instance)
            .BuildServiceProvider();
        var factory = new SinkFactory(services, new ExpandoObjectConverter(new TypeFormats()));
        var sinks = new StorageSinks("store", "namespace", new KnownInstancesOf<ISinkFactory>([factory]));

        var sink = await sinks.GetWithoutProvisioning(definition);
        await sink.GetInstances("indexed_model-20240101000000-deadbeef", 0, 10, ReadModelInstanceVisibility.InitializedOnly);
        return sink;
    }
}
