// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
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
/// <remarks>
/// Factories are resolved by concrete type and are transient, while the provenance cache is silo-wide: the raw route a cached
/// handle admits through belongs to that handle, not to the factory that happened to ask for it.
/// </remarks>
/// <param name="serviceProvider">The backend service provider.</param>
/// <param name="expandoObjectConverter">The schema-aware converter.</param>
public class SinkFactory(IServiceProvider serviceProvider, IExpandoObjectConverter expandoObjectConverter) : ISinkFactory
{
    static readonly ConditionalWeakTable<ISinkProvenanceStorage, SinkProvenanceStorage> _admitted = [];
    static readonly Lock _pairing = new();

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
        _provenance.GetOrAdd((eventStore, @namespace, sink, container), static (key, factory) => factory.CreateProvenance(key), this);

    (SinkProvenanceStorage Raw, ISinkProvenanceStorage Cached) CreateProvenance((EventStoreName Store, EventStoreNamespaceName Namespace, SinkName Sink, ReadModelContainerName? Container) key)
    {
        var database = key.Sink.IsDefault ? serviceProvider.GetRequiredService<IDatabase>() : _namedDatabases.Value.Get(key.Sink).Database;
        var cache = _cache.Value;
        lock (_pairing)
        {
            SinkProvenanceStorage? created = null;
            var cached = cache.Bind((typeof(SinkFactory), key), () => created = new SinkProvenanceStorage(database, key.Store, key.Namespace, key.Container));
            if (created is not null) _admitted.AddOrUpdate(cached, created);

            // A handle bound by another factory keeps the raw route it captures the actual target on; never pair it with an unadmitted one.
            return _admitted.TryGetValue(cached, out var raw) ? (raw, cached) : throw new InvalidOperationException("Read-model storage is not available.");
        }
    }
}
