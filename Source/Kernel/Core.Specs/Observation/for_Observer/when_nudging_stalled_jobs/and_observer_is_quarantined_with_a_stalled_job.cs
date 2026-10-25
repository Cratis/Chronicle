// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_nudging_stalled_jobs;

public class and_observer_is_quarantined_with_a_stalled_job : given.an_observer_with_a_stalled_job
{
    async Task Because()
    {
        await _observer.NudgeStalledJobs();
        AdvanceClock();
        await _observer.TransitionTo<QuarantinedObserver>();
        await _observer.NudgeStalledJobs();
    }

    [Fact] async Task should_not_resume_the_job() => await _jobsManager.DidNotReceive().Resume(_job.Id);
}
