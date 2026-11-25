// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Supervision;
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
/// <param name="departureNotifier">The <see cref="IClientDepartureNotifier"/> telling observers a connection was dropped.</param>
/// <param name="supervisor">The optional <see cref="ISiloSupervisor"/> that revises the clients on a schedule.</param>
[KeepAlive]
[ConnectedClientsPlacement]
public partial class ConnectedClients(
    ILogger<ConnectedClients> logger,
    [FromKeyedServices(WellKnown.MeterName)] IMeter<ConnectedClients> meter,
    IOptions<ChronicleOptions> options,
    IStorage storage,
    IClientDepartureNotifier departureNotifier,
    ISiloSupervisor? supervisor = null) : Grain, IConnectedClients
{
    readonly ConnectedClientsMetrics _metrics = new(meter, options.Value.Telemetry.MetricNames);
    readonly TimeSpan _reviseConnectedClientsPeriod = TimeSpan.FromSeconds(Math.Max(options.Value.ConnectedClients.ReviseIntervalSeconds, options.Value.ConnectedClients.StaleThresholdSeconds));
    readonly TimeSpan _reservationTtl = TimeSpan.FromSeconds(options.Value.ConnectedClients.ReservationTtlSeconds);
    readonly int _staleThresholdSeconds = options.Value.ConnectedClients.StaleThresholdSeconds;
    readonly Dictionary<ConnectionId, ConnectionEntry> _clients = [];
    readonly List<DateTimeOffset> _reservations = [];
    string _supervisionKey = string.Empty;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // A client that closes its connection is reported at once. Revising is only the safety net for one that
        // vanished without it, so it needs no finer clock than the stale threshold, and one supervisor loop per silo
        // runs it rather than a grain timer of its own.
        _supervisionKey = $"connected-clients:{this.GetPrimaryKeyString()}";
        var self = GrainFactory.GetGrain<IConnectedClients>(this.GetPrimaryKeyString());
        supervisor?.Register(
            _supervisionKey,
            _reviseConnectedClientsPeriod,
            async () =>
            {
                await self.Revise();
                return null;
            },
            null);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        supervisor?.Unregister(_supervisionKey);
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

        var entry = CreateEntry(connectionId, version, isRunningWithDebugger, processId, processPath, machineName, clientType);
        if (_clients.TryGetValue(connectionId, out var existing))
        {
            // Observers may still be served by the connection this one replaces; keep them reachable on departure.
            entry.Observers.UnionWith(existing.Observers);
        }

        _clients[connectionId] = entry;

        // The real connection just registered - if it was preceded by a reservation, that
        // reservation has now been fulfilled. Clear the oldest one rather than leave it to expire
        // on its own, so GetConnectionCount() doesn't double-count this client for up to
        // _reservationTtl after it already connected.
        if (_reservations.Count > 0)
        {
            _reservations.RemoveAt(0);
        }

        _metrics.ConnectedClients(_clients.Count);

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
        _metrics.ConnectedClients(_clients.Count);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnClientDisconnected(ConnectionId connectionId, string reason)
    {
        logger.ClientDisconnected(connectionId, reason);

        var observers = _clients.Remove(connectionId, out var removed) ? removed.Observers.ToArray() : [];

        _metrics.ConnectedClients(_clients.Count);

        // Expiry is best effort and must not hold up connection bookkeeping. Leases also expire
        // by kernel time, so a failed scope only delays redelivery until its lease deadline.
        var now = DateTimeOffset.UtcNow;
        _ = BackgroundFaults.Run(() => OccurrenceRecovery.ExpireLeases(storage, connectionId.Value, now, logger), logger, $"{nameof(ConnectedClients)}.{nameof(OccurrenceRecovery.ExpireLeases)}");

        // Handing over must not hold up the registry either, and an observer that is not told falls back to the watchdog.
        if (observers.Length > 0)
        {
            _ = BackgroundFaults.Run(() => departureNotifier.Notify(connectionId, observers), logger, $"{nameof(ConnectedClients)}.{nameof(IClientDepartureNotifier.Notify)}");
        }

        // An observer released while dormant is not in the set above, or is there only for as long as this activation lives.
        _ = BackgroundFaults.Run(() => departureNotifier.NotifyDormant(connectionId, observers), logger, $"{nameof(ConnectedClients)}.{nameof(IClientDepartureNotifier.NotifyDormant)}");

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> OnClientPing(ConnectionId connectionId)
    {
        if (_clients.TryGetValue(connectionId, out var entry))
        {
            entry.Client.LastSeen = DateTimeOffset.UtcNow;
            entry.HasPinged = true;
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

    /// <inheritdoc/>
    public async Task Revise()
    {
        RemoveExpiredReservations();

        if (Debugger.IsAttached) return;

        foreach (var entry in _clients.Values.ToArray())
        {
            var connectedClient = entry.Client;

            // A debugger client that has pinged is paused under a debugger; one that never pinged has no proof of life.
            if (connectedClient.IsRunningWithDebugger && entry.HasPinged) continue;

            if (connectedClient.LastSeen < DateTimeOffset.UtcNow.AddSeconds(-_staleThresholdSeconds))
            {
                await OnClientDisconnected(connectedClient.ConnectionId, $"Last seen was more than {_staleThresholdSeconds} seconds ago");
            }
        }
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

    void RemoveExpiredReservations()
    {
        var cutoff = DateTimeOffset.UtcNow - _reservationTtl;
        _reservations.RemoveAll(reservedAt => reservedAt < cutoff);
    }
}
