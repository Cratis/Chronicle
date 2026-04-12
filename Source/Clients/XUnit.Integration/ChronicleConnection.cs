// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using System.Diagnostics;
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
    Task IChronicleConnection.Connect()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Set the services.
    /// </summary>
    /// <param name="services">Services to set.</param>
    internal void SetServices(IServices services) => _services = services;

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
            Debugger.IsAttached);

        _connectionService = new ConnectionService(grainFactory, loggerFactory.CreateLogger<ConnectionService>());
        _connectionService.Connect(new()
        {
            ConnectionId = lifecycle.ConnectionId,
            IsRunningWithDebugger = Debugger.IsAttached,
        }).Subscribe(HandleConnection);

        // Pre-register the client before signaling Connected(). ConnectionService.Connect()
        // registers via a fire-and-forget Task.Run, creating a race where lifecycle.Connected()
        // triggers observer subscriptions before the client is known to the ConnectedClients grain.
        var connectedClients = grainFactory.GetGrain<IConnectedClients>(0);
        await connectedClients.OnClientConnected(
            new KernelConnectionId(lifecycle.ConnectionId.Value),
            string.Empty,
            Debugger.IsAttached);

        await lifecycle.Connected();
    }

    void HandleConnection(ConnectionKeepAlive keepAlive)
    {
        _connectionService?.ConnectionKeepAlive(keepAlive).GetAwaiter().GetResult();
    }
}
