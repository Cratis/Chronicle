// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService;

/// <summary>
/// Spec for verifying that the server can forcibly disconnect a client
/// by cancelling the CTS registered with the connection manager.
/// </summary>
public class when_server_disconnects_client : given.a_connection_service
{
    ConnectRequest _request;
    CancellationTokenSource _clientCts;
    CancellationTokenSource? _capturedCts;
    bool _completed;

    void Establish()
    {
        _request = CreateRequest();
        _clientCts = new CancellationTokenSource();

        _connectionManager.When(m => m.Register(Arg.Any<ConnectionId>(), Arg.Any<CancellationTokenSource>()))
            .Do(info => _capturedCts = info.Arg<CancellationTokenSource>());
    }

    async Task Because()
    {
        var observable = _service.Connect(_request, _clientCts.Token);
        observable.Subscribe(
            onNext: _ => { },
            onCompleted: () => _completed = true);

        // Let the connection establish.
        await Task.Delay(TimeSpan.FromSeconds(1.5));

        // Simulate a server-initiated disconnect by cancelling the linked CTS.
        await _capturedCts!.CancelAsync();

        // Allow the finally block to complete.
        await Task.Delay(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    void should_have_captured_a_linked_cts() =>
        _capturedCts.ShouldNotBeNull();

    [Fact]
    void should_complete_the_observable() =>
        _completed.ShouldBeTrue();

    [Fact]
    void should_unregister_connection() =>
        _connectionManager.Received(1).Unregister(
            Arg.Is<ConnectionId>(id => id.Value == _request.ConnectionId));

    [Fact]
    void should_notify_grain_of_disconnection() =>
        _connectedClients.Received(1).OnClientDisconnected(
            _request.ConnectionId,
            "Client disconnected");
}
