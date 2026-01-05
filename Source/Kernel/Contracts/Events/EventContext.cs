// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Auditing;
using Cratis.Chronicle.Contracts.Identities;

namespace Cratis.Chronicle.Contracts.Events;

/// <summary>
/// Represents the context in which an event exists - typically what it was appended with.
/// </summary>
[ProtoContract]
public class EventContext
{
    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    [ProtoMember(1)]
    public EventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the event source type.
    /// </summary>
    [ProtoMember(2)]
    public string EventSourceType { get; set; }

    /// <summary>
    /// Gets or sets the event source id.
    /// </summary>
    [ProtoMember(3)]
    public string EventSourceId { get; set; }

    /// <summary>
    /// Gets or sets the sequence number of the event as persisted in the event sequence.
    /// </summary>
    [ProtoMember(4)]
    public ulong SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the event stream type.
    /// </summary>
    [ProtoMember(5)]
    public string EventStreamType { get; set; }

    /// <summary>
    /// Gets or sets the event stream id.
    /// </summary>
    [ProtoMember(6)]
    public string EventStreamId { get; set; }

    /// <summary>
    /// Gets or sets when it occurred.
    /// </summary>
    [ProtoMember(7)]
    public SerializableDateTimeOffset Occurred { get; set; }

    /// <summary>
    /// Gets or sets the event store the event belongs to.
    /// </summary>
    [ProtoMember(8)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the namespace id the event belongs to.
    /// </summary>
    [ProtoMember(9)]
    public string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the correlation id for the event.
    /// </summary>
    [ProtoMember(10)]
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets a collection of causation for what caused the event.
    /// </summary>
    [ProtoMember(11)]
    public IList<Causation> Causation { get; set; }

    /// <summary>
    /// Gets or sets a collection of Identities that caused the event.
    /// </summary>
    [ProtoMember(12)]
    public Identity CausedBy { get; set; }

    /// <summary>
    /// Gets or sets the state relevant for the observer observing.
    /// </summary>
    [ProtoMember(13)]
    public EventObservationState ObservationState { get; set; }

    /// <summary>
    /// Gets or sets the tags associated with the event.
    /// </summary>
    [ProtoMember(14)]
    public IEnumerable<string> Tags { get; set; } = [];
}
