// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectedClients"/>.
/// </summary>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <param name="meter"><see cref="IMeter{ConnectedClients}"/> for metrics.</param>
public class ConnectedClients(
    ILogger<ConnectedClients> logger,
    [FromKeyedServices(WellKnown.MeterName)] IMeter<ConnectedClients> meter) : Grain, IConnectedClients
{
    readonly List<ConnectedClient> _clients = [];

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        this.RegisterGrainTimer(ReviseConnectedClients, new() { DueTime = TimeSpan.Zero, Period = TimeSpan.FromSeconds(1) });
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnClientConnected(
        ConnectionId connectionId,
        string version,
        bool isRunningWithDebugger)
    {
        logger.ClientConnected(connectionId);

        _clients.Where(_ => _.ConnectionId == connectionId).ToList().ForEach(_ => _clients.Remove(_));
        _clients.Add(new()
        {
            ConnectionId = connectionId,
            Version = version,
            LastSeen = DateTimeOffset.UtcNow,
            IsRunningWithDebugger = isRunningWithDebugger
        });

        meter.ConnectedClients(_clients.Count);

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
        }

        meter.ConnectedClients(_clients.Count);

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
    public Task<bool> IsConnected(ConnectionId connectionId) => Task.FromResult(_clients.Exists(_ => _.ConnectionId == connectionId));

    /// <inheritdoc/>
    public Task<ConnectedClient> GetConnectedClient(ConnectionId connectionId) => Task.FromResult(_clients.First(_ => _.ConnectionId == connectionId));

    async Task ReviseConnectedClients(CancellationToken cancellationToken)
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
