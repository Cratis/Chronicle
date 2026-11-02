// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_nudging_stalled_jobs;

public class and_the_observer_does_not_depend_on_a_job : given.an_observer_with_a_stalled_job
{
    void Establish() => _stateStorage.State = _stateStorage.State with { IsReplaying = false, RunningState = ObserverRunningState.Active };
    async Task Because() => await _observer.NudgeStalledJobs();
    [Fact] async Task should_not_list_jobs() => await _jobsManager.DidNotReceive().GetJobs(Arg.Any<JobQuery>());
}
