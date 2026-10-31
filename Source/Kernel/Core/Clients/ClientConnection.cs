// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Microsoft.Extensions.Logging;
using Orleans.Placement;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientConnection"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for reaching subscribers.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[PreferLocalPlacement]
[KeepAlive]
public class ClientConnection(IGrainFactory grainFactory, ILogger<ClientConnection> logger) : Grain, IClientConnection
{
    readonly HashSet<GrainId> _subscribers = [];
    ConnectionState _state = ConnectionState.Unknown;
    ClientConnectionEpoch _epoch = ClientConnectionEpoch.New();

    enum ConnectionState
    {
        Unknown = 0,
        Connected = 1,
        Disconnected = 2
    }

    /// <inheritdoc/>
    public Task Connect()
    {
        if (_state == ConnectionState.Disconnected)
        {
            _epoch = ClientConnectionEpoch.New();
        }

        _state = ConnectionState.Connected;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Disconnect()
    {
        if (_state == ConnectionState.Disconnected)
        {
            return;
        }

        var subscribers = _subscribers.ToArray();
        _subscribers.Clear();
        _state = ConnectionState.Disconnected;

        var connectionId = GrainContext?.GrainId.Key.ToString() ?? string.Empty;
        logger.Disconnected(connectionId, subscribers.Length);

        foreach (var subscriber in subscribers)
        {
            try
            {
                await grainFactory.GetGrain<IClientDisconnectionSubscriber>(subscriber).ClientDisconnected(_epoch);
            }
            catch (Exception ex)
            {
                logger.FailedNotifyingSubscriber(ex, connectionId, subscriber.ToString());
            }
        }

        // Subscribers have been notified and cleared, nothing more to keep in memory.
        Release();
    }

    /// <inheritdoc/>
    public Task<ClientConnectionEpoch> Subscribe(GrainId subscriber)
    {
        if (_state == ConnectionState.Disconnected)
        {
            return Task.FromResult(ClientConnectionEpoch.NotSet);
        }

        _subscribers.Add(subscriber);
        return Task.FromResult(_epoch);
    }

    /// <inheritdoc/>
    public Task Unsubscribe(GrainId subscriber)
    {
        _subscribers.Remove(subscriber);

        if (_state == ConnectionState.Disconnected && _subscribers.Count == 0)
        {
            Release();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Releases the activation. Virtual so the deactivation request can be observed without an Orleans runtime.
    /// </summary>
    protected virtual void Release() => DeactivateOnIdle();
}
