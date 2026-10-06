// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_removing;

public class and_late_work_arrives_during_cleanup : given.an_observer
{
    async Task Establish()
    {
        await _observer.PartitionFailed("partition", 12UL, ["Failed"], "Stack");
        await _observer.Remove();
        _jobsManager.ClearReceivedCalls();
        _storageStats.ResetCounts();
        _failedPartitionsStorageStats.ResetCounts();
    }

    async Task Because()
    {
        await _observer.TryRecoverAllFailedPartitions();
        await _observer.CatchUp();
        await _observer.CaughtUp(JobId.New(), 42UL);
        await _observer.Replayed(42UL);
        await _observer.PartitionFailed("late", 43UL, ["Late failure"], "Stack");
    }

    [Fact] void should_not_start_or_resume_jobs_after_the_fence() => _jobsManager.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_mutate_observer_state_from_late_work() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_reintroduce_failures_during_cleanup() => _failedPartitionsStorageStats.Writes.ShouldEqual(0);
}
