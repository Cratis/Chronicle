// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectedClients"/>.
/// </summary>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <param name="meter"><see cref="IMeter{ConnectedClients}"/> for metrics.</param>
[KeepAlive]
[ConnectedClientsPlacement]
public class ConnectedClients(
    ILogger<ConnectedClients> logger,
    [FromKeyedServices(WellKnown.MeterName)] IMeter<ConnectedClients> meter) : Grain, IConnectedClients
{
    static readonly TimeSpan _reviseConnectedClientsPeriod = TimeSpan.FromSeconds(2);
    static readonly TimeSpan _reservationTtl = TimeSpan.FromSeconds(30);
    readonly List<ConnectedClient> _clients = [];
    readonly List<DateTimeOffset> _reservations = [];
    IGrainTimer? _reviseConnectedClientsTimer;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _reviseConnectedClientsTimer = this.RegisterGrainTimer(ReviseConnectedClients, new() { DueTime = TimeSpan.Zero, Period = _reviseConnectedClientsPeriod });
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _reviseConnectedClientsTimer?.Dispose();
        _reviseConnectedClientsTimer = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnClientConnected(
        ConnectionId connectionId,
        string version,
        bool isRunningWithDebugger,
        int processId,
        string processPath,
        string machineName)
    {
        logger.ClientConnected(connectionId);

        _clients.Where(_ => _.ConnectionId == connectionId).ToList().ForEach(_ => _clients.Remove(_));
        _clients.Add(new()
        {
            ConnectionId = connectionId,
            Version = version,
            LastSeen = DateTimeOffset.UtcNow,
            IsRunningWithDebugger = isRunningWithDebugger,
            ProcessId = processId,
            ProcessPath = processPath,
            MachineName = machineName
        });

        // The real connection just registered - if it was preceded by a reservation, that
        // reservation has now been fulfilled. Clear the oldest one rather than leave it to expire
        // on its own, so GetConnectionCount() doesn't double-count this client for up to
        // _reservationTtl after it already connected.
        if (_reservations.Count > 0)
        {
            _reservations.RemoveAt(0);
        }

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
    public Task<int> GetConnectionCount()
    {
        RemoveExpiredReservations();
        return Task.FromResult(_clients.Count + _reservations.Count);
    }

    /// <inheritdoc/>
    public Task ReserveConnection()
    {
        RemoveExpiredReservations();
        _reservations.Add(DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> IsConnected(ConnectionId connectionId) => Task.FromResult(_clients.Exists(_ => _.ConnectionId == connectionId));

    /// <inheritdoc/>
    public Task<ConnectedClient> GetConnectedClient(ConnectionId connectionId)
    {
        var connectedClient = _clients.Find(_ => _.ConnectionId == connectionId) ?? throw new ClientIsNotConnected(connectionId);
        return Task.FromResult(connectedClient);
    }

    async Task ReviseConnectedClients(CancellationToken cancellationToken)
    {
        RemoveExpiredReservations();

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

    void RemoveExpiredReservations()
    {
        var cutoff = DateTimeOffset.UtcNow - _reservationTtl;
        _reservations.RemoveAll(reservedAt => reservedAt < cutoff);
    }
}
