// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class first_time : given.an_observer
{
    const string EventSourceId = "Something";
    const string StackTrace = "This is the stack trace";
    const string Message = "Something went wrong";

    async Task Because() => await _observer.PartitionFailed(EventSourceId, 42UL, [Message], StackTrace);

    [Fact] void should_have_only_one_failed_partition() => _failedPartitionsState.Partitions.Count().ShouldEqual(1);
    [Fact] void should_have_the_correct_partition() => _failedPartitionsState.Partitions.First().Partition.ShouldEqual((Key)EventSourceId);
    [Fact] void should_have_the_correct_tail() => _failedPartitionsState.Partitions.First().Attempts.First().SequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_have_the_correct_message() => _failedPartitionsState.Partitions.First().Attempts.First().Messages.First().ShouldEqual(Message);
    [Fact] void should_have_the_correct_stack_trace() => _failedPartitionsState.Partitions.First().Attempts.First().StackTrace.ShouldEqual(StackTrace);
}
