// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService;

/// <summary>
/// Spec for verifying that Connect registers the connection and notifies the grain.
/// </summary>
public class when_connecting : given.a_connection_service
{
    ConnectRequest _request;
    IObservable<ConnectionKeepAlive> _observable;
    CancellationTokenSource _clientCts;
    List<ConnectionKeepAlive> _received;
    CancellationTokenSource? _capturedCts;

    void Establish()
    {
        _request = CreateRequest();
        _clientCts = new CancellationTokenSource();
        _received = [];

        _connectionManager.When(m => m.Register(Arg.Any<ConnectionId>(), Arg.Any<CancellationTokenSource>()))
            .Do(info => _capturedCts = info.Arg<CancellationTokenSource>());
    }

    async Task Because()
    {
        _observable = _service.Connect(_request, _clientCts.Token);
        _observable.Subscribe(keepAlive => _received.Add(keepAlive));

        // Let the background loop run long enough to emit at least one keep-alive.
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Cancel the client token to stop the loop.
        await _clientCts.CancelAsync();

        // Allow the finally block to complete.
        await Task.Delay(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    void should_register_connection_with_manager() =>
        _connectionManager.Received(1).Register(
            Arg.Is<ConnectionId>(id => id.Value == _request.ConnectionId),
            Arg.Any<CancellationTokenSource>());

    [Fact]
    void should_notify_grain_of_connection() =>
        _connectedClients.Received(1).OnClientConnected(
            _request.ConnectionId,
            _request.ClientVersion,
            _request.IsRunningWithDebugger);

    [Fact]
    void should_emit_at_least_one_keep_alive() =>
        _received.Count.ShouldBeGreaterThan(0);

    [Fact]
    void should_emit_keep_alive_with_correct_connection_id() =>
        _received.ShouldContain(k => k.ConnectionId == _request.ConnectionId);

    [Fact]
    void should_unregister_connection_after_disconnect() =>
        _connectionManager.Received(1).Unregister(
            Arg.Is<ConnectionId>(id => id.Value == _request.ConnectionId));

    [Fact]
    void should_notify_grain_of_disconnection() =>
        _connectedClients.Received(1).OnClientDisconnected(
            _request.ConnectionId,
            "Client disconnected");
}
