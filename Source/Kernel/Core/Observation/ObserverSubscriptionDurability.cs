// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Patterns;
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
        _ when subscriberType.IsAssignableTo(typeof(ICollapsingProjectionObserverSubscriber)) => ObserverSubscriptionKind.CollapsingProjection,
        _ when subscriberType.IsAssignableTo(typeof(IProjectionObserverSubscriber)) => ObserverSubscriptionKind.Projection,
        _ when subscriberType.IsAssignableTo(typeof(IWebhookObserverSubscriber)) => ObserverSubscriptionKind.Webhook,
        _ when subscriberType.IsAssignableTo(typeof(IEventStoreSubscriptionObserverSubscriber)) => ObserverSubscriptionKind.EventStoreSubscription,
        _ when subscriberType.IsAssignableTo(typeof(IPatternCaptureSubscriber)) => ObserverSubscriptionKind.PatternCapture,
        _ when IsKernelReactor(subscriberType) => ObserverSubscriptionKind.KernelReactor,
        _ => null
    };

    /// <summary>
    /// Gets whether an observer of this kind can be released while it is dormant (decision 0034).
    /// </summary>
    /// <param name="kind">The <see cref="ObserverSubscriptionKind"/> of the observer.</param>
    /// <returns>True for client reactors, client reducers, projections and event store subscriptions; false for every other kind, which stay resident.</returns>
    /// <remarks>
    /// What the kind can do, not what is configured: see <see cref="IsReleasedWhenDormant"/>.
    /// </remarks>
    public static bool CanBeReleasedWhenDormant(this ObserverSubscriptionKind kind) => kind is
        ObserverSubscriptionKind.ClientReactor or
        ObserverSubscriptionKind.ClientReducer or
        ObserverSubscriptionKind.Projection or
        ObserverSubscriptionKind.CollapsingProjection or
        ObserverSubscriptionKind.EventStoreSubscription or
        ObserverSubscriptionKind.PatternCapture;

    /// <summary>
    /// Gets whether an observer of this kind is released while dormant under the configuration.
    /// </summary>
    /// <param name="kind">The <see cref="ObserverSubscriptionKind"/> of the observer.</param>
    /// <param name="kinds">The configured <see cref="Observers.EffectiveDormantKinds"/>.</param>
    /// <returns>True when the kind can be released and is listed.</returns>
    public static bool IsReleasedWhenDormant(this ObserverSubscriptionKind kind, IReadOnlyList<ObserverSubscriptionKind> kinds) =>
        kind.CanBeReleasedWhenDormant() && kinds.Contains(kind);

    /// <summary>
    /// Gets whether an observer of this kind is served by connected clients.
    /// </summary>
    /// <param name="kind">The <see cref="ObserverSubscriptionKind"/> of the observer.</param>
    /// <returns>True for client reactors and client reducers.</returns>
    public static bool IsServedByClient(this ObserverSubscriptionKind kind) => kind is
        ObserverSubscriptionKind.ClientReactor or
        ObserverSubscriptionKind.ClientReducer;

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

        var targets = subscription.Targets
            .Where(target => target.ConnectedClient is not null)
            .Select(target => new ObserverDurableSubscriptionTarget(
                target.ConnectedClient!.ConnectionId.Value,
                target.SiloAddress.ToParsableString()));

        return new(
            kind,
            subscription.DeliversPartitionPages || subscription.SubscriberType.IsAssignableTo(typeof(IObserverPageSubscriber)),
            ObserverDurableSubscriptionFilters.From(subscription.Filters),
            [.. targets]);
    }

    static bool IsKernelReactor(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Reactors.Kernel.IReactorObserverSubscriber<>);
}
