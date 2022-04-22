// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Observation;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_subscribing;

public class and_event_types_has_changed : given.an_observer_and_two_event_types
{
    protected IEnumerable<EventType> new_event_types = new EventType[]
    {
        new("ad9f43ca-8d98-4669-99cd-dbd0eaaea9d9", 1),
        new("3e84ef60-c725-4b45-832d-29e3b663d7cd", 1),
        new("779b7d07-7a78-4c9c-a925-1c68d035cf1b", 1)
    };

    void Establish()
    {
        state.RunningState = ObserverRunningState.Active;
        state.NextEventSequenceNumber = 1;
        event_sequence.Setup(_ => _.GetNextSequenceNumber()).Returns(Task.FromResult((EventSequenceNumber)1));
    }

    async Task Because() => await observer.Subscribe(new_event_types, Guid.NewGuid().ToString());

    [Fact] void should_set_state_to_replaying() => state.RunningState.ShouldEqual(ObserverRunningState.Replaying);
    [Fact] void should_subscribe_to_sequences_stream() => sequence_stream.Verify(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()), Once());
    [Fact] void should_subscribe_with_offset_at_beginning() => subscribed_token.SequenceNumber.ShouldEqual((long)EventSequenceNumber.First.Value);
    [Fact] void should_subscribe_with_event_types() => subscribed_token.EventTypes.ShouldEqual(new_event_types);
}
