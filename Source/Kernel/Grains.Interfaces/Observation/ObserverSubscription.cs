// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents a subscription to an observer.
/// </summary>
/// <param name="SubscriberType">Type that is subscribing.</param>
/// <param name="EventTypes">Represents the event types for the subscription.</param>
/// <param name="Arguments">Any arguments it passed.</param>
public record ObserverSubscription(Type SubscriberType, IEnumerable<EventType> EventTypes, object Arguments)
{
    /// <summary>
    /// Gets a subscription representing no subscription.
    /// </summary>
    public static readonly ObserverSubscription Unsubscribed = new(typeof(IObserverSubscriber), Enumerable.Empty<EventType>(), null!);

    /// <summary>
    /// Check whether or not the subscription is subscribed.
    /// </summary>
    public bool IsSubscribed => !Equals(Unsubscribed);
}
