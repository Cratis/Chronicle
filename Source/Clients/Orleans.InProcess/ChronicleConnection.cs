// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Server;

using System.Diagnostics;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;
using Server::Cratis.Chronicle.Services.Clients;

namespace Cratis.Chronicle.Orleans.InProcess;

/// <summary>
/// Represents an implementation of <see cref="IChronicleConnection"/> for Orleans in-process.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ChronicleConnection"/> class.
/// </remarks>
/// <param name="lifecycle"><see cref="IConnectionLifecycle"/> for managing lifecycle.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for working with grains.</param>
public class ChronicleConnection(
    IConnectionLifecycle lifecycle,
    IGrainFactory grainFactory) : IChronicleConnection, IChronicleServicesAccessor
{
    IServices? _services;
    ConnectionService? _connectionService;

    /// <inheritdoc/>
    public IConnectionLifecycle Lifecycle { get; } = lifecycle;

    /// <inheritdoc/>
    IServices IChronicleServicesAccessor.Services
    {
        get
        {
            ConnectIfNotConnected();
            return _services!;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <summary>
    /// Set the services.
    /// </summary>
    /// <param name="services">Services to set.</param>
    internal void SetServices(IServices services) => _services = services;

    void ConnectIfNotConnected()
    {
        if (!Lifecycle.IsConnected)
        {
            Connect();
        }
    }

    void Connect()
    {
        _connectionService = new ConnectionService(grainFactory);
        _connectionService.Connect(new()
        {
            ConnectionId = Lifecycle.ConnectionId,
            IsRunningWithDebugger = Debugger.IsAttached,
        }).Subscribe(HandleConnection);
        Lifecycle.Connected();
    }

    void HandleConnection(ConnectionKeepAlive keepAlive)
    {
        _connectionService?.ConnectionKeepAlive(keepAlive).GetAwaiter().GetResult();
    }
}
