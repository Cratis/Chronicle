// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the criteria that narrow an event sequence query made for presentation purposes.
/// </summary>
/// <remarks>
/// Every member is optional. An empty collection or a null value means "do not narrow on this
/// dimension" rather than "match nothing".
/// </remarks>
[ProtoContract]
public class EventSequenceQueryCriteria
{
    /// <summary>
    /// Gets or sets the event source identifier to narrow to. Null or empty means every event source.
    /// </summary>
    [ProtoMember(1)]
    public string? EventSourceId { get; set; }

    /// <summary>
    /// Gets or sets the event types to narrow to. Empty means every event type.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IList<EventType> EventTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the tags to narrow to - an event matches when it carries any of them. Empty means every event.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<string> Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets the inclusive lower bound on when the event occurred. Null means unbounded.
    /// </summary>
    [ProtoMember(4)]
    public DateTimeOffset? OccurredFrom { get; set; }

    /// <summary>
    /// Gets or sets the exclusive upper bound on when the event occurred. Null means unbounded.
    /// </summary>
    [ProtoMember(5)]
    public DateTimeOffset? OccurredTo { get; set; }

    /// <summary>
    /// Gets or sets the event source type to narrow to. Null or empty means every event source type.
    /// </summary>
    [ProtoMember(6)]
    public string? EventSourceType { get; set; }

    /// <summary>
    /// Gets or sets the event stream type to narrow to. Null or empty means every event stream type.
    /// </summary>
    [ProtoMember(7)]
    public string? EventStreamType { get; set; }

    /// <summary>
    /// Gets or sets the correlation to narrow to. Null means every correlation.
    /// </summary>
    [ProtoMember(8)]
    public Guid? CorrelationId { get; set; }
}
