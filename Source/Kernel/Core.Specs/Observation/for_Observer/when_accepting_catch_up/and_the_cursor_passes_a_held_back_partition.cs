// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_accepting_catch_up;

public class and_the_cursor_passes_a_held_back_partition : given.an_observer_accepting_a_catch_up_handover
{
    ObserverState _accepted;
    bool _acquiredInline;

    async Task Because()
    {
        await _observer.CaughtUp(JobId.NotSet, 12UL, 12UL);
        _accepted = _writes.PersistedState;
        _acquiredInline = _jobsManager.ReceivedCalls().Any();
    }

    [Fact] void should_accept_the_scanned_cursor_without_rewinding() => _accepted.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)13UL);
    [Fact] void should_accept_a_durable_partition_obligation() => _accepted.PendingCatchUpRepairs!.ShouldContainOnly(new PendingCatchUpRepair("B", 10UL, 13UL));
    [Fact] void should_defer_jobs_manager_work() => _acquiredInline.ShouldBeFalse();
    [Fact] void should_not_start_a_partition_job_inside_the_acknowledgement() => _jobsManager.DidNotReceive().Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>());
    [Fact] void should_not_query_the_jobs_manager_inside_the_acknowledgement() => _jobsManager.DidNotReceive().GetJobs(Arg.Any<JobQuery>());
}
