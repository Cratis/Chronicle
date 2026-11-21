// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// The facts of an observer's subscription that are not already stored in its definition, recorded in its state.
/// </summary>
/// <remarks>
/// Decision 0034, point 1. The record is advisory, never authority: whoever restores a subscription from it validates
/// each target first. Event types, event sequence, replayability and generation delivery stay in the observer definition,
/// and the CLR subscriber type is derived from <see cref="Kind"/>.
/// </remarks>
/// <param name="Kind">What serves the subscription.</param>
/// <param name="DeliversPages">Whether the subscription delivers pages of events.</param>
/// <param name="Filters">The event filters of the subscription, or null when it has none.</param>
/// <param name="Targets">The client connections the subscription delivers to. Empty for kernel-owned kinds.</param>
public record ObserverDurableSubscription(
    ObserverSubscriptionKind Kind,
    bool DeliversPages,
    ObserverDurableSubscriptionFilters? Filters,
    IReadOnlyList<ObserverDurableSubscriptionTarget> Targets);
