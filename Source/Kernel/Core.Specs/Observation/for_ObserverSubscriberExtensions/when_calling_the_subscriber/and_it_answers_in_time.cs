// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriberExtensions.when_calling_the_subscriber;

public class and_it_answers_in_time : Specification
{
    IObserverSubscriber _subscriber;
    ObserverSubscriberResult _result;

    void Establish()
    {
        _subscriber = Substitute.For<IObserverSubscriber>();
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(EventSequenceNumber.First));
    }

    async Task Because() => _result = await _subscriber.OnNextWithin(
        TimeSpan.FromMinutes(1),
        "some-partition",
        [],
        new ObserverSubscriberContext(null));

    [Fact] void should_return_what_the_subscriber_answered() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
}
