// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
namespace Cratis.Chronicle.Grains.Observation.for_Observer.when_handling;

public class and_subscriber_returns_failed_without_a_last_successful_observation : given.an_observer_with_subscription_for_specific_event_type
{
    const string exception_message = "Something went wrong";
    const string event_source_id = "Something";
    const string exception_stack_trace = "This is the stack trace";
    static EventSequenceNumber event_sequence_number_to_handle;

    void Establish()
    {
        event_sequence_number_to_handle = 42UL;
        _stateStorage.State = _stateStorage.State with
        {
            NextEventSequenceNumber = event_sequence_number_to_handle,
            LastHandledEventSequenceNumber = 41UL
        };
        var failure = ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable) with
        {
            ExceptionMessages = [exception_message],
            ExceptionStackTrace = exception_stack_trace
        };
        _subscriber
            .OnNext(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(failure);
    }

    async Task Because() => await _observer.Handle(event_source_id, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, event_sequence_number_to_handle)]);

    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_have_same_next_event_sequence_number_state() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(event_sequence_number_to_handle);
    [Fact] void should_have_same_last_handled_event_sequence_number_state() => _stateStorage.State.LastHandledEventSequenceNumber.Value.ShouldEqual(41UL);
    [Fact] void should_write_failed_partitions_state_once() => _failedPartitionsStorageStats.Writes.ShouldEqual(1);
    [Fact] void should_add_failed_partition() => _failedPartitionsState.Partitions.Count().ShouldEqual(1);
    [Fact] void should_capture_partition() => _failedPartitionsState.Partitions.First().Partition.Value.ShouldEqual(event_source_id);
    [Fact] void should_capture_exception_message() => _failedPartitionsState.Partitions.First().Attempts.First().Messages.First().ShouldEqual(exception_message);
    [Fact] void should_capture_exception_stack_Trace() => _failedPartitionsState.Partitions.First().Attempts.First().StackTrace.ShouldEqual(exception_stack_trace);
    [Fact] void should_capture_event_sequence_number() => _failedPartitionsState.Partitions.First().Attempts.First().SequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
}
