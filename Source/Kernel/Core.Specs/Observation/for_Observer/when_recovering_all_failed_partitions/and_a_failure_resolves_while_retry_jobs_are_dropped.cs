// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovering_all_failed_partitions;

/// <summary>
/// Removing a retry job runs its completion callbacks, which reach the observer through interleaved calls. A partition
/// recovering from one of them while the drop is under way must neither stop the other drops nor the recovery.
/// </summary>
public class and_a_failure_resolves_while_retry_jobs_are_dropped : given.an_observer
{
    const string PartitionA = "event-source-a";
    const string PartitionB = "event-source-b";

    JobState _staleJobForA;
    JobState _staleJobForB;
    Exception? _error;

    void Establish()
    {
        var failedPartitions = new FailedPartitions();
        failedPartitions.AddFailedPartition(PartitionA);
        failedPartitions.AddFailedPartition(PartitionB);
        _failedPartitionsStorage.State = failedPartitions;
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 2 };

        _staleJobForA = given.stopped_retry_jobs.Prepared(_observerKey, PartitionA, FailedPartitionId.New());
        _staleJobForB = given.stopped_retry_jobs.Prepared(_observerKey, PartitionB, FailedPartitionId.New());
        HasOutstandingJobs(_staleJobForA, _staleJobForB);
        _jobsManager.Delete(_staleJobForA.Id).Returns(_ => _observer.FailedPartitionRecovered(PartitionB, EventSequenceNumber.First));
    }

    async Task Because()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        _error = await Catch.Exception(_silo.TimerRegistry.FireAllAsync);
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] async Task should_drop_the_job_of_the_first_partition() => await _jobsManager.Received().Delete(_staleJobForA.Id);
    [Fact] async Task should_drop_the_job_of_the_partition_that_recovered() => await _jobsManager.Received().Delete(_staleJobForB.Id);

    [Fact]
    void should_still_recover_the_first_partition() => _jobsManager.Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(
            Arg.Is<RetryFailedPartitionRequest>(_ => _.Key == (Key)PartitionA));
}
