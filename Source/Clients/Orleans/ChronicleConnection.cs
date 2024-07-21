// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Server;
extern alias Client;

using System.Diagnostics;
using Client::Cratis.Chronicle;
using Client::Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;
using Server::Cratis.Chronicle.Services.Clients;

namespace Orleans.Hosting;

/// <summary>
/// Represents an implementation of <see cref="IChronicleConnection"/> for Orleans in-process.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ChronicleConnection"/> class.
/// </remarks>
/// <param name="lifecycle"><see cref="IConnectionLifecycle"/> for managing lifecycle.</param>
/// <param name="services"><see cref="IServices"/> to use.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for working with grains.</param>
public class ChronicleConnection(
    IConnectionLifecycle lifecycle,
    IServices services,
    IGrainFactory grainFactory) : IChronicleConnection
{
    IConnectionService? _connectionService;

    /// <inheritdoc/>
    public IConnectionLifecycle Lifecycle { get; } = lifecycle;

    /// <inheritdoc/>
    public IServices Services
    {
        get
        {
            ConnectIfNotConnected();
            return services;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }

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
    }

    void HandleConnection(ConnectionKeepAlive keepAlive)
    {
        _connectionService?.ConnectionKeepAlive(keepAlive).GetAwaiter().GetResult();
    }
}
