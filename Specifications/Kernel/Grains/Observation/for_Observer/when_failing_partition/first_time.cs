// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Keys;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Observer.when_failing_partition;

public class first_time : given.an_observer
{
    const string event_source_id = "Something";
    const string stack_trace = "This is the stack trace";
    const string message = "Something went wrong";

    async Task Because() => await observer.PartitionFailed(event_source_id, 42UL, new[] { message }, stack_trace);

    [Fact] void should_have_only_one_failed_partition() => failed_partitions_state.Partitions.Count().ShouldEqual(1);
    [Fact] void should_have_the_correct_partition() => failed_partitions_state.Partitions.First().Partition.ShouldEqual((Key)event_source_id);
    [Fact] void should_have_the_correct_tail() => failed_partitions_state.Partitions.First().Attempts.First().SequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_have_the_correct_message() => failed_partitions_state.Partitions.First().Attempts.First().Messages.First().ShouldEqual(message);
    [Fact] void should_have_the_correct_stack_trace() => failed_partitions_state.Partitions.First().Attempts.First().StackTrace.ShouldEqual(stack_trace);
}
