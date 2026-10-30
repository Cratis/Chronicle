// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_a_window_step_completes;

public class and_the_resume_failed : given.a_running_reactor_replay
{
    int _startedBefore;
    bool _resumed;

    protected override int NumberOfPartitions => ReplayObserver.ActiveStepWindow + 1;

    async Task Establish()
    {
        _startedBefore = Started;
        _resumed = await ResumeWithObserverNotEnteringReplay();
    }

    async Task Because() => await Succeed(0);

    [Fact] void should_have_started_only_the_window() => _startedBefore.ShouldEqual(ReplayObserver.ActiveStepWindow);
    [Fact] void should_have_failed_to_resume() => _resumed.ShouldBeFalse();
    [Fact] async Task should_have_reached_the_resume_hook() => await _observer.Received().Replay();
    [Fact] void should_not_start_the_waiting_step() => Started.ShouldEqual(ReplayObserver.ActiveStepWindow);
}
