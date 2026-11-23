// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Coordinates conservative inventory and uncached physical database eligibility checks.
/// </summary>
/// <param name="storage">The authoritative definition and sink storage.</param>
[Singleton]
public class ReadModelSinkProvenance(IStorage storage) : IReadModelSinkProvenance
{
    readonly ConcurrentDictionary<EventStoreName, ReadModelProvenance> _eventStores = new();

    /// <inheritdoc/>
    public Task Preserve(EventStoreName eventStore) => For(eventStore).Preserve();

    /// <inheritdoc/>
    public Task EnsureReadable(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition definition) =>
        For(eventStore).EnsureReadable(@namespace, definition);

    ReadModelProvenance For(EventStoreName eventStore) => _eventStores.GetOrAdd(eventStore, static (name, storage) => new(storage, name), storage);
}
