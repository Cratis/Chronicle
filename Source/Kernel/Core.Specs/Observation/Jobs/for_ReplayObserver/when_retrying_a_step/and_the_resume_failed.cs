// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_retrying_a_step;

public class and_the_resume_failed : given.a_running_reactor_replay
{
    bool _resumed;

    async Task Establish()
    {
        await FailByTimeout(0);
        _resumed = await ResumeWithObserverNotEnteringReplay();
    }

    async Task Because() => await _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_have_failed_to_resume() => _resumed.ShouldBeFalse();
    [Fact] async Task should_have_reached_the_resume_hook() => await _observer.Received().Replay();
    [Fact] void should_not_retry_the_step_for_an_observer_that_is_not_replaying() => RetriesOf(0).ShouldEqual(0);
    [Fact] async Task should_not_resume_replay_in_the_sinks() => await _replayServiceClient.DidNotReceive().ResumeReplayFor(Arg.Any<ObserverDetails>());
    [Fact] void should_stay_running() => _stateStorage.State.Status.ShouldEqual(JobStatus.Running);
}
