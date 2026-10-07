// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_checking_if_it_can_resume;

public class and_the_observer_is_quarantined : given.an_observer_in_running_state
{
    protected override ObserverRunningState RunningState => ObserverRunningState.Quarantined;

    async Task Because() => _canResume = await _job.CanResumeForTesting();

    [Fact] void should_not_be_able_to_resume() => _canResume.ShouldBeFalse();
    [Fact] async Task should_not_ask_the_observer_to_replay() => await _observer.DidNotReceive().Replay();
}
