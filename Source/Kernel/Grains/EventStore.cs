// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Types;

namespace Cratis.Chronicle.Grains;

/// <summary>
/// Represents an implementation of <see cref="IEventStore"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventStore"/> class.
/// </remarks>
/// <param name="storage">The <see cref="IStorage"/>.</param>
/// <param name="name">Name of the event store.</param>
/// <param name="objectComparer">The <see cref="IObjectComparer"/>.</param>
/// <param name="sinkFactories"><see cref="IInstancesOf{T}"/> of <see cref="ISinkFactory"/>.</param>
public class EventStore(
    IStorage storage,
    EventStoreName name,
    IObjectComparer objectComparer,
    IInstancesOf<ISinkFactory> sinkFactories) : IEventStore
{
    readonly ConcurrentDictionary<EventStoreNamespaceName, IEventStoreNamespace> _namespaces = new();

    /// <inheritdoc/>
    public EventStoreName Name { get; } = name;

    /// <inheritdoc/>
    public IEventStoreStorage Storage { get; } = storage.GetEventStore(name);

    /// <inheritdoc/>
    public IImmutableList<IEventStoreNamespace> Namespaces { get; private set; } = ImmutableList<IEventStoreNamespace>.Empty;

    /// <inheritdoc/>
    public IEventStoreNamespace GetNamespace(EventStoreNamespaceName @namespace)
    {
        if (!_namespaces.TryGetValue(@namespace, out var namespaceInstance))
        {
            namespaceInstance = new EventStoreNamespace(
                storage,
                Name,
                @namespace,
                objectComparer,
                sinkFactories);
            _namespaces[@namespace] = namespaceInstance;
            Namespaces = Namespaces.Add(namespaceInstance);
        }

        return namespaceInstance;
    }
}
