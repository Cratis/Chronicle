// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_nudging_stalled_jobs;

public class and_two_ticks_fall_within_the_stall_period : given.an_observer_with_a_stalled_job
{
    async Task Because()
    {
        await _observer.NudgeStalledJobs();
        await _observer.NudgeStalledJobs();
    }

    [Fact] void should_list_the_jobs_once() => _jobsManager.Received(1).GetJobs(Arg.Any<JobQuery>());
}
