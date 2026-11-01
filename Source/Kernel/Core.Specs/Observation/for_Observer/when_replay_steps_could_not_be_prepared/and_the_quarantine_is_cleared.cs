// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_steps_could_not_be_prepared;

public class and_the_quarantine_is_cleared : given.a_replaying_observer
{
    async Task Establish()
    {
        await _observer.ReplayStepsCouldNotBePrepared();
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because() => await _observer.ClearObserverQuarantine();

    [Fact] async Task should_not_be_quarantined() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] void should_start_a_new_replay() => _jobsManager.Received(1).Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
}
