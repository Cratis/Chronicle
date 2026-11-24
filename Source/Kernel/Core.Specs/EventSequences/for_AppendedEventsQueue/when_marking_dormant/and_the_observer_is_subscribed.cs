// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_marking_dormant;

/// <summary>
/// The queue keeps nothing for a dormant observer: its live subscription is dropped and the table rings it by its next event.
/// </summary>
public class and_the_observer_is_subscribed : given.a_queue_with_an_unsubscribed_instance
{
    bool _released;
    bool _dormant;
    IReadOnlyList<AppendedEventsQueueObserverSubscription> _subscriptions;

    async Task Establish() => await _queue.Subscribe(_observerKey, EventTypeSet.From([_eventType]));

    async Task Because()
    {
        _released = await _queue.TryMarkDormant(_observerKey);
        _dormant = await _queue.IsDormant(_observerKey);
        _subscriptions = await _queue.GetSubscriptions();
    }

    [Fact] void should_release_the_subscription() => _released.ShouldBeTrue();
    [Fact] void should_hold_no_subscription_for_it() => _subscriptions.ShouldBeEmpty();
    [Fact] void should_report_it_as_dormant() => _dormant.ShouldBeTrue();
}
