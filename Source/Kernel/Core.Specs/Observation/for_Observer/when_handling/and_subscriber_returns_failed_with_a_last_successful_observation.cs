// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_subscriber_returns_failed_with_a_last_successful_observation : given.an_observer_with_subscription_for_specific_event_type
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
        var failure = ObserverSubscriberResult.Failed(_eventSequenceNumberToHandle) with
        {
            ExceptionMessages = [ExceptionMessage],
            ExceptionStackTrace = ExceptionStackTrace
        };
        _subscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(Task.FromResult(failure));
    }

    async Task Because() => await _observer.Handle(EventSourceId, [
        AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, _eventSequenceNumberToHandle),
        AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, _eventSequenceNumberToHandle.Next())]);

    [Fact] void should_write_state_once() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_write_correct_last_handled_event_sequence_number_state() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_eventSequenceNumberToHandle);
    [Fact] void should_write_correct_next_event_sequence_number_state() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_eventSequenceNumberToHandle.Next());
    [Fact] void should_write_failed_partitions_state_once() => _failedPartitionsStorageStats.Writes.ShouldEqual(1);
    [Fact] void should_add_failed_partition() => _failedPartitionsState.Partitions.Count().ShouldEqual(1);
    [Fact] void should_capture_partition() => _failedPartitionsState.Partitions.First().Partition.Value.ShouldEqual(EventSourceId);
    [Fact] void should_capture_exception_message() => _failedPartitionsState.Partitions.First().Attempts.First().Messages.First().ShouldEqual(ExceptionMessage);
    [Fact] void should_capture_exception_stack_Trace() => _failedPartitionsState.Partitions.First().Attempts.First().StackTrace.ShouldEqual(ExceptionStackTrace);
    [Fact] void should_capture_event_sequence_number() => _failedPartitionsState.Partitions.First().Attempts.First().SequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
}
