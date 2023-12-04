// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Kernel.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Observer.when_failing_partition;

public class and_partition_is_already_failed : given.an_observer
{
    const string event_source_id = "Something";
    const string first_stack_trace = "First: This is the stack trace";
    const string first_message = "First: Something went wrong";
    const string second_stack_trace = "Second: This is the stack trace";
    const string second_message = "Second: Something went wrong";

    FailedPartition failed_partition;

    void Establish()
    {
        failed_partition = new()
        {
            Partition = event_source_id,
            Attempts = new[]
            {
                new FailedPartitionAttempt
                {
                    Occurred = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(1)),
                    SequenceNumber = 42UL,
                    Messages = new[] { first_message },
                    StackTrace = first_stack_trace
                }
            }
        };
        failed_partitions_state.Add(failed_partition);
    }

    async Task Because() => await observer.PartitionFailed(event_source_id, 44UL, new[] { second_message }, second_stack_trace);

    [Fact] void should_have_only_one_failed_partition() => failed_partitions_state.Partitions.Count().ShouldEqual(1);
    [Fact] void should_have_two_attempts() => failed_partitions_state.Partitions.First().Attempts.Count().ShouldEqual(2);
    [Fact] void should_have_the_correct_partition() => failed_partitions_state.Partitions.First().Partition.ShouldEqual((Key)event_source_id);
    [Fact] void should_have_the_correct_tail_for_the_first_attempt() => failed_partitions_state.Partitions.First().Attempts.First().SequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_have_the_correct_message_for_the_first_attempt() => failed_partitions_state.Partitions.First().Attempts.First().Messages.First().ShouldEqual(first_message);
    [Fact] void should_have_the_correct_stack_trace_for_the_first_attempt() => failed_partitions_state.Partitions.First().Attempts.First().StackTrace.ShouldEqual(first_stack_trace);
    [Fact] void should_have_the_correct_tail_for_the_second_attempt() => failed_partitions_state.Partitions.First().Attempts.ToArray()[1].SequenceNumber.ShouldEqual((EventSequenceNumber)44UL);
    [Fact] void should_have_the_correct_message_for_the_second_attempt() => failed_partitions_state.Partitions.First().Attempts.ToArray()[1].Messages.First().ShouldEqual(second_message);
    [Fact] void should_have_the_correct_stack_trace_for_the_second_attempt() => failed_partitions_state.Partitions.First().Attempts.ToArray()[1].StackTrace.ShouldEqual(second_stack_trace);
}
