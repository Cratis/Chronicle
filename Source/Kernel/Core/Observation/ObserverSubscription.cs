// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents a subscription to an observer.
/// </summary>
/// <param name="ObserverId">The <see cref="ObserverId"/> that the subscription is for.</param>
/// <param name="ObserverKey">The <see cref="ObserverKey"/> that the subscription is for.</param>
/// <param name="EventTypes">Represents the event types for the subscription.</param>
/// <param name="SubscriberType">Type that is subscribing.</param>
/// <param name="SiloAddress">The <see cref="SiloAddress"/> for the subscriber.</param>
/// <param name="Arguments">Optional arguments for the subscriber.</param>
/// <param name="IsReplayable">Whether the observer supports replay scenarios.</param>
/// <param name="Filters">Optional <see cref="ObserverFilters"/> to apply when observing events.</param>
public record ObserverSubscription(
    ObserverId ObserverId,
    ObserverKey ObserverKey,
    IEnumerable<EventType> EventTypes,
    Type SubscriberType,
    SiloAddress SiloAddress,
    object? Arguments = null,
    bool IsReplayable = true,
    ObserverFilters? Filters = null)
{
    /// <summary>
    /// Gets a subscription representing no subscription.
    /// </summary>
    public static readonly ObserverSubscription Unsubscribed = new(ObserverId.Unspecified, ObserverKey.NotSet, [], typeof(NullObserverSubscriber), SiloAddress.Zero, null, true);

    /// <summary>
    /// Gets the connected client instances events fan out to. Client-owned observers hold one
    /// <see cref="ObserverSubscriberTarget"/> per connected client instance, with
    /// <see cref="SiloAddress"/> and <see cref="Arguments"/> mirroring the first target.
    /// Kernel-owned subscriptions have no targets and deliver to <see cref="SiloAddress"/> directly.
    /// </summary>
    public IReadOnlyList<ObserverSubscriberTarget> Targets { get; init; } = [];

    /// <summary>
    /// Check whether the subscription is subscribed.
    /// </summary>
    public bool IsSubscribed => !ObserverId.Equals(ObserverId.Unspecified) && !Equals(Unsubscribed);

    /// <summary>
    /// Gets the <see cref="ObserverSubscriberKey"/> that resolves the subscriber a partition's events are
    /// delivered through.
    /// </summary>
    /// <param name="partition">The <see cref="Key">partition</see> the events belong to.</param>
    /// <param name="siloAddress">The <see cref="SiloAddress"/> of the silo the subscriber should run on.</param>
    /// <returns>The <see cref="ObserverSubscriberKey"/> to resolve the subscriber grain with.</returns>
    /// <remarks>
    /// Every path that delivers to a subscriber - live delivery, catch up and replay - resolves its grain through
    /// this method, so they agree on the identity of the activation handling a partition. A subscriber marked with
    /// <see cref="IUnpartitionedObserverSubscriber"/> gets <see cref="ObserverSubscriberKey.AllPartitions"/> in
    /// place of the partition and therefore a single activation for the whole observer; every other subscriber
    /// keeps the partition and therefore an activation per event source.
    /// </remarks>
    public ObserverSubscriberKey GetSubscriberKeyFor(Key partition, SiloAddress siloAddress)
    {
        EventSourceId eventSourceId = partition?.ToString() ?? EventSourceId.Unspecified;

        if (SubscriberType.IsAssignableTo(typeof(IUnpartitionedObserverSubscriber)))
        {
            eventSourceId = ObserverSubscriberKey.AllPartitions;
        }

        return new(
            ObserverKey.ObserverId,
            ObserverKey.EventStore,
            ObserverKey.Namespace,
            ObserverKey.EventSequenceId,
            eventSourceId,
            siloAddress.ToParsableString());
    }
}
