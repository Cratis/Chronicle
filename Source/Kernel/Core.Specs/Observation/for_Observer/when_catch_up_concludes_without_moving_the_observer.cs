// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A zero-work catch-up lap must not reset the consecutive catch-up recovery count (#4592).
/// </summary>
public class when_catch_up_concludes_without_moving_the_observer : given.an_observer_with_subscription
{
    int _attemptsBefore;
    int _attemptsAfter;
    bool _backedOff;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero);
        await _observer.CaughtUp(JobId.New(), 42L);
        await _observer.CaughtUp(JobId.New(), 42L);
        _attemptsBefore = 3;
        _observer.SetCatchupRecoveryAttemptsForTesting(_attemptsBefore);
    }

    async Task Because()
    {
        await _observer.CaughtUp(JobId.New(), 42L);
        _attemptsAfter = _observer.CatchupRecoveryAttempts;
        _backedOff = await _observer.IsCatchUpBackedOff();
    }

    [Fact] void should_not_reset_the_recovery_attempts() => _attemptsAfter.ShouldEqual(_attemptsBefore);
    [Fact] void should_back_off_catching_up_again() => _backedOff.ShouldBeTrue();
}
