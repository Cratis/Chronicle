// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService;

/// <summary>
/// Spec for verifying that Connect waits on the connection gate
/// and only proceeds once connections are allowed.
/// </summary>
public class when_connections_are_blocked : given.a_connection_service
{
    ConnectRequest _request;
    CancellationTokenSource _clientCts;
    TaskCompletionSource _gateTcs;
    bool _registered;

    void Establish()
    {
        _request = CreateRequest();
        _clientCts = new CancellationTokenSource();
        _gateTcs = new TaskCompletionSource();

        // Block WaitUntilAcceptingConnections until we release the gate.
        _connectionManager.WaitUntilAcceptingConnections().Returns(_gateTcs.Task);

        _connectionManager.When(m => m.Register(Arg.Any<ConnectionId>(), Arg.Any<CancellationTokenSource>()))
            .Do(_ => _registered = true);
    }

    async Task Because()
    {
        _service.Connect(_request, _clientCts.Token);

        // The connection should be waiting on the gate.
        await Task.Delay(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    void should_not_register_while_blocked() =>
        _registered.ShouldBeFalse();

    [Fact]
    void should_not_notify_grain_while_blocked() =>
        _connectedClients.DidNotReceive().OnClientConnected(
            Arg.Any<ConnectionId>(),
            Arg.Any<string>(),
            Arg.Any<bool>());
}
