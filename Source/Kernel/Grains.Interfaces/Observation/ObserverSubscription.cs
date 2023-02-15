// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents a subscription to an observer.
/// </summary>
/// <param name="ObserverId">The <see cref="ObserverId"/> that the subscription is for.</param>
/// <param name="ObserverKey">The <see cref="ObserverKey"/> that the subscription is for.</param>
/// <param name="EventTypes">Represents the event types for the subscription.</param>
/// <param name="SubscriberType">Type that is subscribing.</param>
/// <param name="Arguments">Any arguments it passed.</param>
public record ObserverSubscription(ObserverId ObserverId, ObserverKey ObserverKey, IEnumerable<EventType> EventTypes, Type SubscriberType, object Arguments)
{
    /// <summary>
    /// Gets a subscription representing no subscription.
    /// </summary>
    public static readonly ObserverSubscription Unsubscribed = new(ObserverId.Unspecified, ObserverKey.NotSet, Enumerable.Empty<EventType>(), typeof(IObserverSubscriber), null!);

    /// <summary>
    /// Check whether or not the subscription is subscribed.
    /// </summary>
    public bool IsSubscribed => !Equals(Unsubscribed);
}
