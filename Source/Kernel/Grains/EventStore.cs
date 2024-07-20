// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Cratis.Chronicle.Grains.Observation.Reducers;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Grains;

/// <summary>
/// Represents an implementation of <see cref="IEventStore"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventStore"/> class.
/// </remarks>
/// <param name="name">Name of the event store.</param>
/// <param name="storage"><see cref="IEventStoreStorage"/> for accessing underlying storage for the specific event store.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting service instances.</param>
public class EventStore(
    EventStoreName name,
    IEventStoreStorage storage,
    IServiceProvider serviceProvider) : IEventStore
{
    readonly ConcurrentDictionary<EventStoreNamespaceName, IEventStoreNamespace> _namespaces = new();

    /// <inheritdoc/>
    public EventStoreName Name { get; } = name;

    /// <inheritdoc/>
    public IEventStoreStorage Storage { get; } = storage;

    /// <inheritdoc/>
    public IReducerPipelineDefinitions ReducerPipelineDefinitions { get; } = new ReducerPipelineDefinitions();

    /// <inheritdoc/>
    public IImmutableList<IEventStoreNamespace> Namespaces { get; private set; } = ImmutableList<IEventStoreNamespace>.Empty;

    /// <inheritdoc/>
    public IEventStoreNamespace GetNamespace(EventStoreNamespaceName @namespace)
    {
        if (!_namespaces.TryGetValue(@namespace, out var namespaceInstance))
        {
            namespaceInstance = new EventStoreNamespace(
                Name,
                @namespace,
                Storage.GetNamespace(@namespace),
                serviceProvider);
            _namespaces[@namespace] = namespaceInstance;
            Namespaces = Namespaces.Add(namespaceInstance);
        }

        return namespaceInstance;
    }
}
