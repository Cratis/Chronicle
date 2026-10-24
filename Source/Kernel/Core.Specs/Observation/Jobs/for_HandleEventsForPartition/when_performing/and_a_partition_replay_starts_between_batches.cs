// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_a_partition_replay_starts_between_batches : given.a_performing_job_step_with_observer_filters
{
    int _batch;

    void Establish()
    {
        _eventCursor.MoveNext().Returns(_ => Task.FromResult(++_batch <= 2));
        _eventCursor.Current.Returns(_ => [CreateEvent((ulong)_batch, _audited)]);
        _observer.IsPartitionFenced(Arg.Any<Key>(), Arg.Any<JobId>()).Returns(false, true);
    }

    Task Because() => PerformStep();

    [Fact] void should_deliver_only_the_first_batch() => _handledBatches.SelectMany(_ => _).ShouldEqual([new EventSequenceNumber(1UL)]);
    [Fact] void should_check_before_each_batch() => _observer.Received(2).IsPartitionFenced(_performState.Partition, Arg.Any<JobId>());
    [Fact] void should_finish_successfully() => _succeeded.ShouldBeTrue();
    [Fact] void should_keep_the_last_handled_position() => _result.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)1UL);
    [Fact] void should_keep_the_last_scanned_position() => _result.LastScannedEventSequenceNumber.ShouldEqual((EventSequenceNumber)1UL);
}
