// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.DependencyInjection;
using Cratis.Types;

namespace Cratis.Chronicle.Grains;

/// <summary>
/// Represents an implementation of <see cref="IKernel"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Kernel"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
/// <param name="objectComparer">The <see cref="IObjectComparer"/>.</param>
/// <param name="sinkFactories"><see cref="IInstancesOf{T}"/> of <see cref="ISinkFactory"/>.</param>
[Singleton]
public class Kernel(
    IStorage storage,
    IObjectComparer objectComparer,
    IInstancesOf<ISinkFactory> sinkFactories) : IKernel
{
    readonly ConcurrentDictionary<EventStoreName, IEventStore> _eventStores = new();

    /// <inheritdoc/>
    public IStorage Storage { get; } = storage;

    /// <inheritdoc/>
    public IEventStore GetEventStore(EventStoreName name)
    {
        if (!_eventStores.TryGetValue(name, out var eventStore))
        {
            eventStore = new EventStore(
                Storage,
                name,
                objectComparer,
                sinkFactories);
            _eventStores[name] = eventStore;
        }

        return eventStore;
    }
}
