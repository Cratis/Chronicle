// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Server;

using System.Diagnostics;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.Extensions.Logging;
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
        if (!lifecycle.IsConnected)
        {
            Connect();
        }
    }

    void Connect()
    {
        _connectionService = new ConnectionService(grainFactory, loggerFactory.CreateLogger<ConnectionService>());
        _connectionService.Connect(new()
        {
            ConnectionId = lifecycle.ConnectionId,
            IsRunningWithDebugger = Debugger.IsAttached,
        }).Subscribe(HandleConnection);
        lifecycle.Connected();
    }

    void HandleConnection(ConnectionKeepAlive keepAlive)
    {
        _connectionService?.ConnectionKeepAlive(keepAlive).GetAwaiter().GetResult();
    }
}
