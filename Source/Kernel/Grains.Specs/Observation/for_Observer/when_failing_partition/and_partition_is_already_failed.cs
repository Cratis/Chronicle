// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Grains.Observation.for_Observer.when_failing_partition;

public class and_partition_is_already_failed : given.an_observer
{
    const string EventSourceId = "Something";
    const string FirstStackTrace = "First: This is the stack trace";
    const string FirstMessage = "First: Something went wrong";
    const string SecondStackTrace = "Second: This is the stack trace";
    const string SecondMessage = "Second: Something went wrong";

    FailedPartition _failedPartition;

    void Establish()
    {
        _failedPartition = _failedPartitionsState.AddFailedPartition(EventSourceId, 42UL, [FirstMessage], FirstStackTrace);
    }

    async Task Because() => await _observer.PartitionFailed(EventSourceId, 44UL, [SecondMessage], SecondStackTrace);

    [Fact] void should_have_only_one_failed_partition() => _failedPartitionsState.Partitions.Count().ShouldEqual(1);
    [Fact] void should_have_two_attempts() => _failedPartitionsState.Partitions.First().Attempts.Count().ShouldEqual(2);
    [Fact] void should_have_the_correct_partition() => _failedPartitionsState.Partitions.First().Partition.ShouldEqual((Key)EventSourceId);
    [Fact] void should_have_the_correct_tail_for_the_first_attempt() => _failedPartitionsState.Partitions.First().Attempts.First().SequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_have_the_correct_message_for_the_first_attempt() => _failedPartitionsState.Partitions.First().Attempts.First().Messages.First().ShouldEqual(FirstMessage);
    [Fact] void should_have_the_correct_stack_trace_for_the_first_attempt() => _failedPartitionsState.Partitions.First().Attempts.First().StackTrace.ShouldEqual(FirstStackTrace);
    [Fact] void should_have_the_correct_tail_for_the_second_attempt() => _failedPartitionsState.Partitions.First().Attempts.ToArray()[1].SequenceNumber.ShouldEqual((EventSequenceNumber)44UL);
    [Fact] void should_have_the_correct_message_for_the_second_attempt() => _failedPartitionsState.Partitions.First().Attempts.ToArray()[1].Messages.First().ShouldEqual(SecondMessage);
    [Fact] void should_have_the_correct_stack_trace_for_the_second_attempt() => _failedPartitionsState.Partitions.First().Attempts.ToArray()[1].StackTrace.ShouldEqual(SecondStackTrace);
}
