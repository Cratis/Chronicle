// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriberExtensions.when_calling_the_subscriber;

public class and_it_is_partitioned_with_many_events : Specification
{
    IObserverSubscriber _subscriber;

    void Establish()
    {
        _subscriber = Substitute.For<IObserverSubscriber>();
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(EventSequenceNumber.First));
    }

    async Task Because() => await _subscriber.OnNextWithin(
        TimeSpan.FromMinutes(1),
        "some-partition",
        Enumerable.Range(0, 50).Select(i => AppendedEvent.EmptyWithEventSequenceNumber((ulong)i)).ToArray(),
        new ObserverSubscriberContext(null));

    [Fact] void should_deliver_the_batch_in_one_call() => _subscriber.Received(1).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
}
