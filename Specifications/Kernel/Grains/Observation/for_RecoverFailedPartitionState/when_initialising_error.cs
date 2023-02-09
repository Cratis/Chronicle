// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_RecoverFailedPartitionState;

public class when_initialising_error : Specification
{
    static readonly IEnumerable<EventType> event_types = new[]
        { new EventType(Guid.NewGuid(), 1, false), new EventType(Guid.NewGuid(), 1, false) };

    RecoverFailedPartitionState state;
    ObserverSubscriberKey subscriber_key;
    ObserverKey observer_key;
    EventSourceId partition_id;
    EventSequenceNumber initial_error;

    void Establish()
    {
        initial_error = EventSequenceNumber.First;
        partition_id = Guid.NewGuid();
        observer_key = new ObserverKey(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        subscriber_key = ObserverSubscriberKey.FromObserverKey(observer_key, partition_id);

        state = new RecoverFailedPartitionState();
    }

    void Because()
    {
        state.InitialiseError(initial_error, event_types, observer_key, subscriber_key);
    }

    [Fact]
    void should_set_the_initial_error() => state.InitialError.ShouldEqual(initial_error);

    [Fact]
    void should_set_the_current_error() => state.CurrentError.ShouldEqual(initial_error);

    [Fact]
    void should_set_the_next_sequence_number() => state.NextSequenceNumberToProcess.ShouldEqual(initial_error);

    [Fact]
    void should_set_the_initial_failed_on() => state.InitialPartitionFailedOn.ShouldNotEqual(DateTimeOffset.MinValue);

    [Fact]
    void should_set_the_event_types() => state.EventTypes.ShouldEqual(event_types);

    [Fact]
    void should_set_the_subscriber_key() => state.SubscriberKey.ShouldEqual(subscriber_key.ToString());

    [Fact]
    void should_set_the_observer_key() => state.ObserverKey.ShouldEqual(observer_key.ToString());
}