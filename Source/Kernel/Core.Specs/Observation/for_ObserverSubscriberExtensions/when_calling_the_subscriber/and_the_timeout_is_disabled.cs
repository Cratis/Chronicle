// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriberExtensions.when_calling_the_subscriber;

/// <summary>
/// Zero is the escape hatch for a subscriber whose work legitimately has no upper bound - the call is left to the
/// transport's own timeout, which is what every delivery did before the observer's timeout was applied at all.
/// </summary>
public class and_the_timeout_is_disabled : Specification
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
        TimeSpan.Zero,
        "some-partition",
        [],
        new ObserverSubscriberContext(null));

    [Fact] void should_wait_for_the_subscriber() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
}
