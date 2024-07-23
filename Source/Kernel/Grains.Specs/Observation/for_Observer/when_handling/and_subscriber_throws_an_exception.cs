// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Orleans.TestKit;

namespace Cratis.Chronicle.Grains.Observation.for_Observer.when_handling;

public class and_subscriber_throws_an_exception : given.an_observer_with_subscription_for_specific_event_type
{
    const string exception_message = "Something went wrong";
    const string event_source_id = "Something";
    const string exception_stack_trace = "This is the stack trace";

    void Establish() =>
        subscriber.Setup(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>())).Throws(new ExceptionWithPreDefinedStackTrace(exception_message, exception_stack_trace));

    async Task Because() => await observer.Handle(event_source_id, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);

    [Fact] void should_write_state_once() => storage_stats.Writes.ShouldEqual(1);
    [Fact] void should_write_failed_partitions_state_once() => failed_partitions_storage_stats.Writes.ShouldEqual(1);
    [Fact] void should_add_failed_partition() => failed_partitions_state.Partitions.Count().ShouldEqual(1);
    [Fact] void should_capture_partition() => failed_partitions_state.Partitions.First().Partition.Value.ShouldEqual(event_source_id);
    [Fact] void should_capture_exception_message() => failed_partitions_state.Partitions.First().Attempts.First().Messages.First().ShouldEqual(exception_message);
    [Fact] void should_capture_exception_stack_Trace() => failed_partitions_state.Partitions.First().Attempts.First().StackTrace.ShouldEqual(exception_stack_trace);
    [Fact] void should_capture_event_sequence_number() => failed_partitions_state.Partitions.First().Attempts.First().SequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
}
