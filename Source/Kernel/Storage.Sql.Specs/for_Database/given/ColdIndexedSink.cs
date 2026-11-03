// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
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
    /// The occurrence the specifications read.
    /// </summary>
    internal const string Occurrence = "indexed_model-20240101000000-deadbeef";

    /// <summary>
    /// A schema with a string key and an integer value.
    /// </summary>
    internal const string IntegerValueSchema = """{"type":"object","properties":{"id":{"type":"string"},"value":{"type":"integer"}}}""";

    /// <summary>
    /// A schema whose identifier is spelled <c language="csharp">ID</c>, which earlier releases stored beside a synthesized <c language="csharp">Id</c> key.
    /// </summary>
    internal const string UpperCaseIdentifierSchema = """{"type":"object","properties":{"ID":{"type":"string"},"value":{"type":"integer"}}}""";

    /// <summary>
    /// A schema with a decimal key, which SQLite orders through a shadow column.
    /// </summary>
    internal const string DecimalKeySchema = """{"type":"object","properties":{"id":{"type":"number","format":"decimal"},"value":{"type":"integer"}}}""";

    /// <summary>
    /// Resolves the sink without provisioning and asks it for an occurrence that does not exist.
    /// </summary>
    /// <param name="database">The real database.</param>
    /// <returns>The sink.</returns>
    internal static Task<ISink> ReadAnOccurrence(IDatabase database) =>
        Resolve(database, IntegerValueSchema, "store", "namespace", async sink =>
        {
            await sink.GetInstances(Occurrence, 0, 10, ReadModelInstanceVisibility.InitializedOnly);
            return sink;
        });

    /// <summary>
    /// Reads the occurrence through the sink's list query and its live query.
    /// </summary>
    /// <param name="database">The real database.</param>
    /// <param name="schema">The JSON schema of the read model.</param>
    /// <param name="eventStore">The event store the sink belongs to.</param>
    /// <param name="namespace">The namespace the sink belongs to.</param>
    /// <param name="occurrence">The occurrence to read.</param>
    /// <returns>What the two queries returned.</returns>
    internal static Task<OccurrenceRead> ReadTheOccurrence(IDatabase database, string schema, string eventStore = "store", string @namespace = "namespace", string occurrence = Occurrence) =>
        ReadThe(database, schema, eventStore, @namespace, occurrence);

    /// <summary>
    /// Reads the primary table, which is what a query without an occurrence reads, through the sink's list query and its live query.
    /// </summary>
    /// <param name="database">The real database.</param>
    /// <param name="schema">The JSON schema of the read model.</param>
    /// <param name="eventStore">The event store the sink belongs to.</param>
    /// <param name="namespace">The namespace the sink belongs to.</param>
    /// <returns>What the two queries returned.</returns>
    internal static Task<OccurrenceRead> ReadThePrimaryTable(IDatabase database, string schema, string eventStore, string @namespace) =>
        ReadThe(database, schema, eventStore, @namespace, null);

    static Task<OccurrenceRead> ReadThe(IDatabase database, string schema, string eventStore, string @namespace, ReadModelContainerName? occurrence) =>
        Resolve(database, schema, eventStore, @namespace, async sink =>
        {
            var instances = await sink.GetInstances(occurrence, 0, 10, ReadModelInstanceVisibility.InitializedOnly);
            var observed = await sink.ObserveInstances(occurrence, 0, 10, ReadModelInstanceVisibility.InitializedOnly).Take(1).ToTask().WaitAsync(TimeSpan.FromSeconds(30));
            return new OccurrenceRead(instances.TotalCount, instances.Instances.Count(), observed.Count());
        });

    /// <summary>
    /// Resolves the sink without provisioning and lets <paramref name="read"/> use it, as a cold sink is used.
    /// </summary>
    /// <typeparam name="T">The type of what is read.</typeparam>
    /// <param name="database">The real database.</param>
    /// <param name="schemaJson">The JSON schema of the read model.</param>
    /// <param name="eventStore">The event store the sink belongs to.</param>
    /// <param name="namespace">The namespace the sink belongs to.</param>
    /// <param name="read">What to do with the sink.</param>
    /// <returns>What <paramref name="read"/> returned.</returns>
    internal static Task<T> Cold<T>(IDatabase database, string schemaJson, string eventStore, string @namespace, Func<ISink, Task<T>> read) =>
        Resolve(database, schemaJson, eventStore, @namespace, read);

    static async Task<T> Resolve<T>(IDatabase database, string schemaJson, string eventStore, string @namespace, Func<ISink, Task<T>> read)
    {
        var schema = await JsonSchema.FromJsonAsync(schemaJson);
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
        var sinks = new StorageSinks(eventStore, @namespace, new KnownInstancesOf<ISinkFactory>([factory]));

        return await read(await sinks.GetWithoutProvisioning(definition));
    }
}
