// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_nudging_stalled_jobs;

public class and_resuming_a_stalled_job_is_slow : given.an_observer_with_a_stalled_job
{
    readonly TaskCompletionSource<bool> _resume = new();
    bool _completed;

    void Establish() => _jobsManager.Resume(_job.Id).Returns(_resume.Task);

    async Task Because()
    {
        await _observer.NudgeStalledJobs();
        AdvanceClock();
        var nudge = _observer.NudgeStalledJobs();
        _completed = nudge.IsCompletedSuccessfully;
        _resume.SetResult(true);
        await nudge;
    }

    [Fact] void should_not_wait_for_the_resume() => _completed.ShouldBeTrue();
    [Fact] async Task should_attempt_the_resume_once() => await _jobsManager.Received(1).Resume(_job.Id);
}
