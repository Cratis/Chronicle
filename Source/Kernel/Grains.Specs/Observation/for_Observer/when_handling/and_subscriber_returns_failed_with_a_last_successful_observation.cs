// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Orleans.TestKit;

namespace Cratis.Chronicle.Grains.Observation.for_Observer.when_handling;

public class and_subscriber_returns_failed_with_a_last_successful_observation : given.an_observer_with_subscription_for_specific_event_type
{
    const string exception_message = "Something went wrong";
    const string event_source_id = "Something";
    const string exception_stack_trace = "This is the stack trace";

    static EventSequenceNumber event_sequence_number_to_handle;

    void Establish()
    {
        event_sequence_number_to_handle = 42UL;
        state_storage.State = state_storage.State with
        {
            NextEventSequenceNumber = event_sequence_number_to_handle,
            LastHandledEventSequenceNumber = 41UL
        };
        var failure = ObserverSubscriberResult.Failed(event_sequence_number_to_handle) with
        {
            ExceptionMessages = [exception_message],
            ExceptionStackTrace = exception_stack_trace
        };
        subscriber
            .Setup(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>()))
            .Returns(Task.FromResult(failure));
    }

    async Task Because() => await observer.Handle(event_source_id, [
        AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, event_sequence_number_to_handle),
        AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, event_sequence_number_to_handle.Next())]);

    [Fact] void should_write_state_once() => storage_stats.Writes.ShouldEqual(1);
    [Fact] void should_write_correct_last_handled_event_sequence_number_state() => state_storage.State.LastHandledEventSequenceNumber.ShouldEqual(event_sequence_number_to_handle);
    [Fact] void should_write_correct_next_event_sequence_number_state() => state_storage.State.NextEventSequenceNumber.ShouldEqual(event_sequence_number_to_handle.Next());
    [Fact] void should_write_failed_partitions_state_once() => failed_partitions_storage_stats.Writes.ShouldEqual(1);
    [Fact] void should_add_failed_partition() => failed_partitions_state.Partitions.Count().ShouldEqual(1);
    [Fact] void should_capture_partition() => failed_partitions_state.Partitions.First().Partition.Value.ShouldEqual(event_source_id);
    [Fact] void should_capture_exception_message() => failed_partitions_state.Partitions.First().Attempts.First().Messages.First().ShouldEqual(exception_message);
    [Fact] void should_capture_exception_stack_Trace() => failed_partitions_state.Partitions.First().Attempts.First().StackTrace.ShouldEqual(exception_stack_trace);
    [Fact] void should_capture_event_sequence_number() => failed_partitions_state.Partitions.First().Attempts.First().SequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
}