// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_an_operator_retries_a_failed_bounded_repair : given.an_observer_behind_a_repair_barrier
{
    bool _successorSkippedTheFailedPartition;

    void Establish()
    {
        GivenHealthySuccessorPartition("Q");
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(async _ =>
            {
                _successorSkippedTheFailedPartition = (await _observer.GetFailedPartitionKeys()).Contains((Key)"P");
                await _observer.CaughtUp(JobId.NotSet, 13UL, 16UL);
                return Result<JobId, StartJobError>.Success(JobId.New());
            });
        _jobsManager.Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>())
            .Returns(async _ =>
            {
                await _subscriber.OnNext("P", [Event(12UL), Event(13UL), Event(14UL), Event(15UL), Event(16UL)], new ObserverSubscriberContext(null));
                await _observer.FailedPartitionRecovered("P", 16UL, 16UL);
                return Result<JobId, StartJobError>.Success(JobId.New());
            });
    }

    async Task Because()
    {
        await _observer.CatchUp();
        await _silo.RunCatchUpContinuation();
        await _observer.Handle("P", [Event(14UL)]);
        await _observer.HandoverRepairCaughtUp("P", 11UL, 14UL, 11UL, succeeded: false);
        await _silo.RunCatchUpContinuation();
        await _observer.Handle("P", [Event(14UL)]);
        await _observer.TryStartRecoverJobForFailedPartition("P");
    }

    [Fact] void should_exclude_the_failed_partition_from_the_successor() => _successorSkippedTheFailedPartition.ShouldBeTrue();
    [Fact] void should_retry_from_the_failure_position() => _jobsManager.Received(1).Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(_ => _.FromSequenceNumber == (EventSequenceNumber)12UL));
    [Fact] void should_deliver_the_failed_range_and_successor_suffix_once() => _delivered.ShouldEqual([(EventSequenceNumber)12UL, (EventSequenceNumber)13UL, (EventSequenceNumber)14UL, (EventSequenceNumber)15UL, (EventSequenceNumber)16UL]);
    [Fact] void should_clear_the_failure_after_retry() => _failedPartitionsStorage.State.Partitions.ShouldBeEmpty();
    [Fact] void should_not_recreate_a_repair_for_the_suffix() => _jobsManager.Received(1).Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>());
}
