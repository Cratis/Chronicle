// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Observer.when_handling;

public class and_subscriber_returns_failed : given.an_observer_with_subscription_for_specific_event_type
{
    const string exception_message = "Something went wrong";
    const string event_source_id = "Something";
    const string exception_stack_trace = "This is the stack trace";

    void Establish()
    {
        var failure = ObserverSubscriberResult.Failed(42UL) with
        {
            ExceptionMessages = new[] { exception_message },
            ExceptionStackTrace = exception_stack_trace
        };
        subscriber
            .Setup(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>()))
            .Returns(Task.FromResult(failure));
    }

    async Task Because() => await observer.Handle(event_source_id, new[] { AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 0) });

    [Fact] void should_write_state_once() => silo.StorageStats().Writes.ShouldEqual(1);
    [Fact] void should_write_failed_partitions_state_once() => written_failed_partitions_states.Count.ShouldEqual(1);
    [Fact] void should_add_failed_partition() => written_failed_partitions_states[0].Partitions.Count().ShouldEqual(1);
    [Fact] void should_capture_partition() => written_failed_partitions_states[0].Partitions.First().Partition.Value.ShouldEqual(event_source_id);
    [Fact] void should_capture_exception_message() => written_failed_partitions_states[0].Partitions.First().Attempts.First().Messages.First().ShouldEqual(exception_message);
    [Fact] void should_capture_exception_stack_Trace() => written_failed_partitions_states[0].Partitions.First().Attempts.First().StackTrace.ShouldEqual(exception_stack_trace);
    [Fact] void should_capture_event_sequence_number() => written_failed_partitions_states[0].Partitions.First().Attempts.First().SequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
}
