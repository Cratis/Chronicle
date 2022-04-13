// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Aksio.Cratis.Events.Store.Grains.Connections;

/// <summary>
/// Represents an implementation of <see cref="IConnectedClients"/>.
/// </summary>
public class ConnectedClients : Grain, IConnectedClients
{
    readonly ConcurrentDictionary<string, List<IConnectedClientObserver>> _observers = new();
    readonly ISiloStatusOracle _siloStatusOracle;
    readonly ILogger<ConnectedClients> _logger;
    string _lastConnectedClient = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedClients"/> class.
    /// </summary>
    /// <param name="siloStatusOracle"><see cref="ISiloStatusOracle"/> for details on currently running silo.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ConnectedClients(ISiloStatusOracle siloStatusOracle, ILogger<ConnectedClients> logger)
    {
        _siloStatusOracle = siloStatusOracle;
        _logger = logger;

        // TODO: (the plan)
        // - Clients connecting are connected for the silo they are connecting to
        // - If silo goes down, during OnActivateAsync() we will get members of the cluster and remove all connected clients for silos that are no longer with us
        // - Subscribe to silo changes (SubscribeToSiloStatusEvents)
        // - Update GetLastConnectedClientConnectionId() for connected clients to be for specific silo - client needs to know which silo it is connected to.
        Console.WriteLine(_siloStatusOracle);
    }

    /// <inheritdoc/>
    public Task OnClientConnected(string connectionId)
    {
        _lastConnectedClient = connectionId;
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
                observer.Disconnected(connectionId);
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

    /// <inheritdoc/>
    public Task<string> GetLastConnectedClientConnectionId()
    {
        return Task.FromResult(_lastConnectedClient);
    }
}
