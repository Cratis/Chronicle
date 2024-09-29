// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Auditing;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Identities;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the payload for appending an event.
/// </summary>
[ProtoContract]
public class AppendRequest : IEventSequenceRequest
{
    /// <inheritdoc/>
    [ProtoMember(1)]
    public string EventStoreName { get; set; }

    /// <inheritdoc/>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <inheritdoc/>
    [ProtoMember(3)]
    public string EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier.
    /// </summary>
    [ProtoMember(4)]
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the event source identifier.
    /// </summary>
    [ProtoMember(5)]
    public string EventSourceId { get; set; }

    /// <summary>
    /// Gets or sets the event stream type.
    /// </summary>
    [ProtoMember(6)]
    public string EventStreamType { get; set; }

    /// <summary>
    /// Gets or sets the event stream identifier.
    /// </summary>
    [ProtoMember(7)]
    public string EventStreamId { get; set; }

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    [ProtoMember(8)]
    public EventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the content of the event - in the form of a JSON payload.
    /// </summary>
    [ProtoMember(9)]
    public string Content { get; set; }

    /// <summary>
    /// Gets or sets the causation.
    /// </summary>
    [ProtoMember(10)]
    public IList<Causation> Causation { get; set; }

    /// <summary>
    /// Gets or sets the caused by.
    /// </summary>
    [ProtoMember(11)]
    public Identity CausedBy { get; set; }
}
