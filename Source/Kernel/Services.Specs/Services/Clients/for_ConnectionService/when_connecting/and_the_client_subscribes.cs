// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using KernelConnectionService = Cratis.Chronicle.Services.Clients.ConnectionService;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

/// <summary>
/// A client does not consider itself connected until the first keep-alive arrives, so withholding that first
/// heartbeat for a whole interval charges every client the interval before it can do anything.
/// </summary>
public class and_the_client_subscribes : Specification
{
    const int KeepAliveIntervalSeconds = 30;

    IConnectionService _connectionService;
    IConnectedClients _connectedClients;
    ConnectionKeepAlive _firstKeepAlive;
    bool _arrived;

    void Establish()
    {
        var silo = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        _connectedClients = Substitute.For<IConnectedClients>();

        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IConnectedClients>(silo.ToParsableString(), Arg.Any<string>()).Returns(_connectedClients);

        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        localSiloDetails.SiloAddress.Returns(silo);

        // Long enough that a heartbeat arriving on the interval could not be mistaken for the immediate one.
        var options = new ChronicleOptions { ConnectedClients = new() { KeepAliveIntervalSeconds = KeepAliveIntervalSeconds } };

        _connectionService = new KernelConnectionService(
            grainFactory,
            localSiloDetails,
            NullLogger<KernelConnectionService>.Instance,
            Options.Create(options));
    }

    async Task Because()
    {
        using var received = new SemaphoreSlim(0, 1);
        using var subscription = _connectionService.Connect(new ConnectRequest { ConnectionId = "the-client" })
            .Subscribe(keepAlive =>
            {
                _firstKeepAlive = keepAlive;
                received.Release();
            });

        _arrived = await received.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_send_a_keep_alive_without_waiting_for_the_interval() => _arrived.ShouldBeTrue();
    [Fact] void should_identify_the_connection_it_belongs_to() => _firstKeepAlive.ConnectionId.ShouldEqual("the-client");
    [Fact] async Task should_register_the_client_first() => await _connectedClients.Received(1).OnClientConnected("the-client", Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
}
