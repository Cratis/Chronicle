// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_nudging_stalled_jobs;

public class and_quarantine_begins_while_listing_jobs : given.an_observer_with_a_stalled_job
{
    async Task Because()
    {
        await _observer.NudgeStalledJobs();
        AdvanceClock();
        var query = new TaskCompletionSource<IImmutableList<JobState>>();
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(
            _ => query.Task,
            _ => Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(_job)));
        var nudge = _observer.NudgeStalledJobs();
        await _observer.TransitionTo<QuarantinedObserver>();
        query.SetResult(ImmutableList.Create(_job));
        await nudge;
    }

    [Fact] async Task should_not_resume_the_job() => await _jobsManager.DidNotReceive().Resume(_job.Id);
}
