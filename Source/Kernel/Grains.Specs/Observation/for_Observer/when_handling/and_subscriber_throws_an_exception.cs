// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Orleans.TestKit;

namespace Cratis.Chronicle.Grains.Observation.for_Observer.when_handling;

public class and_subscriber_throws_an_exception : given.an_observer_with_subscription_for_specific_event_type
{
    const string _exceptionMessage = "Something went wrong";
    const string _eventSourceId = "Something";
    const string _exceptionStackTrace = "This is the stack trace";

    void Establish() =>
        _subscriber
            .When(_ => _.OnNext(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()))
            .Throws(new ExceptionWithPreDefinedStackTrace(_exceptionMessage, _exceptionStackTrace));

    async Task Because() => await _observer.Handle(_eventSourceId, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);

    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_write_failed_partitions_state_once() => _failedPartitionsStorageStats.Writes.ShouldEqual(1);
    [Fact] void should_add_failed_partition() => _failedPartitionsState.Partitions.Count().ShouldEqual(1);
    [Fact] void should_capture_partition() => _failedPartitionsState.Partitions.First().Partition.Value.ShouldEqual(event_source_id);
    [Fact] void should_capture_exception_message() => _failedPartitionsState.Partitions.First().Attempts.First().Messages.First().ShouldEqual(exception_message);
    [Fact] void should_capture_exception_stack_Trace() => _failedPartitionsState.Partitions.First().Attempts.First().StackTrace.ShouldEqual(exception_stack_trace);
    [Fact] void should_capture_event_sequence_number() => _failedPartitionsState.Partitions.First().Attempts.First().SequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
}
