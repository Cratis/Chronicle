// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Describes a subscription being installed on an <see cref="Observer"/>.
/// </summary>
/// <param name="Type">The <see cref="ObserverType"/> being registered.</param>
/// <param name="EventTypes">The event types being observed.</param>
/// <param name="SiloAddress">The silo the subscriber is on.</param>
/// <param name="SubscriberArgs">The arguments handed to the subscriber, if any.</param>
/// <param name="IsReplayable">Whether the observer can be replayed.</param>
/// <param name="Filters">The optional <see cref="ObserverFilters"/>.</param>
/// <param name="GenerationDelivery">How event generations are delivered.</param>
internal sealed record SubscriptionRequest(
    ObserverType Type,
    IEnumerable<EventType> EventTypes,
    SiloAddress SiloAddress,
    object? SubscriberArgs,
    bool IsReplayable,
    ObserverFilters? Filters = null,
    EventGenerationDelivery GenerationDelivery = EventGenerationDelivery.Compatibility)
{
    /// <summary>
    /// Gets whether the event types are merged into the ones already subscribed.
    /// </summary>
    public bool Additive { get; init; }

    /// <summary>
    /// Gets whether the subscription recovers a stalled one instead of registering anew.
    /// </summary>
    public bool Recovering { get; init; }

    /// <summary>
    /// Gets whether a retired observer is reactivated by the subscription.
    /// </summary>
    public bool ReactivateRetired { get; init; } = true;

    /// <summary>
    /// Gets whether the subscription is made automatically by the kernel rather than explicitly registered.
    /// </summary>
    public bool Automatic { get; init; }

    /// <summary>
    /// Gets whether the subscription may end a quarantine.
    /// </summary>
    public bool LeavesQuarantine => !Automatic && !Recovering;

    /// <summary>
    /// Gets the client instance that may be handed over as one more fan-out target, if the subscription is an ordinary registration for one.
    /// </summary>
    public ConnectedClient? HandOverCandidate => !Additive && !Recovering && !Automatic ? SubscriberArgs as ConnectedClient : null;

    /// <summary>
    /// Gets the request for resubscribing the same definition to another client instance, as the kernel does on its own.
    /// </summary>
    /// <param name="type">The registered <see cref="ObserverType"/>.</param>
    /// <param name="target">The target to subscribe.</param>
    /// <returns>The resubscription request.</returns>
    public SubscriptionRequest ResubscribingTo(ObserverType type, ObserverSubscriberTarget target) =>
        this with
        {
            Type = type,
            SiloAddress = target.SiloAddress,
            SubscriberArgs = target.ConnectedClient,
            Additive = false,
            Recovering = false,
            ReactivateRetired = false,
            Automatic = true
        };
}
