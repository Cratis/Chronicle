// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting_concurrently;

public class and_a_disconnect_handler_awaits_a_connect : given.a_connection_with_a_real_lifecycle_in_flight
{
    Exception _handlerError;
    Task _disconnecting;
    int _resolvesBeforeTheLaterAttempt;

    void Establish() =>
        _real.OnDisconnected += async () =>
        {
            try
            {
                await _connection.Connect();
            }
            catch (Exception ex)
            {
                _handlerError = ex;
            }
        };

    async Task Because()
    {
        _disconnecting = _real.Disconnected();
        await _fenceAwaited.Task;
        _time.Advance(TimeSpan.FromSeconds(ConnectTimeoutSeconds));
        await _disconnecting;
        _resolvesBeforeTheLaterAttempt = Volatile.Read(ref _resolves);

        var later = _connection.Reconnect();
        await HandshakeIsPending();
        _handshake.SetResult();
        _connectedGate.SetResult();
        await later;
    }

    [Fact] void should_fail_the_connect_the_handler_awaited_with_a_timeout() => _handlerError.ShouldBeOfExactType<ConnectionTimedOut>();
    [Fact] void should_let_the_disconnect_complete() => _disconnecting.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_not_resolve_servers_for_the_attempt_that_never_got_past_the_fence() => _resolvesBeforeTheLaterAttempt.ShouldEqual(0);
    [Fact] void should_connect_on_the_next_attempt() => _real.IsConnected.ShouldBeTrue();
}
