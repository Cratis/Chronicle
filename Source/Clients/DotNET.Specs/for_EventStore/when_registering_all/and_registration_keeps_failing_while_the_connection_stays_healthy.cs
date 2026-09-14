// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

/// <summary>
/// The connection watchdog only re-drives registration when the transport's keep-alive goes stale - it never
/// does that again once the transport is healthy. A registration that exhausts its bounded attempts while the
/// transport stays healthy the whole time must therefore keep retrying itself, or some observers stay
/// unsubscribed forever even though nothing else is wrong (#3935).
/// </summary>
public class and_registration_keeps_failing_while_the_connection_stays_healthy : given.an_event_store_with_a_projection_that_cannot_be_built
{
    static readonly Exception _failure = new("the kernel refused the seeding call");

    int _attempts;
    Exception _thrownByFirstCall;

    void Establish()
    {
        _registrationRetry.MaxAttempts = 1;
        _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);
        _eventStore.Seeding.Register().Returns(_ =>
        {
            _attempts++;

            // Fails the first two attempts (the initial call, then one background retry), succeeds from the third.
            return _attempts < 3 ? Task.FromException(_failure) : Task.CompletedTask;
        });
    }

    async Task Because()
    {
        await _projections.Discover();
        _thrownByFirstCall = await Catch.Exception(() => _eventStore.RegisterAll());

        // The caller already has its answer (the throw above) - nobody calls RegisterAll() again from here on.
        // Only the background loop the failure started keeps the kernel calls going.
        await _eventStore.PendingBackgroundRegistrationRetry!;
    }

    [Fact] void should_fail_the_first_call() => _thrownByFirstCall.ShouldEqual(_failure);
    [Fact] void should_keep_trying_without_being_asked_again() => _attempts.ShouldEqual(3);
    [Fact] void should_eventually_report_registration_as_successful() => _eventStore.Registration.IsSuccess.ShouldBeTrue();
}
