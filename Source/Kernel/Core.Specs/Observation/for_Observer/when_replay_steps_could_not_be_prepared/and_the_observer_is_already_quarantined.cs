// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_steps_could_not_be_prepared;

public class and_the_observer_is_already_quarantined : for_Observer.given.a_quarantined_observer
{
    void Establish() => _stateStorage.State = _stateStorage.State with { IsReplaying = true };

    async Task Because()
    {
        await _observer.ReplayStepsCouldNotBePrepared();
        await RunWatchdogTicks();
    }

    [Fact] async Task should_remain_quarantined() => (await _observer.IsObserverQuarantined()).ShouldBeTrue();
    [Fact] void should_keep_the_replay_pending() => _stateStorage.State.IsReplaying.ShouldBeTrue();
    [Fact] void should_not_start_another_replay() => ShouldNotHaveStartedReplay();
}
