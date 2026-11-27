// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Represents an implementation of <see cref="IGraphSinkFactory"/> for <see cref="Neo4jGraphSink"/>.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> to get the <see cref="INeo4jConnections"/> from.</param>
public class Neo4jGraphSinkFactory(IServiceProvider serviceProvider) : IGraphSinkFactory
{
    static readonly ConditionalWeakTable<ISinkProvenanceStorage, Neo4jSinkProvenanceStorage> _admitted = [];
    static readonly Lock _pairing = new();

    readonly ConcurrentDictionary<string, INeo4jConnections> _namedConnections = new(StringComparer.OrdinalIgnoreCase);
    readonly Lazy<ISinkProvenanceCache> _cache = new(() => serviceProvider.GetService<ISinkProvenanceCache>() ?? new SinkProvenanceCache());

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.Neo4j;

    /// <inheritdoc/>
    public IGraphSink CreateGraphFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel) =>
        CreateGraphFor(eventStore, @namespace, readModel, SinkName.Default);

    /// <inheritdoc/>
    public IGraphSink CreateGraphFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel, SinkName sink)
    {
        var (name, connections) = ConnectionsFor(sink);
        var (_, raw) = Bind(eventStore, @namespace, name, connections);
        return new Neo4jGraphSink(GraphDefinition.FromSchema(readModel.GetSchemaForLatestGeneration()), eventStore, @namespace, connections, raw.Target);
    }

    /// <inheritdoc/>
    public ISinkProvenanceStorage GetProvenance(EventStoreName eventStore, EventStoreNamespaceName @namespace, SinkName sink, ReadModelContainerName? containerName = null)
    {
        var (name, connections) = ConnectionsFor(sink);
        return Bind(eventStore, @namespace, name, connections).Cached;
    }

    /// <summary>
    /// Get the settings of a named Neo4j sink: its own server and authentication, resolved through the kernel's connection resolution.
    /// </summary>
    /// <param name="kernel">The <see cref="ChronicleOptions"/> of the kernel.</param>
    /// <param name="configuration">The <see cref="SinkConfiguration"/> of the sink.</param>
    /// <returns>The <see cref="ChronicleOptions"/> the sink's connections are made with.</returns>
    internal static ChronicleOptions OptionsFor(ChronicleOptions kernel, SinkConfiguration configuration) => new()
    {
        Neo4j = configuration.ToNeo4jOptions(),
        Storage = new() { ConnectionResolution = kernel.Storage.ConnectionResolution }
    };

    (ISinkProvenanceStorage Cached, Neo4jSinkProvenanceStorage Raw) Bind(EventStoreName eventStore, EventStoreNamespaceName scope, SinkName name, INeo4jConnections connections)
    {
        lock (_pairing)
        {
            Neo4jSinkProvenanceStorage? created = null;
            var cached = _cache.Value.Bind(
                (typeof(Neo4jGraphSinkFactory), name, eventStore, scope),
                () => created = new Neo4jSinkProvenanceStorage(new Neo4jGraphTarget(connections, eventStore, scope)));
            if (created is not null) _admitted.AddOrUpdate(cached, created);
            return _admitted.TryGetValue(cached, out var raw) ? (cached, raw) : throw Neo4jGraphTarget.Unavailable();
        }
    }

    (SinkName Name, INeo4jConnections Connections) ConnectionsFor(SinkName sink)
    {
        if (sink.IsDefault) return (SinkName.Default, serviceProvider.GetRequiredService<INeo4jConnections>());

        var options = serviceProvider.GetRequiredService<IOptions<ChronicleOptions>>().Value;
        if (!options.Sinks.TryGet(sink.Value, out var key, out var configuration) ||
            !string.Equals(configuration.Type, WellKnownSinkTypes.Neo4j.Value, StringComparison.OrdinalIgnoreCase))
        {
            throw new SinkTypeDoesNotSupportNamedConfigurations(WellKnownSinkTypes.Neo4j, sink);
        }

        var connections = _namedConnections.GetOrAdd(
            key,
            static (_, state) => new Neo4jConnections(
                Options.Create(OptionsFor(state.Options, state.Configuration)),
                state.ServiceProvider.GetRequiredService<IConfiguration>(),
                state.ServiceProvider.GetRequiredService<INeo4jDriverFactory>(),
                state.ServiceProvider),
            (ServiceProvider: serviceProvider, Options: options, Configuration: configuration));
        return (key, connections);
    }
}
