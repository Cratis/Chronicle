// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueues.when_checking_subscription;

/// <summary>
/// Only the queue the observer routes to holds its subscription, so answering correctly means asking that queue and
/// no other. The other queues deliberately report a different observer.
/// </summary>
public class and_the_observer_is_on_its_queue : given.two_seeded_queues
{
    ObserverKey _observerKey;
    bool _isSubscribed;

    void Establish()
    {
        _observerKey = ObserverKeyFor("a-live-observer");
        _queueGrains[QueueIndexFor(_observerKey)].GetSubscriptions().Returns(
        [
            new AppendedEventsQueueObserverSubscription(_observerKey, [subscribed_event_type.Id])
        ]);
    }

    async Task Because() => _isSubscribed = await _queues.IsSubscribed(_observerKey);

    [Fact] void should_report_the_observer_as_subscribed() => _isSubscribed.ShouldBeTrue();
}
