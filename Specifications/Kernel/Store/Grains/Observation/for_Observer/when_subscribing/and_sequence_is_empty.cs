// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Observation;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer;

public class and_sequence_is_empty : given.an_observer_and_two_event_types
{
    void Establish()
    {
        event_sequence.Setup(_ => _.GetNextSequenceNumber()).Returns(Task.FromResult((EventSequenceNumber)0));
    }

    async Task Because() => await observer.Subscribe(event_types, observer_namespace);

    [Fact] void should_set_current_namespace_in_state() => state_on_write.CurrentNamespace.ShouldEqual(observer_namespace);
    [Fact] void should_set_state_to_active() => state_on_write.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_subscribe_to_sequences_stream() => sequence_stream.Verify(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()), Once());
    [Fact] void should_subscribe_with_offset_at_beginning() => subscribed_token.SequenceNumber.ShouldEqual((long)EventSequenceNumber.First.Value);
    [Fact] void should_subscribe_with_event_types() => subscribed_token.EventTypes.ShouldEqual(event_types);
}
