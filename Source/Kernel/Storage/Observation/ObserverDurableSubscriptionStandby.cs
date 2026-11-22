// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// A client connection that registered the observer with a definition a later registration replaced, kept so that it can take
/// over when the connection that holds the subscription leaves.
/// </summary>
/// <remarks>
/// A standby is a registration, so it carries the definition it registered with, as plain values every storage provider can
/// persist. The record is advisory like the rest of <see cref="ObserverDurableSubscription"/>: a standby whose connection has
/// left is skipped when the observer looks for one to promote.
/// </remarks>
/// <param name="ConnectionId">The identifier of the client connection.</param>
/// <param name="Silo">The parsable address of the silo that terminates the connection.</param>
/// <param name="EventTypes">The event types the connection registered, each as the id, the generation and the tombstone flag joined by plus signs.</param>
/// <param name="IsReplayable">Whether the connection registered the observer as replayable.</param>
/// <param name="Filters">The event filters the connection registered, or null when it has none.</param>
/// <param name="PinsGenerations">Whether the connection registered the observer to receive pinned event generations.</param>
/// <param name="DeliversPages">Whether the connection's subscriber takes pages of events.</param>
public record ObserverDurableSubscriptionStandby(
    string ConnectionId,
    string Silo,
    IReadOnlyList<string> EventTypes,
    bool IsReplayable,
    ObserverDurableSubscriptionFilters? Filters,
    bool PinsGenerations,
    bool DeliversPages);
