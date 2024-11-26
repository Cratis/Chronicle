// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Grains.Observation.for_Observer.when_failing_partition;

public class first_time : given.an_observer
{
    const string event_source_id = "Something";
    const string stack_trace = "This is the stack trace";
    const string message = "Something went wrong";

    async Task Because() => await _observer.PartitionFailed(event_source_id, 42UL, [message], stack_trace);

    [Fact] void should_have_only_one_failed_partition() => _failedPartitionsState.Partitions.Count().ShouldEqual(1);
    [Fact] void should_have_the_correct_partition() => _failedPartitionsState.Partitions.First().Partition.ShouldEqual((Key)event_source_id);
    [Fact] void should_have_the_correct_tail() => _failedPartitionsState.Partitions.First().Attempts.First().SequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_have_the_correct_message() => _failedPartitionsState.Partitions.First().Attempts.First().Messages.First().ShouldEqual(message);
    [Fact] void should_have_the_correct_stack_trace() => _failedPartitionsState.Partitions.First().Attempts.First().StackTrace.ShouldEqual(stack_trace);
}
