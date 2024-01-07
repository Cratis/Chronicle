// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Kernel.Storage;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains;

/// <summary>
/// Represents an implementation of <see cref="IKernel"/>.
/// </summary>
[Singleton]
public class Kernel : IKernel
{
    readonly ConcurrentDictionary<EventStoreName, IEventStore> _eventStores = new();
    readonly IServiceProvider _serviceProvider;
    readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="Kernel"/> class.
    /// </summary>
    /// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting service instances.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public Kernel(
        IStorage storage,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory)
    {
        Storage = storage;
        _serviceProvider = serviceProvider;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    public IStorage Storage { get; }

    /// <inheritdoc/>
    public IEventStore GetEventStore(EventStoreName name)
    {
        if (!_eventStores.TryGetValue(name, out var eventStore))
        {
            eventStore = new EventStore(
                name,
                Storage.GetEventStore(name),
                _serviceProvider,
                _loggerFactory);
            _eventStores[name] = eventStore;
        }

        return eventStore;
    }
}
