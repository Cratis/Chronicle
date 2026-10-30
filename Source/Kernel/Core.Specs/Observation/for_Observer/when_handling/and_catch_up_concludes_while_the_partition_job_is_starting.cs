// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// A live batch hand-over holds its partition before its job is started, so the job is not yet listed. CaughtUp is
/// AlwaysInterleave, and the observer-wide job it hands over releases every hold without an owning partition job;
/// releasing this one would leave the partition job running while live delivery resumes for the same partition.
/// </summary>
public class and_catch_up_concludes_while_the_partition_job_is_starting : given.an_observer_with_a_live_delivery_batch
{
    static readonly JobId _concludingJob = JobId.New();
    static readonly JobId _partitionJob = JobId.New();
    readonly TaskCompletionSource<Result<JobId, StartJobError>> _partitionJobStart = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _heldWhileStarting;

    void Establish()
    {
        HasOutstandingJobs(new JobState
        {
            Id = _concludingJob,
            Status = JobStatus.Running,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
        });

        _jobsManager
            .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(_ => _partitionJobStart.Task);
        _jobsManager
            .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(_ => Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)2UL);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)2UL);
    }

    async Task Because()
    {
        var handling = _observer.Handle(Partition, _events);
        await _observer.CaughtUp(_concludingJob, 1UL);
        _heldWhileStarting = _stateStorage.State.CatchingUpPartitions.Contains(Partition);

        _partitionJobStart.SetResult(Result<JobId, StartJobError>.Success(_partitionJob));
        await handling;
    }

    [Fact] void should_keep_the_hold_while_the_partition_job_is_starting() => _heldWhileStarting.ShouldBeTrue();
    [Fact] void should_keep_the_hold_once_the_partition_job_has_started() => _stateStorage.State.CatchingUpPartitions.ShouldContain(Partition);
    [Fact] void should_start_the_partition_job_once() => _jobsManager.Received(1).Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>());
}
