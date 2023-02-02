// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.when_subscribing;

public class and_observer_is_new : given.an_observer_and_two_event_types
{
    void Establish()
    {
        state.RunningState = ObserverRunningState.New;
        state.NextEventSequenceNumber = 0;
        state.EventTypes = Array.Empty<EventType>();
        event_sequence_storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, event_types, null)).Returns(Task.FromResult((EventSequenceNumber)1));
    }

    async Task Because() => await observer.Subscribe<ObserverSubscriber>(event_types);

    [Fact] void should_set_state_to_replaying() => state.RunningState.ShouldEqual(ObserverRunningState.CatchingUp);
    [Fact] void should_subscribe_to_sequences_stream() => sequence_stream.Verify(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()), Once());
    [Fact] void should_subscribe_with_offset_at_beginning() => subscribed_token.SequenceNumber.ShouldEqual((long)EventSequenceNumber.First.Value);
}
