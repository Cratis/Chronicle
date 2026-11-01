// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

public class and_a_cursor_batch_holds_more_events_of_one_partition_than_one_call_may_carry : given.a_performing_job_step
{
    void Establish()
    {
        _performState.EventObservationState = EventObservationState.None;
        _jobStep.MaxEventsPerBatch = 2;
        _eventCursor.Current.Returns(Enumerable.Range(1, 5).Select(_ => CreateEvent((ulong)_, "module")).ToArray());
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_call_the_subscriber_once_per_slice() => _handledBatches.Count.ShouldEqual(3);
    [Fact] void should_fill_the_first_call_to_the_bound() => _handledBatches[0].SequenceNumbers.Count.ShouldEqual(2);
    [Fact] void should_fill_the_second_call_to_the_bound() => _handledBatches[1].SequenceNumbers.Count.ShouldEqual(2);
    [Fact] void should_send_the_remainder_last() => _handledBatches[2].SequenceNumbers.Single().ShouldEqual((EventSequenceNumber)5UL);
}
