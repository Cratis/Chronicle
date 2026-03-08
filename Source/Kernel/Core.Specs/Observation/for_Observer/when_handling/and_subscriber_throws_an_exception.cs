// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_subscriber_throws_an_exception : given.an_observer_with_subscription_for_specific_event_type
{
    const string ExceptionMessage = "Something went wrong";
    const string EventSourceId = "Something";
    const string ExceptionStackTrace = "This is the stack trace";

    void Establish() =>
        _subscriber
            .When(_ => _.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()))
            .Throws(new ExceptionWithPreDefinedStackTrace(ExceptionMessage, ExceptionStackTrace));

    async Task Because() => await _observer.Handle(EventSourceId, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);

    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_write_failed_partitions_state_once() => _failedPartitionsStorageStats.Writes.ShouldEqual(1);
    [Fact] void should_add_failed_partition() => _failedPartitionsState.Partitions.Count().ShouldEqual(1);
    [Fact] void should_capture_partition() => _failedPartitionsState.Partitions.First().Partition.Value.ShouldEqual(EventSourceId);
    [Fact] void should_capture_exception_message() => _failedPartitionsState.Partitions.First().Attempts.First().Messages.First().ShouldEqual(ExceptionMessage);
    [Fact] void should_capture_exception_stack_Trace() => _failedPartitionsState.Partitions.First().Attempts.First().StackTrace.ShouldEqual(ExceptionStackTrace);
    [Fact] void should_capture_event_sequence_number() => _failedPartitionsState.Partitions.First().Attempts.First().SequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
}
