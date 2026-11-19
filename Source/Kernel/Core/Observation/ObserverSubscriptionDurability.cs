// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for turning a live <see cref="ObserverSubscription"/> into its durable form (decision 0034).
/// </summary>
public static class ObserverSubscriptionDurability
{
    /// <summary>
    /// Gets the <see cref="ObserverSubscriptionKind"/> a subscriber type belongs to.
    /// </summary>
    /// <param name="subscriberType">The subscriber type of a subscription.</param>
    /// <returns>The kind, or null when the type is not one of the closed set of subscribers that have a durable form.</returns>
    public static ObserverSubscriptionKind? GetSubscriptionKind(this Type subscriberType) => subscriberType switch
    {
        _ when subscriberType.IsAssignableTo(typeof(IReactorObserverSubscriber)) => ObserverSubscriptionKind.ClientReactor,
        _ when subscriberType.IsAssignableTo(typeof(IReducerObserverSubscriber)) => ObserverSubscriptionKind.ClientReducer,
        _ when subscriberType.IsAssignableTo(typeof(IProjectionObserverSubscriber)) ||
               subscriberType.IsAssignableTo(typeof(ICollapsingProjectionObserverSubscriber)) => ObserverSubscriptionKind.Projection,
        _ when subscriberType.IsAssignableTo(typeof(IWebhookObserverSubscriber)) => ObserverSubscriptionKind.Webhook,
        _ when subscriberType.IsAssignableTo(typeof(IEventStoreSubscriptionObserverSubscriber)) => ObserverSubscriptionKind.EventStoreSubscription,
        _ when IsKernelReactor(subscriberType) => ObserverSubscriptionKind.KernelReactor,
        _ => null
    };

    /// <summary>
    /// Gets the durable form of a subscription.
    /// </summary>
    /// <param name="subscription">The live subscription.</param>
    /// <returns>The durable subscription, or null when the observer is not subscribed or its subscriber has no durable form.</returns>
    /// <remarks>
    /// The silo is recorded for the client targets only. A kernel-owned subscription is restored on whichever silo
    /// activates the observer.
    /// </remarks>
    public static ObserverDurableSubscription? ToDurable(this ObserverSubscription subscription)
    {
        if (!subscription.IsSubscribed || subscription.SubscriberType.GetSubscriptionKind() is not { } kind)
        {
            return null;
        }

        return new(
            kind,
            subscription.DeliversPartitionPages || subscription.SubscriberType.IsAssignableTo(typeof(IObserverPageSubscriber)),
            ObserverDurableSubscriptionFilters.From(subscription.Filters),
            [.. subscription.Targets
                .Where(target => target.ConnectedClient is not null)
                .Select(target => new ObserverDurableSubscriptionTarget(
                    target.ConnectedClient!.ConnectionId.Value,
                    target.SiloAddress.ToParsableString()))]);
    }

    static bool IsKernelReactor(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Reactors.Kernel.IReactorObserverSubscriber<>);
}
