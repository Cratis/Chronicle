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
    readonly SemaphoreSlim _somethingHappened = new(0);
    int _keepAlivesSent;
    IConnectionService _connectionService;
    IDisposable _subscription;

    void Establish()
    {
        var silo = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        _connectedClients = Substitute.For<IConnectedClients>();
        _connectedClients.WhenForAnyArgs(_ => _.OnClientConnected(default!, default!, default, default, default!, default!, default!)).Do(_ => _somethingHappened.Release());
        _connectedClients.WhenForAnyArgs(_ => _.OnClientRestored(default!, default!, default, default, default!, default!, default!)).Do(_ => _somethingHappened.Release());
        _connectedClients.WhenForAnyArgs(_ => _.IsConnected(default!)).Do(_ => _somethingHappened.Release());

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

    void Destroy()
    {
        _subscription?.Dispose();
        _somethingHappened.Dispose();
    }

    protected void Connect(bool isRunningWithDebugger = false) =>
        _subscription = _connectionService
            .Connect(new ConnectRequest { ConnectionId = TheConnection, IsRunningWithDebugger = isRunningWithDebugger })
            .Subscribe(_ =>
            {
                Interlocked.Increment(ref _keepAlivesSent);
                _somethingHappened.Release();
            });

    protected async Task ConnectAndWaitForRegistration(bool isRunningWithDebugger = false)
    {
        Connect(isRunningWithDebugger);
        await WaitUntil(() => CallsTo(nameof(IConnectedClients.OnClientConnected)) >= 1);
    }

    /// <summary>
    /// Gets how many keep-alives the connection service has sent to the client.
    /// </summary>
    protected int KeepAlivesSent => Volatile.Read(ref _keepAlivesSent);

    protected Task Respond() => _connectionService.ConnectionKeepAlive(new ConnectionKeepAlive { ConnectionId = TheConnection });

    /// <summary>
    /// Waits until <paramref name="condition"/> holds, re-checking it each time the connection service calls the
    /// registry or sends a keep-alive rather than on a clock, and gives up after the patience runs out.
    /// </summary>
    /// <param name="condition">The condition to wait for.</param>
    /// <returns>Awaitable task.</returns>
    protected async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTimeOffset.UtcNow + _patience;
        while (!condition())
        {
            var remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero || !await _somethingHappened.WaitAsync(remaining))
            {
                return;
            }
        }
    }

    protected int CallsTo(string methodName) =>
        _connectedClients.ReceivedCalls().Count(call => call.GetMethodInfo().Name == methodName);

    protected Task WasRestored(int times) =>
        _connectedClients.Received(times).OnClientRestored(new ConnectionId(TheConnection), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());

    protected Task WasRegistered(int times) =>
        _connectedClients.Received(times).OnClientConnected(new ConnectionId(TheConnection), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
}
