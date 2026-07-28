// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueues.when_checking_subscription;

/// <summary>
/// A spill clears the observer from its queue without telling the observer. Another observer's subscription is left
/// on the queue so the answer has to come from matching the key, not from the queue happening to be empty.
/// </summary>
public class and_the_observer_was_dropped : given.two_seeded_queues
{
    ObserverKey _observerKey;
    bool _isSubscribed;

    void Establish()
    {
        _observerKey = ObserverKeyFor("a-spilled-observer");
        _queueGrains[QueueIndexFor(_observerKey)].GetSubscriptions().Returns(
        [
            new AppendedEventsQueueObserverSubscription(ObserverKeyFor("another-observer"), [subscribed_event_type.Id])
        ]);
    }

    async Task Because() => _isSubscribed = await _queues.IsSubscribed(_observerKey);

    [Fact] void should_report_the_observer_as_not_subscribed() => _isSubscribed.ShouldBeFalse();
}
