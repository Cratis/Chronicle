// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Grains.Observation.for_Observer.when_failing_partition;

public class and_partition_is_already_failed : given.an_observer
{
    const string _eventSourceId = "Something";
    const string first_stack_trace = "First: This is the stack trace";
    const string first_message = "First: Something went wrong";
    const string second_stack_trace = "Second: This is the stack trace";
    const string second_message = "Second: Something went wrong";

    FailedPartition failed_partition;

    void Establish()
    {
        failed_partition = _failedPartitionsState.AddFailedPartition(_eventSourceId, 42UL, [first_message], first_stack_trace);
    }

    async Task Because() => await _observer.PartitionFailed(_eventSourceId, 44UL, [second_message], second_stack_trace);

    [Fact] void should_have_only_one_failed_partition() => _failedPartitionsState.Partitions.Count().ShouldEqual(1);
    [Fact] void should_have_two_attempts() => _failedPartitionsState.Partitions.First().Attempts.Count().ShouldEqual(2);
    [Fact] void should_have_the_correct_partition() => _failedPartitionsState.Partitions.First().Partition.ShouldEqual((Key)_eventSourceId);
    [Fact] void should_have_the_correct_tail_for_the_first_attempt() => _failedPartitionsState.Partitions.First().Attempts.First().SequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_have_the_correct_message_for_the_first_attempt() => _failedPartitionsState.Partitions.First().Attempts.First().Messages.First().ShouldEqual(first_message);
    [Fact] void should_have_the_correct_stack_trace_for_the_first_attempt() => _failedPartitionsState.Partitions.First().Attempts.First().StackTrace.ShouldEqual(first_stack_trace);
    [Fact] void should_have_the_correct_tail_for_the_second_attempt() => _failedPartitionsState.Partitions.First().Attempts.ToArray()[1].SequenceNumber.ShouldEqual((EventSequenceNumber)44UL);
    [Fact] void should_have_the_correct_message_for_the_second_attempt() => _failedPartitionsState.Partitions.First().Attempts.ToArray()[1].Messages.First().ShouldEqual(second_message);
    [Fact] void should_have_the_correct_stack_trace_for_the_second_attempt() => _failedPartitionsState.Partitions.First().Attempts.ToArray()[1].StackTrace.ShouldEqual(second_stack_trace);
}
