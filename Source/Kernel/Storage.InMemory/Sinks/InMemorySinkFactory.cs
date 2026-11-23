// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/> for <see cref="InMemorySink"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemorySinkFactory"/> class.
/// </remarks>
/// <param name="typeFormats">The <see cref="ITypeFormats"/> for resolving actual types from JSON schema.</param>
public class InMemorySinkFactory(ITypeFormats typeFormats) : ISinkFactory
{
    readonly ConcurrentDictionary<(EventStoreName, EventStoreNamespaceName), InMemorySinkProvenanceStorage> _provenance = new();

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.InMemory;

    /// <inheritdoc/>
    public ISinkProvenanceStorage GetProvenance(EventStoreName eventStore, EventStoreNamespaceName @namespace, SinkName sink, ReadModelContainerName? containerName = null) =>
        sink.IsDefault ? _provenance.GetOrAdd((eventStore, @namespace), _ => new()) : throw new SinkTypeDoesNotSupportNamedConfigurations(TypeId, sink);

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel) => new InMemorySink(readModel, typeFormats, GetProvenance(eventStore, @namespace, SinkName.Default));
}
