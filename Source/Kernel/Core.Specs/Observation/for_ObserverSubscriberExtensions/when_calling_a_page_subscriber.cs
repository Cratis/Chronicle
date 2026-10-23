// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriberExtensions;

public class when_calling_a_page_subscriber : Specification
{
    IObserverPageSubscriber _subscriber;
    AppendedEvent[] _events;
    ObserverSubscriberResult _result;

    void Establish()
    {
        _subscriber = Substitute.For<IObserverPageSubscriber, IUnpartitionedObserverSubscriber>();
        _events = Enumerable.Range(1, 50).Select(_ => AppendedEvent.EmptyWithEventSequenceNumber((ulong)_)).ToArray();
        _subscriber.OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(ObserverSubscriberResult.Ok(50UL));
    }

    async Task Because() => _result = await _subscriber.OnNextPageWithin(TimeSpan.Zero, _events, new(null));

    [Fact] void should_bypass_the_live_unpartitioned_cap() => _subscriber.Received(1).OnNextPage(_events, Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_return_the_page_result() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)50UL);
}
