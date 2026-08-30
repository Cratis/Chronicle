// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriberExtensions.when_calling_the_subscriber;

/// <summary>
/// The caller stops waiting and says so in terms of the subscriber rather than the transport, so the failure that
/// gets recorded classifies as a timeout instead of looking like the observer being wrong.
/// </summary>
public class and_it_does_not_answer_within_the_timeout : Specification
{
    /// <summary>
    /// The smallest positive timeout there is, so the specification never waits on the clock for a result it already
    /// knows: the subscriber below never answers, so the timeout always wins and there is nothing to race.
    /// </summary>
    static readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(1);

    /// <summary>
    /// Slow on purpose - infinitely so - because giving up on it is the behavior under test.
    /// </summary>
    readonly TaskCompletionSource<ObserverSubscriberResult> _neverAnswers = new();
    IObserverSubscriber _subscriber;
    Exception _thrown;

    void Establish()
    {
        _subscriber = Substitute.For<IObserverSubscriber>();
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(_neverAnswers.Task);
    }

    async Task Because() => _thrown = await Catch.Exception(() =>
        _subscriber.OnNextWithin(_timeout, "some-partition", [], new ObserverSubscriberContext(null)));

    void Destroy() => _neverAnswers.TrySetCanceled();

    [Fact] void should_give_up_on_the_subscriber() => _thrown.ShouldBeOfExactType<SubscriberCallTimedOut>();
    [Fact] void should_classify_as_a_timeout() => _thrown.ToFailureKind().ShouldEqual(FailureKind.Timeout);
}
