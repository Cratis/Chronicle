// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Represents which events an observer reader is scoped to read: either every event type, or an explicit set of them.
/// </summary>
/// <remarks>
/// <para>
/// The scope is carried by every observer job request and every observer key index lookup, so that the decision
/// of what a reader reads is made once, by <see cref="For"/>, and not re-derived from an event type list at each
/// reader. A wildcard scope reads every event type, including types that do not exist yet. An explicit scope reads
/// exactly the types it holds.
/// </para>
/// <para>
/// Today an observer that is not subscribed to all events but has no event types in its definition reads every type,
/// because that is what an empty list has always meant to its readers. <see cref="For"/> maps that observer to
/// <see cref="Wildcard"/> so that nothing changes. Decision 0010 section 8 makes an empty explicit scope read nothing;
/// that is a later change and no scope produced here is an empty explicit one.
/// </para>
/// </remarks>
/// <param name="IsWildcard">Whether the scope reads every event type.</param>
/// <param name="EventTypes">The event types an explicit scope reads. Always empty for a wildcard scope.</param>
public record ObserverReaderScope(bool IsWildcard, IReadOnlyList<EventType> EventTypes)
{
    /// <summary>
    /// Gets the scope that reads every event type.
    /// </summary>
    public static ObserverReaderScope Wildcard => new(true, []);

    /// <summary>
    /// Creates a scope that reads exactly the given event types.
    /// </summary>
    /// <param name="eventTypes">The <see cref="EventType">event types</see> to read.</param>
    /// <returns>The explicit <see cref="ObserverReaderScope"/>.</returns>
    public static ObserverReaderScope Explicit(IEnumerable<EventType> eventTypes) => new(false, eventTypes.ToArray());

    /// <summary>
    /// Computes the scope for an observer. This is the one place the scope is decided.
    /// </summary>
    /// <param name="subscribesToAllEvents">Whether the observer is subscribed to all events.</param>
    /// <param name="definitionEventTypes">The event types of the observer's definition.</param>
    /// <returns>The <see cref="ObserverReaderScope"/> the observer's readers use.</returns>
    public static ObserverReaderScope For(bool subscribesToAllEvents, IEnumerable<EventType> definitionEventTypes)
    {
        var eventTypes = definitionEventTypes.ToArray();
        return subscribesToAllEvents || eventTypes.Length == 0 ? Wildcard : Explicit(eventTypes);
    }

    /// <summary>
    /// Gets the scope a request reads with: the one it carries, or - for a request persisted before requests
    /// carried a scope - the one its event type list always meant.
    /// </summary>
    /// <param name="scope">The scope the request carries, if any.</param>
    /// <param name="requestEventTypes">The event types the request carries.</param>
    /// <returns>The <see cref="ObserverReaderScope"/> to read with.</returns>
    public static ObserverReaderScope ForRequest(ObserverReaderScope? scope, IEnumerable<EventType>? requestEventTypes) =>
        scope ?? For(false, requestEventTypes ?? []);

    /// <summary>
    /// Gets the event types a reader reads, given the types its request asked for.
    /// </summary>
    /// <param name="requestedEventTypes">The event types the request asked for, which may be a narrower set than the scope.</param>
    /// <returns>The event types to read, or null when every event type is to be read.</returns>
    public IReadOnlyList<EventType>? EventTypesToRead(IEnumerable<EventType>? requestedEventTypes)
    {
        var requested = requestedEventTypes?.ToArray() ?? [];
        if (requested.Length != 0)
        {
            return requested;
        }

        return IsWildcard ? null : EventTypes;
    }
}
