// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

/// <summary>
/// An actual transport disconnect means the connection watchdog is about to own reconnecting - and, once
/// reconnected, registering again via <c>OnConnected</c>. The background retry loop must stand down for that
/// rather than keep racing it, so it stops trying the moment a real disconnect happens instead of only when it
/// succeeds (#3935).
/// </summary>
public class and_the_connection_disconnects_while_retrying_in_the_background : given.an_event_store_with_a_projection_that_cannot_be_built
{
    static readonly Exception _failure = new("the kernel refused the seeding call");

    int _attempts;
    Exception _thrownByFirstCall;

    void Establish()
    {
        _registrationRetry.MaxAttempts = 1;
        _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);
        _eventStore.Seeding.Register().Returns(_ => RegisterAndFail());
    }

    async Task RegisterAndFail()
    {
        _attempts++;

        // Disconnects right after the background loop's first retry attempt, before it could ever succeed.
        // Awaited so every OnDisconnected subscriber - including the one this fix adds - has run before this
        // attempt's failure reaches the retry loop, rather than racing it.
        if (_attempts == 2)
        {
            await _connectionLifecycle.Disconnected();
        }

        throw _failure;
    }

    async Task Because()
    {
        await _projections.Discover();
        _thrownByFirstCall = await Catch.Exception(() => _eventStore.RegisterAll());
        await _eventStore.PendingBackgroundRegistrationRetry!;
    }

    [Fact] void should_fail_the_first_call() => _thrownByFirstCall.ShouldEqual(_failure);
    [Fact] void should_stop_after_the_disconnect_instead_of_retrying_forever() => _attempts.ShouldEqual(2);
}
