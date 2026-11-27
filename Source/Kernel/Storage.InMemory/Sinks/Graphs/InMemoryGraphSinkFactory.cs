// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs;

/// <summary>
/// Represents an implementation of <see cref="IGraphSinkFactory"/> for <see cref="InMemoryGraphSink"/>.
/// </summary>
/// <param name="cache">The silo's shared physical provenance cache.</param>
public class InMemoryGraphSinkFactory(ISinkProvenanceCache? cache = null) : IGraphSinkFactory
{
    static readonly ConditionalWeakTable<ISinkProvenanceCache, ConcurrentDictionary<(EventStoreName, EventStoreNamespaceName), InMemorySinkProvenanceStorage>> _scopes = [];
    readonly ISinkProvenanceCache _cache = cache ?? new SinkProvenanceCache();

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.InMemoryGraph;

    /// <inheritdoc/>
    public ISinkProvenanceStorage GetProvenance(EventStoreName eventStore, EventStoreNamespaceName @namespace, SinkName sink, ReadModelContainerName? containerName = null) =>
        sink.IsDefault
            ? _cache.Bind((typeof(InMemoryGraphSinkFactory), eventStore, @namespace), () => StateFor(eventStore, @namespace))
            : throw new SinkTypeDoesNotSupportNamedConfigurations(TypeId, sink);

    /// <inheritdoc/>
    public IGraphSink CreateGraphFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel)
    {
        StateFor(eventStore, @namespace);
        return new InMemoryGraphSink(Concepts.Graphs.GraphDefinition.FromSchema(readModel.GetSchemaForLatestGeneration()));
    }

    InMemorySinkProvenanceStorage StateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        _scopes.GetOrCreateValue(_cache).GetOrAdd((eventStore, @namespace), _ => new());
}
