// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriberExtensions.when_calling_the_subscriber;

public class and_orleans_gives_up_before_the_timeout : Specification
{
    IObserverSubscriber _subscriber;
    Exception _error;

    void Establish()
    {
        _subscriber = Substitute.For<IObserverSubscriber>();
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(Task.FromException<ObserverSubscriberResult>(new TimeoutException("Response did not arrive on time")));
    }

    async Task Because() => _error = await Catch.Exception(() => _subscriber.OnNextWithin(
        TimeSpan.FromMinutes(10),
        "some-partition",
        [],
        new ObserverSubscriberContext(null)));

    [Fact] void should_report_that_orleans_gave_up() => _error.ShouldBeOfExactType<SubscriberResponseTimedOut>();
    [Fact] void should_keep_the_orleans_timeout() => _error.InnerException.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_classify_as_a_timeout() => _error.ToFailureKind().ShouldEqual(FailureKind.Timeout);
}
