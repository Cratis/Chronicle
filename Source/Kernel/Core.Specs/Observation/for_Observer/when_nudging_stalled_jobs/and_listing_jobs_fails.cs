// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_nudging_stalled_jobs;

public class and_listing_jobs_fails : given.an_observer_with_a_stalled_job
{
    async Task Because()
    {
        await _observer.NudgeStalledJobs();
        AdvanceClock();
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromException<IImmutableList<JobState>>(new InvalidOperationException("listing failed")));
        await _observer.NudgeStalledJobs();
    }

    [Fact] void should_keep_the_observer_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Replaying);
    [Fact] async Task should_not_resume_the_job() => await _jobsManager.DidNotReceive().ResumeIfRunning(_job.Id);
}
