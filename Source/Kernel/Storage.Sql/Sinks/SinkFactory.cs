// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Creates SQL sinks without resolving an inactive storage backend during construction.
/// </summary>
/// <param name="serviceProvider">The backend service provider.</param>
/// <param name="expandoObjectConverter">The schema-aware converter.</param>
public class SinkFactory(IServiceProvider serviceProvider, IExpandoObjectConverter expandoObjectConverter) : ISinkFactory
{
    readonly Lazy<NamedSinkDatabases> _namedDatabases = new(() => new(serviceProvider));
    readonly Lazy<ISinkProvenanceCache> _cache = new(() => serviceProvider.GetService<ISinkProvenanceCache>() ?? new SinkProvenanceCache());
    readonly ConcurrentDictionary<(EventStoreName, EventStoreNamespaceName, SinkName, ReadModelContainerName?), (SinkProvenanceStorage Raw, ISinkProvenanceStorage Cached)> _provenance = new();

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.SQL;

    /// <inheritdoc/>
    public ISinkProvenanceStorage GetProvenance(EventStoreName eventStore, EventStoreNamespaceName @namespace, SinkName sink, ReadModelContainerName? containerName = null) =>
        Provenance(eventStore, @namespace, sink, containerName).Cached;

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel)
    {
        var database = serviceProvider.GetRequiredService<IDatabase>();
        var replayingTables = serviceProvider.GetRequiredService<ReplayingTables>();
        return Bind(new Sink(eventStore, @namespace, readModel, database, expandoObjectConverter, replayingTables, serviceProvider.GetRequiredService<ILogger<Sink>>()), eventStore, @namespace, SinkName.Default, readModel.ContainerName);
    }

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel, SinkName sink)
    {
        if (sink.IsDefault) return CreateFor(eventStore, @namespace, readModel);
        var (database, replayingTables) = _namedDatabases.Value.Get(sink);
        return Bind(new Sink(eventStore, @namespace, readModel, database, expandoObjectConverter, replayingTables, serviceProvider.GetRequiredService<ILogger<Sink>>()), eventStore, @namespace, sink, readModel.ContainerName);
    }

    Sink Bind(Sink instance, EventStoreName eventStore, EventStoreNamespaceName @namespace, SinkName sink, ReadModelContainerName container)
    {
        var provenance = Provenance(eventStore, @namespace, sink, container);
        instance.BindProvenance(provenance.Cached, provenance.Raw);
        return instance;
    }

    (SinkProvenanceStorage Raw, ISinkProvenanceStorage Cached) Provenance(EventStoreName eventStore, EventStoreNamespaceName @namespace, SinkName sink, ReadModelContainerName? container) =>
        _provenance.GetOrAdd((eventStore, @namespace, sink, container), key =>
        {
            var database = sink.IsDefault ? serviceProvider.GetRequiredService<IDatabase>() : _namedDatabases.Value.Get(sink).Database;
            var raw = new SinkProvenanceStorage(database, eventStore, @namespace, container);
            return (raw, _cache.Value.Bind((typeof(SinkFactory), key), () => raw));
        });
}
