// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents the event a write is made for, when the sink records applied event markers.
/// </summary>
/// <param name="Namespace">The event store namespace of the event.</param>
/// <param name="SourceValue">The event source id of the event.</param>
/// <param name="EventTypeId">The id of the event type.</param>
/// <param name="SequenceNumber">The event sequence number of the event.</param>
/// <param name="RecordOwnSlot">Whether the write to the key document records a slot of its own.</param>
internal sealed record AppliedEventMarkerContext(string Namespace, string SourceValue, string EventTypeId, EventSequenceNumber SequenceNumber, bool RecordOwnSlot)
{
    /// <summary>
    /// Gets the identity of the event stream of the event.
    /// </summary>
    public string OwnStream => AppliedEventMarkers.StreamHash(Namespace, SourceValue);

    /// <summary>
    /// Creates the context of an event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="recordOwnSlot">Whether the write to the key document records a slot of its own.</param>
    /// <returns>The context.</returns>
    public static AppliedEventMarkerContext For(AppendedEvent @event, bool recordOwnSlot) =>
        new(@event.Context.Namespace.Value, @event.Context.EventSourceId.Value, @event.Context.EventType.Id.Value, @event.Context.SequenceNumber, recordOwnSlot);
}
