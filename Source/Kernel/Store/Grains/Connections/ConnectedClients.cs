// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aksio.Cratis.Events.Store.Grains.Connections;

/// <summary>
/// Represents an implementation of <see cref="IConnectedClients"/>.
/// </summary>
public class ConnectedClients : Grain, IConnectedClients
{
    readonly ConcurrentDictionary<string, List<IConnectedClientObserver>> _observers = new();
    readonly ILogger<ConnectedClients> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedClients"/> class.
    /// </summary>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ConnectedClients(ILogger<ConnectedClients> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task OnClientConnected(string connectionId)
    {
        _logger.ClientConnected(connectionId);
        _observers[connectionId] = new List<IConnectedClientObserver>();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnClientDisconnected(string connectionId)
    {
        _logger.ClientDisconnected(connectionId);
        if (_observers.TryGetValue(connectionId, out var observers))
        {
            foreach (var observer in observers)
            {
                observer.Disconnected();
            }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SubscribeOnDisconnected(string connectionId, IConnectedClientObserver observer)
    {
        if (_observers.TryGetValue(connectionId, out var observers))
        {
            observers.Add(observer);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UnsubscribeOnDisconnected(string connectionId, IConnectedClientObserver observer)
    {
        if (_observers.TryGetValue(connectionId, out var observers))
        {
            observers.Remove(observer);
        }

        return Task.CompletedTask;
    }
}
