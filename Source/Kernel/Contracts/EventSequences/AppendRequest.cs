// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Auditing;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.EventSequences.Concurrency;
using Cratis.Chronicle.Contracts.Identities;

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the payload for appending an event.
/// </summary>
[ProtoContract]
public class AppendRequest : IEventSequenceRequest
{
    /// <inheritdoc/>
    [ProtoMember(1)]
    public string EventStore { get; set; }

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
    /// Gets or sets the event source type.
    /// </summary>
    [ProtoMember(5)]
    public string EventSourceType { get; set; }

    /// <summary>
    /// Gets or sets the event source identifier.
    /// </summary>
    [ProtoMember(6)]
    public string EventSourceId { get; set; }

    /// <summary>
    /// Gets or sets the event stream type.
    /// </summary>
    [ProtoMember(7)]
    public string EventStreamType { get; set; }

    /// <summary>
    /// Gets or sets the event stream identifier.
    /// </summary>
    [ProtoMember(8)]
    public string EventStreamId { get; set; }

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    [ProtoMember(9)]
    public EventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the content of the event - in the form of a JSON payload.
    /// </summary>
    [ProtoMember(10)]
    public string Content { get; set; }

    /// <summary>
    /// Gets or sets the causation.
    /// </summary>
    [ProtoMember(11)]
    public IList<Causation> Causation { get; set; }

    /// <summary>
    /// Gets or sets the caused by.
    /// </summary>
    [ProtoMember(12)]
    public Identity CausedBy { get; set; }

    /// <summary>
    /// Gets or sets the concurrency scope.
    /// </summary>
    [ProtoMember(13)]
    public ConcurrencyScope ConcurrencyScope { get; set; }

    /// <summary>
    /// Gets or sets the tags associated with the event.
    /// </summary>
    [ProtoMember(14, IsRequired = true)]
    public IEnumerable<string> Tags { get; set; } = [];
}
