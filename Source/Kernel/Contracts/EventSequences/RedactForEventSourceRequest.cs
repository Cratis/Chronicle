// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Auditing;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Identities;

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the payload for redacting all events for a specific event source.
/// </summary>
[ProtoContract]
public class RedactForEventSourceRequest : IEventSequenceRequest
{
    /// <inheritdoc/>
    [ProtoMember(1)]
    public string EventStore { get; set; } = string.Empty;

    /// <inheritdoc/>
    [ProtoMember(2)]
    public string Namespace { get; set; } = string.Empty;

    /// <inheritdoc/>
    [ProtoMember(3)]
    public string EventSequenceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event source id to redact events for.
    /// </summary>
    [ProtoMember(4)]
    public string EventSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for redacting the events.
    /// </summary>
    [ProtoMember(5)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event types to redact. An empty list means all event types for the event source.
    /// </summary>
    [ProtoMember(6, IsRequired = true)]
    public IList<EventType> EventTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the correlation identifier.
    /// </summary>
    [ProtoMember(7)]
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the causation.
    /// </summary>
    [ProtoMember(8, IsRequired = true)]
    public IList<Causation> Causation { get; set; } = [];

    /// <summary>
    /// Gets or sets the caused by.
    /// </summary>
    [ProtoMember(9, IsRequired = true)]
    public Identity CausedBy { get; set; } = new();
}
