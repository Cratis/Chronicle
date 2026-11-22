// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Gives the observers of event store subscriptions their ids, and reads the target event store back out of one.
/// </summary>
/// <remarks>
/// The observer lives in the source event store's outbox and forwards to the target, so the id is unique per
/// (subscription, target): without the target, two target stores subscribing to the same source would share one observer
/// and the second would be starved. Because the id names the target, an observer woken from its durable record learns where
/// to forward from its own id, and the record need not carry it.
/// </remarks>
public static class EventStoreSubscriptionObserverIds
{
    const string Separator = "->";

    /// <summary>
    /// Gets the id of the observer of a subscription for a target event store.
    /// </summary>
    /// <param name="subscriptionId">The <see cref="EventStoreSubscriptionId"/>.</param>
    /// <param name="targetEventStore">The <see cref="EventStoreName"/> the subscription forwards to.</param>
    /// <returns>The <see cref="ObserverId"/>.</returns>
    public static ObserverId For(EventStoreSubscriptionId subscriptionId, EventStoreName targetEventStore) =>
        new($"{subscriptionId.Value}{Separator}{targetEventStore.Value}");

    /// <summary>
    /// Gets the target event store an observer of an event store subscription forwards to.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/>.</param>
    /// <param name="targetEventStore">The target <see cref="EventStoreName"/> when the id names one.</param>
    /// <returns>True when the id names a target; false otherwise.</returns>
    public static bool TryGetTargetEventStore(ObserverId observerId, out EventStoreName targetEventStore)
    {
        var value = observerId.Value;
        var index = value.LastIndexOf(Separator, StringComparison.Ordinal);
        if (index < 0 || index + Separator.Length >= value.Length)
        {
            targetEventStore = EventStoreName.NotSet;
            return false;
        }

        targetEventStore = new EventStoreName(value[(index + Separator.Length)..]);
        return true;
    }
}
