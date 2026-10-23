// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriberExtensions;

public class when_a_page_subscriber_times_out : Specification
{
    IObserverPageSubscriber _subscriber;
    readonly TaskCompletionSource<ObserverSubscriberResult> _pending = new();
    Exception? _error;

    void Establish()
    {
        _subscriber = Substitute.For<IObserverPageSubscriber>();
        _subscriber.OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(_pending.Task);
    }

    async Task Because() => _error = await Catch.Exception(() => _subscriber.OnNextPageWithin(TimeSpan.FromMilliseconds(1), [], new(null)));

    void Destroy() => _pending.TrySetResult(ObserverSubscriberResult.Ok(EventSequenceNumber.Unavailable));

    [Fact] void should_report_the_named_timeout() => _error.ShouldBeOfExactType<SubscriberCallTimedOut>();
}
