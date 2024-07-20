// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains;

/// <summary>
/// Represents an implementation of <see cref="IKernel"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Kernel"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting service instances.</param>
[Singleton]
public class Kernel(
    IStorage storage,
    IServiceProvider serviceProvider) : IKernel
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
                name,
                Storage.GetEventStore(name),
                serviceProvider);
            _eventStores[name] = eventStore;
        }

        return eventStore;
    }
}
