// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation.for_ObserverState;

public class when_failing_partition_first_time : Specification
{
    static EventSourceId partition = Guid.NewGuid();
    static EventSequenceNumber sequence_number = 42;
    static string[] messages = new[]
    {
        "first line",
        "second line"
    };
    static string stack_trace = "The stack trace";
    ObserverState state;

    void Establish() => state = new();

    void Because() => state.FailPartition(partition, sequence_number, messages, stack_trace);

    [Fact] void should_have_only_one_failed_partition() => state.FailedPartitions.Count().ShouldEqual(1);
    [Fact] void should_set_correct_partition() => state.FailedPartitions.First().EventSourceId.ShouldEqual(partition);
    [Fact] void should_set_correct_sequence_number() => state.FailedPartitions.First().SequenceNumber.ShouldEqual(sequence_number);
    [Fact] void should_set_correct_messages() => state.FailedPartitions.First().Messages.ShouldEqual(messages);
    [Fact] void should_set_correct_stack_trace() => state.FailedPartitions.First().StackTrace.ShouldEqual(stack_trace);
    [Fact] void should_set_attempts_count_to_one() => state.FailedPartitions.First().Attempts.ShouldEqual(1);
    [Fact] void should_have_failed_partitions_state() => state.HasFailedPartitions.ShouldBeTrue();
}
