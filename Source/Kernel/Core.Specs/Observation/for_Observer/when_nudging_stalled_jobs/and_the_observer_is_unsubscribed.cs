// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_nudging_stalled_jobs;

public class and_the_observer_is_unsubscribed : given.an_observer_with_a_stalled_job
{
    async Task Establish()
    {
        await _observer.Unsubscribe();
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because() => await _observer.NudgeStalledJobs();
    [Fact] async Task should_not_list_jobs() => await _jobsManager.DidNotReceive().GetJobs(Arg.Any<JobQuery>());
}
