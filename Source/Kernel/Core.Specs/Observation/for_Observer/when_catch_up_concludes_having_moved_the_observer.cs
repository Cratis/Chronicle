// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_catch_up_concludes_having_moved_the_observer : given.an_observer_with_subscription
{
    int _attemptsAfter;
    bool _backedOff;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero);
        await _observer.CaughtUp(JobId.New(), 42L);
        _observer.SetCatchupRecoveryAttemptsForTesting(3);
    }

    async Task Because()
    {
        await _observer.CaughtUp(JobId.New(), 50L);
        _attemptsAfter = _observer.CatchupRecoveryAttempts;
        _backedOff = await _observer.IsCatchUpBackedOff();
    }

    [Fact] void should_reset_the_recovery_attempts() => _attemptsAfter.ShouldEqual(0);
    [Fact] void should_not_back_off() => _backedOff.ShouldBeFalse();
}
