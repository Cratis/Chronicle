// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.Extensions.Logging;
using ConnectionService = KernelCore::Cratis.Chronicle.Services.Clients.ConnectionService;
using IConnectedClients = KernelCore::Cratis.Chronicle.Clients.IConnectedClients;
using KernelConnectionId = KernelConcepts::Cratis.Chronicle.Concepts.Clients.ConnectionId;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents an implementation of <see cref="IChronicleConnection"/> for Orleans in-process.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ChronicleConnection"/> class.
/// </remarks>
/// <param name="lifecycle"><see cref="IConnectionLifecycle"/> for managing lifecycle.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for working with grains.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
internal class ChronicleConnection(
    IConnectionLifecycle lifecycle,
    IGrainFactory grainFactory,
    ILoggerFactory loggerFactory) : IChronicleConnection, IChronicleServicesAccessor
{
    /// <summary>
    /// In-process test clients share the silo process and cannot network-drop, so they are flagged as
    /// keep-alive-exempt to stop the kernel disconnecting them on the (cross-silo) keep-alive timeout.
    /// </summary>
    const bool KeepAliveExempt = true;

    IServices? _services;
    ConnectionService? _connectionService;

    /// <inheritdoc/>
    IConnectionLifecycle IChronicleConnection.Lifecycle => lifecycle;

    /// <inheritdoc/>
    IServices IChronicleServicesAccessor.Services
    {
        get
        {
            ConnectIfNotConnected().GetAwaiter().GetResult();
            return _services!;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lifecycle.Disconnected().GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    Task IChronicleConnection.Connect() => Connect();

    /// <summary>
    /// Set the services.
    /// </summary>
    /// <param name="services">Services to set.</param>
    internal void SetServices(IServices services) => _services = services;

    /// <summary>
    /// Re-establishes the client connection after a lifecycle disconnect.
    /// Registers with the <c>ConnectedClients</c> grain, re-creates the keep-alive
    /// stream, and signals <see cref="IConnectionLifecycle.Connected"/> which triggers
    /// <c>RegisterAll</c> on the <see cref="IEventStore"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal Task Reconnect() => Connect();

    async Task ConnectIfNotConnected()
    {
        if (!lifecycle.IsConnected)
        {
            await Connect();
        }
    }

    async Task Connect()
    {
        // Register the client connection with the ConnectedClients grain BEFORE
        // signaling connected. The OnConnected callbacks (e.g. RegisterAll) register
        // observers whose grains call GetConnectedClient(). If the client hasn't been
        // registered yet, that call throws ClientIsNotConnected and the observer
        // silently fails to subscribe — causing flaky WaitForState timeouts.
        // The production ChronicleConnection avoids this by waiting for the first
        // keep-alive (which only arrives after OnClientConnected completes).
        var connectedClients = grainFactory.GetGrain<IConnectedClients>(0);
        await connectedClients.OnClientConnected(
            (KernelConnectionId)lifecycle.ConnectionId.Value,
            string.Empty,
            KeepAliveExempt);

        _connectionService = new ConnectionService(grainFactory, loggerFactory.CreateLogger<ConnectionService>());
        _connectionService.Connect(new()
        {
            ConnectionId = lifecycle.ConnectionId,
            IsRunningWithDebugger = KeepAliveExempt,
        }).Subscribe(HandleConnection);

        await lifecycle.Connected();
    }

    void HandleConnection(ConnectionKeepAlive keepAlive) => _ = SendKeepAlive();

    async Task SendKeepAlive()
    {
        // Fire-and-forget and swallow transient failures. The kernel emits keep-alives on a 1s loop and
        // terminates that loop — disconnecting the client — if delivering one throws. Each ping is a
        // cross-silo grain call here, so a single transient rejection or slow call must not propagate back
        // into the emission loop (which the previous synchronous GetResult() did) nor block it.
        try
        {
            var connectionId = (KernelConnectionId)lifecycle.ConnectionId.Value;
            var connectedClients = grainFactory.GetGrain<IConnectedClients>(0);
            var stillConnected = await connectedClients.OnClientPing(connectionId);

            // The ConnectedClients grain keeps its registry in memory only. In a cluster it can reactivate
            // (or migrate silos) and lose that registry, after which OnClientPing reports the client as
            // unknown and observers start failing with "Subscriber is disconnected". Re-register so the
            // grain learns about this still-alive client again and observers stay subscribed. The
            // lifecycle.IsConnected guard is essential: a test that explicitly disconnects via
            // Lifecycle.Disconnected() must stay disconnected — without it this heartbeat would silently
            // resurrect the connection and break reconnect specs.
            if (!stillConnected && lifecycle.IsConnected)
            {
                await connectedClients.OnClientConnected(connectionId, string.Empty, KeepAliveExempt);
            }
        }
        catch
        {
        }
    }
}
