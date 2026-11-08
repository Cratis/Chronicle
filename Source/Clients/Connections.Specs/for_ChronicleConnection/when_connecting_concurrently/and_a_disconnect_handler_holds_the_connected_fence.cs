// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting_concurrently;

public class and_a_disconnect_handler_holds_the_connected_fence : given.a_connection_with_a_real_lifecycle_in_flight
{
    Task _starter;
    Exception _starterError;
    Task _disconnecting;

    void Establish() =>
        _real.OnDisconnected += async () =>
        {
            try
            {
                await _starter;
            }
            catch
            {
                // The starter's failure is what the spec observes.
            }
        };

    async Task Because()
    {
        _starter = StartCaller();
        await AttemptHasStarted();
        await HandshakeIsPending();

        _disconnecting = _real.Disconnected();
        _handshake.SetResult();
        await _lifecycleLogger.Fenced;
        _time.Advance(TimeSpan.FromSeconds(ConnectTimeoutSeconds));
        _starterError = await Catch.Exception(() => _starter);
        await _disconnecting;

        _connectedGate.SetResult();
        await _connection.Reconnect();
    }

    [Fact] void should_fail_the_connect_with_a_timeout() => _starterError.ShouldBeOfExactType<ConnectionTimedOut>();
    [Fact] void should_let_the_disconnect_complete() => _disconnecting.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_connect_on_the_next_attempt() => _real.IsConnected.ShouldBeTrue();
}
