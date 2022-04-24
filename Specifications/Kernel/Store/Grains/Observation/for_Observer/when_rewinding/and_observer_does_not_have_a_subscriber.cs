// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Observation;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_rewinding;

public class and_observer_does_not_have_a_subscriber : given.an_observer_and_two_event_types
{
    void Establish() => state.CurrentNamespace = ObserverNamespace.NotSet;

    async Task Because() => await observer.Rewind();

    [Fact] void should_not_subscribe_to_sequence_stream() => sequence_stream.Verify(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<StreamSequenceToken>(), IsAny<StreamFilterPredicate>(), IsAny<object>()), Never());
}
