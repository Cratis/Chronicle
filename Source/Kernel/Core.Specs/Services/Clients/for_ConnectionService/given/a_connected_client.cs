// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using KernelConnectionService = Cratis.Chronicle.Services.Clients.ConnectionService;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.given;

/// <summary>
/// A client connected through a <see cref="KernelConnectionService"/> with the shortest keep-alive interval, so the
/// connection is checked within a second of connecting.
/// </summary>
public class a_connected_client : Specification
{
    protected const string TheConnection = "the-client";
    static readonly TimeSpan _patience = TimeSpan.FromSeconds(10);

    protected IConnectedClients _connectedClients;
    IConnectionService _connectionService;
    IDisposable _subscription;

    void Establish()
    {
        var silo = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        _connectedClients = Substitute.For<IConnectedClients>();

        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IConnectedClients>(silo.ToParsableString(), Arg.Any<string>()).Returns(_connectedClients);

        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        localSiloDetails.SiloAddress.Returns(silo);

        var options = Options.Create(new ChronicleOptions { ConnectedClients = new() { KeepAliveIntervalSeconds = 1 } });
        _connectionService = new KernelConnectionService(
            grainFactory,
            localSiloDetails,
            new ConnectedClientsQuery(grainFactory, options),
            new OpenConnections(),
            NullLogger<KernelConnectionService>.Instance,
            options);
    }

    void Destroy() => _subscription?.Dispose();

    protected void Connect(bool isRunningWithDebugger = false) =>
        _subscription = _connectionService
            .Connect(new ConnectRequest { ConnectionId = TheConnection, IsRunningWithDebugger = isRunningWithDebugger })
            .Subscribe(_ => { });

    protected async Task ConnectAndWaitForRegistration(bool isRunningWithDebugger = false)
    {
        Connect(isRunningWithDebugger);
        await WaitUntil(() => CallsTo(nameof(IConnectedClients.OnClientConnected)) >= 1);
    }

    protected Task Respond() => _connectionService.ConnectionKeepAlive(new ConnectionKeepAlive { ConnectionId = TheConnection });

    protected async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTimeOffset.UtcNow + _patience;
        while (!condition() && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(20);
        }
    }

    protected int CallsTo(string methodName) =>
        _connectedClients.ReceivedCalls().Count(call => call.GetMethodInfo().Name == methodName);

    protected Task WasRestored(int times) =>
        _connectedClients.Received(times).OnClientRestored(new ConnectionId(TheConnection), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());

    protected Task WasRegistered(int times) =>
        _connectedClients.Received(times).OnClientConnected(new ConnectionId(TheConnection), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
}
