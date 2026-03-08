// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for <see cref="IObserverSubscriber"/>.
/// </summary>
public static class ObserverSubscriberExtensions
{
    /// <summary>
    /// Gets the keys for the subscriber.
    /// </summary>
    /// <param name="subscriber">The <see cref="IObserverSubscriber"/>.</param>
    /// <returns><see cref="ObserverKey"/> and <see cref="ObserverSubscriberKey"/>.</returns>
    public static (ObserverKey ObserverKey, ObserverSubscriberKey ObserverSubscriberKey) GetKeys(
        this IObserverSubscriber subscriber)
    {
        var subscriberKey = ObserverSubscriberKey.Parse(subscriber.GetPrimaryKeyString());
        var observerKey = new ObserverKey(subscriberKey.ObserverId, subscriberKey.EventStore, subscriberKey.Namespace, subscriberKey.EventSequenceId);
        return (observerKey, subscriberKey);
    }
}
