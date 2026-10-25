// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_nudging_stalled_jobs;

public class and_a_catch_up_concludes_while_listing_jobs : given.an_observer_with_a_stalled_job
{
    async Task Because()
    {
        await _observer.NudgeStalledJobs();
        AdvanceClock();
        var lookup = new TaskCompletionSource<IImmutableList<JobState>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(
            _ => lookup.Task,
            _ => Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(_job)));
        var nudge = _observer.NudgeStalledJobs();
        var caughtUp = _observer.CaughtUp(_job.Id, EventSequenceNumber.First);
        lookup.SetResult(ImmutableList.Create(_job));
        await Task.WhenAll(nudge, caughtUp).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] async Task should_not_resume_the_job() => await _jobsManager.DidNotReceive().ResumeIfRunning(_job.Id);
}
