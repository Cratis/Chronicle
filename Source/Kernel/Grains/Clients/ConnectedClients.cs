// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Connections;
using Cratis.Kernel.Orleans.Observers;
using Microsoft.Extensions.Logging;

namespace Cratis.Kernel.Grains.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectedClients"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConnectedClients"/> class.
/// </remarks>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <param name="metricsFactory"><see cref="IConnectedClientsMetricsFactory"/> for creating metrics.</param>
public class ConnectedClients(
    ILogger<ConnectedClients> logger,
    IConnectedClientsMetricsFactory metricsFactory) : Grain, IConnectedClients
{
    /// <summary>
    /// Gets the name of the HTTP client for connected clients.
    /// </summary>
    public const string ConnectedClientsHttpClient = "connected-clients";

    readonly List<ConnectedClient> _clients = [];
    readonly ObserverManager<INotifyClientDisconnected> _clientDisconnectedObservers = new(TimeSpan.FromMinutes(1), logger, "ClientDisconnectedObservers");
    IConnectedClientsMetrics? _metrics;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _metrics = metricsFactory.Create();
        RegisterTimer(ReviseConnectedClients, null!, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnClientConnected(
        ConnectionId connectionId,
        string version,
        bool isRunningWithDebugger)
    {
        var microserviceId = (MicroserviceId)this.GetPrimaryKey();

        logger.ClientConnected(connectionId);

        _clients.Where(_ => _.ConnectionId == connectionId).ToList().ForEach(_ => _clients.Remove(_));
        _clients.Add(new ConnectedClient
        {
            ConnectionId = connectionId,
            Version = version,
            LastSeen = DateTimeOffset.UtcNow,
            IsRunningWithDebugger = isRunningWithDebugger
        });
        _metrics?.SetConnectedClients(_clients.Count);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnClientDisconnected(ConnectionId connectionId, string reason)
    {
        logger.ClientDisconnected(connectionId, reason);

        var client = _clients.Find(_ => _.ConnectionId == connectionId);
        if (client is not null)
        {
            _clients.Remove(client);
            _clientDisconnectedObservers.Notify(_ => _.OnClientDisconnected(client));
        }
        _metrics?.SetConnectedClients(_clients.Count);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> OnClientPing(ConnectionId connectionId)
    {
        var client = _clients.Find(_ => _.ConnectionId == connectionId);
        if (client is not null)
        {
            client.LastSeen = DateTimeOffset.UtcNow;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ConnectedClient>> GetAllConnectedClients() => Task.FromResult(_clients.AsEnumerable());

    /// <inheritdoc/>
    public Task SubscribeDisconnected(INotifyClientDisconnected subscriber)
    {
        _clientDisconnectedObservers.Subscribe(subscriber, subscriber);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UnsubscribeDisconnected(INotifyClientDisconnected subscriber)
    {
        _clientDisconnectedObservers.Unsubscribe(subscriber);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> IsConnected(ConnectionId connectionId) => Task.FromResult(_clients.Exists(_ => _.ConnectionId == connectionId));

    /// <inheritdoc/>
    public Task<ConnectedClient> GetConnectedClient(ConnectionId connectionId) => Task.FromResult(_clients.First(_ => _.ConnectionId == connectionId));

    async Task ReviseConnectedClients(object state)
    {
        if (Debugger.IsAttached) return;

        foreach (var connectedClient in _clients.ToArray())
        {
            if (connectedClient.IsRunningWithDebugger) continue;

            if (connectedClient.LastSeen < DateTimeOffset.UtcNow.AddSeconds(-5))
            {
                await OnClientDisconnected(connectedClient.ConnectionId, "Last seen was more than 5 seconds ago");
            }
        }
    }
}
