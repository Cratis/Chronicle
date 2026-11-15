// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents what a peer may read from this event store's outbox.
/// </summary>
/// <remarks>
/// A grant covers either all public event types or a specific set. The ceiling is always the public event types,
/// so no grant, however broad, exposes a private or unspecified event type.
/// </remarks>
/// <param name="AllPublic">Whether the grant covers every public event type.</param>
/// <param name="EventTypes">The specific event types granted when <paramref name="AllPublic"/> is false.</param>
public record FederationGrant(bool AllPublic, IReadOnlySet<EventTypeId> EventTypes)
{
    /// <summary>
    /// Gets a grant covering every public event type.
    /// </summary>
    public static FederationGrant AllPublicEventTypes { get; } = new(true, new HashSet<EventTypeId>());

    /// <summary>
    /// Create a grant for specific event types.
    /// </summary>
    /// <param name="eventTypes">The event types to grant.</param>
    /// <returns>The grant.</returns>
    public static FederationGrant For(params EventTypeId[] eventTypes) => new(false, new HashSet<EventTypeId>(eventTypes));

    /// <summary>
    /// Check whether an event type may be delivered to the peer.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <param name="visibility">The visibility of the event type.</param>
    /// <returns>True when the event type is public and covered by the grant.</returns>
    public bool Permits(EventTypeId eventType, EventTypeVisibility visibility) =>
        visibility == EventTypeVisibility.Public && (AllPublic || EventTypes.Contains(eventType));
}
