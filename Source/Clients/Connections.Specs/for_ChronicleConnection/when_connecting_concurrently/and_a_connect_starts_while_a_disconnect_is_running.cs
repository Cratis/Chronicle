// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting_concurrently;

public class and_a_connect_starts_while_a_disconnect_is_running : given.a_connection_with_a_real_lifecycle_in_flight
{
    readonly TaskCompletionSource _disconnectEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseDisconnect = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ConnectionId _before;
    ConnectionId _seenByConnectedHandlers;

    void Establish()
    {
        _before = _real.ConnectionId;
        _real.OnDisconnected += async () =>
        {
            _disconnectEntered.TrySetResult();
            await _releaseDisconnect.Task;
        };
        _real.OnConnected += () =>
        {
            _seenByConnectedHandlers = _real.ConnectionId;
            return Task.CompletedTask;
        };
    }

    async Task Because()
    {
        var disconnecting = _real.Disconnected();
        await _disconnectEntered.Task;
        var caller = StartCaller();

        // The connect is held at the fence. On code that does not hold it the handshake is reached instead, which ends the wait too.
        await Task.WhenAny(_fenceAwaited.Task, HandshakeIsPending());
        _releaseDisconnect.TrySetResult();
        await HandshakeIsPending();
        _handshake.SetResult();
        _connectedGate.SetResult();
        await Task.WhenAll(disconnecting, caller);
    }

    [Fact] void should_hold_the_connect_behind_the_disconnect() => _fenceAwaited.Task.IsCompleted.ShouldBeTrue();
    [Fact] void should_send_one_handshake() => _handshakeConnectionIds.Count.ShouldEqual(1);
    [Fact] void should_not_send_the_id_of_the_generation_that_was_disconnected() => _handshakeConnectionIds[0].ShouldNotEqual(_before.ToString());
    [Fact] void should_send_the_id_the_connected_handlers_see() => _handshakeConnectionIds[0].ShouldEqual(_seenByConnectedHandlers.ToString());
}
