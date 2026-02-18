// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_subscriber_returns_failed_without_a_last_successful_observation : given.an_observer_with_subscription_for_specific_event_type
{
    const string ExceptionMessage = "Something went wrong";
    const string EventSourceId = "Something";
    const string ExceptionStackTrace = "This is the stack trace";
    static EventSequenceNumber _eventSequenceNumberToHandle;

    void Establish()
    {
        _eventSequenceNumberToHandle = 42UL;
        _stateStorage.State = _stateStorage.State with
        {
            NextEventSequenceNumber = _eventSequenceNumberToHandle,
            LastHandledEventSequenceNumber = 41UL
        };
        var failure = ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable) with
        {
            ExceptionMessages = [ExceptionMessage],
            ExceptionStackTrace = ExceptionStackTrace
        };
        _subscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(failure);
    }

    async Task Because() => await _observer.Handle(EventSourceId, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, _eventSequenceNumberToHandle)]);

    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_have_same_next_event_sequence_number_state() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_eventSequenceNumberToHandle);
    [Fact] void should_have_same_last_handled_event_sequence_number_state() => _stateStorage.State.LastHandledEventSequenceNumber.Value.ShouldEqual(41UL);
    [Fact] void should_write_failed_partitions_state_once() => _failedPartitionsStorageStats.Writes.ShouldEqual(1);
    [Fact] void should_add_failed_partition() => _failedPartitionsState.Partitions.Count().ShouldEqual(1);
    [Fact] void should_capture_partition() => _failedPartitionsState.Partitions.First().Partition.Value.ShouldEqual(EventSourceId);
    [Fact] void should_capture_exception_message() => _failedPartitionsState.Partitions.First().Attempts.First().Messages.First().ShouldEqual(ExceptionMessage);
    [Fact] void should_capture_exception_stack_Trace() => _failedPartitionsState.Partitions.First().Attempts.First().StackTrace.ShouldEqual(ExceptionStackTrace);
    [Fact] void should_capture_event_sequence_number() => _failedPartitionsState.Partitions.First().Attempts.First().SequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
}
