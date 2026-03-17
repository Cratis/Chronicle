// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Tasks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientConnectionManager"/>.
/// </summary>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
internal sealed class ClientConnectionManager(ILogger<ClientConnectionManager> logger) : IClientConnectionManager
{
    readonly ConcurrentDictionary<ConnectionId, CancellationTokenSource> _connections = new();
    readonly AsyncManualResetEvent _gate = CreateOpenGate();

    /// <inheritdoc/>
    public void Register(ConnectionId connectionId, CancellationTokenSource cancellationTokenSource)
    {
        _connections[connectionId] = cancellationTokenSource;
        logger.ConnectionRegistered(connectionId);
    }

    /// <inheritdoc/>
    public void Unregister(ConnectionId connectionId)
    {
        _connections.TryRemove(connectionId, out _);
        logger.ConnectionUnregistered(connectionId);
    }

    /// <inheritdoc/>
    public void DisconnectAll(string reason)
    {
        logger.DisconnectingAllClients(reason, _connections.Count);

        foreach (var (_, cts) in _connections)
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The CTS was already disposed by a normal disconnect — safe to ignore.
            }
        }

        _connections.Clear();
    }

    /// <inheritdoc/>
    public void BlockConnections()
    {
        logger.BlockingNewConnections();
        _gate.Reset();
    }

    /// <inheritdoc/>
    public void AllowConnections()
    {
        logger.AllowingNewConnections();
        _gate.Set();
    }

    /// <inheritdoc/>
    public Task WaitUntilAcceptingConnections() => _gate.WaitAsync();

    static AsyncManualResetEvent CreateOpenGate()
    {
        var gate = new AsyncManualResetEvent();
        gate.Set();
        return gate;
    }
}
