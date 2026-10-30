// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Storage;
using Cratis.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectedClients"/>.
/// </summary>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <param name="meter"><see cref="IMeter{ConnectedClients}"/> for metrics.</param>
/// <param name="options"><see cref="IOptions{ChronicleOptions}"/> for configuration.</param>
/// <param name="storage">The infrastructure storage for disconnected leases.</param>
[KeepAlive]
[ConnectedClientsPlacement]
public partial class ConnectedClients(
    ILogger<ConnectedClients> logger,
    [FromKeyedServices(WellKnown.MeterName)] IMeter<ConnectedClients> meter,
    IOptions<ChronicleOptions> options,
    IStorage storage) : Grain, IConnectedClients
{
    readonly TimeSpan _reviseConnectedClientsPeriod = TimeSpan.FromSeconds(options.Value.ConnectedClients.ReviseIntervalSeconds);
    readonly TimeSpan _reservationTtl = TimeSpan.FromSeconds(options.Value.ConnectedClients.ReservationTtlSeconds);
    readonly int _staleThresholdSeconds = options.Value.ConnectedClients.StaleThresholdSeconds;
    readonly Dictionary<ConnectionId, ConnectionEntry> _clients = [];
    readonly List<DateTimeOffset> _reservations = [];
    IGrainTimer? _reviseConnectedClientsTimer;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // A registry that is activated again - for instance after a directory failure - knows no client, while the
        // connections open on this silo are still there. Put back the ones that are still live.
        var localConnections = ServiceProvider.GetService<ILocalClientConnections>();
        var localSiloDetails = ServiceProvider.GetService<ILocalSiloDetails>();
        if (localConnections is not null && localSiloDetails is not null)
        {
            var keyedForThisSilo = this.GetPrimaryKeyString() == localSiloDetails.SiloAddress.ToParsableString();
            var live = localConnections.GetLive(DateTimeOffset.UtcNow, KeepAliveLapse.From(options.Value.ConnectedClients));
            await SeedLiveConnections(live, keyedForThisSilo);
            logger.LocalConnectionStoreRead(RuntimeIdentity, this.GetPrimaryKeyString(), localConnections.StoreId, keyedForThisSilo, _clients.Count);
        }

        _reviseConnectedClientsTimer = this.RegisterGrainTimer(ReviseConnectedClients, new() { DueTime = TimeSpan.Zero, Period = _reviseConnectedClientsPeriod });
        logger.Activated(RuntimeIdentity);
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
        string machineName,
        string clientType)
    {
        logger.ClientConnected(connectionId);

        _clients[connectionId] = CreateEntry(connectionId, version, isRunningWithDebugger, processId, processPath, machineName, clientType);

        // The real connection just registered - if it was preceded by a reservation, that
        // reservation has now been fulfilled. Clear the oldest one rather than leave it to expire
        // on its own, so GetConnectionCount() doesn't double-count this client for up to
        // _reservationTtl after it already connected.
        if (_reservations.Count > 0)
        {
            _reservations.RemoveAt(0);
        }

        meter.ConnectedClients(_clients.Count);
        logger.ClientConnectedOnSilo(RuntimeIdentity, connectionId, _clients.Count);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnClientRestored(
        ConnectionId connectionId,
        string version,
        bool isRunningWithDebugger,
        int processId,
        string processPath,
        string machineName,
        string clientType)
    {
        if (_clients.ContainsKey(connectionId))
        {
            return Task.CompletedTask;
        }

        logger.ClientRestored(connectionId);
        _clients[connectionId] = CreateEntry(connectionId, version, isRunningWithDebugger, processId, processPath, machineName, clientType);
        meter.ConnectedClients(_clients.Count);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnClientDisconnected(ConnectionId connectionId, string reason)
    {
        logger.ClientDisconnected(connectionId, reason);

        _clients.Remove(connectionId);

        meter.ConnectedClients(_clients.Count);
        logger.ClientDisconnectedOnSilo(RuntimeIdentity, connectionId, reason, _clients.Count);

        // Expiry is best effort and must not hold up connection bookkeeping. Leases also expire
        // by kernel time, so a failed scope only delays redelivery until its lease deadline.
        var now = DateTimeOffset.UtcNow;
        _ = Task.Run(() => OccurrenceRecovery.ExpireLeases(storage, connectionId.Value, now, logger));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> OnClientPing(ConnectionId connectionId)
    {
        if (_clients.TryGetValue(connectionId, out var entry))
        {
            entry.Client.LastSeen = DateTimeOffset.UtcNow;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ConnectedClient>> GetAllConnectedClients() => Task.FromResult(_clients.Values.Select(entry => entry.Client));

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
    public Task<bool> IsConnected(ConnectionId connectionId) => Task.FromResult(_clients.ContainsKey(connectionId));

    /// <inheritdoc/>
    public Task<ConnectedClient> GetConnectedClient(ConnectionId connectionId)
    {
        var entry = _clients.GetValueOrDefault(connectionId) ?? throw new ClientIsNotConnected(connectionId);
        return Task.FromResult(entry.Client);
    }

    /// <summary>
    /// Registers the live connections open on the silo this registry is keyed for, skipping the ones already known.
    /// </summary>
    /// <param name="connections">The <see cref="LiveClientConnection"/>s that are open and responding on the local silo.</param>
    /// <param name="keyedForThisSilo">Whether this registry is the one for the local silo. A registry placed here as a fallback for a dead silo is not.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task SeedLiveConnections(IEnumerable<LiveClientConnection> connections, bool keyedForThisSilo)
    {
        if (!keyedForThisSilo)
        {
            return;
        }

        var count = 0;
        foreach (var connection in connections.Where(connection => !_clients.ContainsKey(connection.ConnectionId)))
        {
            await OnClientRestored(
                connection.ConnectionId,
                connection.Version,
                connection.IsRunningWithDebugger,
                connection.ProcessId,
                connection.ProcessPath,
                connection.MachineName,
                connection.ClientType);
            count++;
        }

        logger.SeededLiveConnections(RuntimeIdentity, count);
    }

    static ConnectionEntry CreateEntry(
        ConnectionId connectionId,
        string version,
        bool isRunningWithDebugger,
        int processId,
        string processPath,
        string machineName,
        string clientType) => new(new()
        {
            ConnectionId = connectionId,
            Version = version,
            LastSeen = DateTimeOffset.UtcNow,
            IsRunningWithDebugger = isRunningWithDebugger,
            ProcessId = processId,
            ProcessPath = processPath,
            MachineName = machineName,
            ClientType = clientType
        });

    async Task ReviseConnectedClients(CancellationToken cancellationToken)
    {
        RemoveExpiredReservations();

        if (Debugger.IsAttached) return;

        foreach (var connectedClient in _clients.Values.Select(entry => entry.Client).ToArray())
        {
            if (connectedClient.IsRunningWithDebugger) continue;

            if (connectedClient.LastSeen < DateTimeOffset.UtcNow.AddSeconds(-_staleThresholdSeconds))
            {
                await OnClientDisconnected(connectedClient.ConnectionId, $"Last seen was more than {_staleThresholdSeconds} seconds ago");
            }
        }
    }

    void RemoveExpiredReservations()
    {
        var cutoff = DateTimeOffset.UtcNow - _reservationTtl;
        _reservations.RemoveAll(reservedAt => reservedAt < cutoff);
    }
}
