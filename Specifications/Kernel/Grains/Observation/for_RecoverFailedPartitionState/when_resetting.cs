// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_RecoverFailedPartitionState;

public class when_resetting : Specification
{
    static readonly IEnumerable<EventType> event_types = new[] { new EventType(Guid.NewGuid(), 1, false), new EventType(Guid.NewGuid(), 1, false) };

    RecoverFailedPartitionState state;
    ObserverId observer_id;
    string subscriber_key;
    string observer_key;

    Task Establish()
    {
        observer_id = Guid.NewGuid();
        subscriber_key = "subscriber_key";
        observer_key = "observer_key";

        state = new RecoverFailedPartitionState()
        {
            EventTypes = event_types,
            ObserverId = observer_id,
            InitialError = EventSequenceNumber.First,
            CurrentError = EventSequenceNumber.Max,
            NextSequenceNumberToProcess = EventSequenceNumber.Max,
            NumberOfAttemptsOnCurrentError = 10,
            NumberOfAttemptsOnSinceInitialized = 100,
            InitialPartitionFailedOn = DateTimeOffset.UtcNow,
            LastAttemptOnCurrentError = DateTimeOffset.UtcNow,
            Messages = new[] { "Something went wrong" },
            StackTrace = "something went wrong",
            SubscriberKey = subscriber_key,
            ObserverKey = observer_key
        };

        return Task.CompletedTask;
    }

    Task Because()
    {
        state.Reset();
        return Task.CompletedTask;
    }

    [Fact] void should_reset_the_initial_error() => state.InitialError.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_reset_the_current_error() => state.CurrentError.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_reset_the_next_sequence_number() => state.NextSequenceNumberToProcess.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_reset_the_number_of_attempts_current() => state.NumberOfAttemptsOnCurrentError.ShouldEqual(0);
    [Fact] void should_reset_the_number_of_attempts_total() => state.NumberOfAttemptsOnSinceInitialized.ShouldEqual(0);
    [Fact] void should_reset_the_initial_failed_on() => state.InitialPartitionFailedOn.ShouldEqual(DateTimeOffset.MinValue);
    [Fact] void should_reset_the_last_attempted() => state.LastAttemptOnCurrentError.ShouldEqual(DateTimeOffset.MinValue);
    [Fact] void should_reset_the_error_messages() => state.Messages.ShouldBeEmpty();
    [Fact] void should_reset_the_stacktrace() => state.StackTrace.ShouldEqual(string.Empty);
    [Fact] void should_not_reset_the_event_types() => state.EventTypes.ShouldEqual(event_types);
    [Fact] void should_not_reset_the_observer_id() => state.ObserverId.ShouldEqual(observer_id);
    [Fact] void should_not_reset_the_subscriber_key() => state.SubscriberKey.ShouldEqual(subscriber_key);
    [Fact] void should_not_reset_the_observer_key() => state.ObserverKey.ShouldEqual(observer_key);
}
