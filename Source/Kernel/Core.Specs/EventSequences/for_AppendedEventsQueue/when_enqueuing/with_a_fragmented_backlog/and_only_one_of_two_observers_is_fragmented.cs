// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_a_fragmented_backlog;

/// <summary>
/// The spill decision is per observer: only the observer whose share of the dispatch is fragmented moves to catch-up,
/// while an observer that needs a single delivery keeps receiving events live.
/// </summary>
public class and_only_one_of_two_observers_is_fragmented : given.a_fragmented_backlog
{
    readonly EventType _otherEventType = new("Some other event", 1);
    ObserverKey _otherObserverKey;
    IObserver _otherObserver;

    async Task Establish()
    {
        _otherObserverKey = new ObserverKey("other observer", "Some event store", "Some namespace", "Some event sequence");
        _otherObserver = Substitute.For<IObserver>();
        _observerProbe = identity => identity == _otherObserverKey.ToString() ? _otherObserver : _observer;
        await _queue.Subscribe(_otherObserverKey, [_otherEventType]);
    }

    async Task Because()
    {
        AppendedEvent[] events =
        [
            .. Alternating(Limit + 1, _eventType),
            EventOf(_otherEventType, "c", (ulong)Limit + 1),
            EventOf(_otherEventType, "c", (ulong)Limit + 2)
        ];
        await EnqueueAndDeplete(events);
    }

    [Fact] void should_spill_the_fragmented_observer() => _observer.Received(1).CatchUp();
    [Fact] void should_deliver_to_the_other_observer_in_one_call() =>
        _otherObserver.Received(1).Handle(Arg.Any<Key>(), Arg.Is<IEnumerable<AppendedEvent>>(events => events.Count() == 2));
    [Fact] void should_not_spill_the_other_observer() => _otherObserver.DidNotReceive().CatchUp();
    [Fact] async Task should_keep_only_the_other_observers_subscription() =>
        (await _queue.GetSubscriptions()).Select(_ => _.ObserverKey).ShouldContainOnly(_otherObserverKey);
}
