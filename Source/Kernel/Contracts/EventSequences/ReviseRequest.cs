// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Auditing;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Identities;

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the payload for revising an event.
/// </summary>
[ProtoContract]
public class ReviseRequest : IEventSequenceRequest
{
    /// <inheritdoc/>
    [ProtoMember(1, IsRequired = true)]
    public string EventStore { get; set; }

    /// <inheritdoc/>
    [ProtoMember(2, IsRequired = true)]
    public string Namespace { get; set; }

    /// <inheritdoc/>
    [ProtoMember(3, IsRequired = true)]
    public string EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets the sequence number of the event to revise.
    /// </summary>
    [ProtoMember(4)]
    public ulong SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    [ProtoMember(5, IsRequired = true)]
    public EventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the revising content of the event - in the form of a JSON payload.
    /// </summary>
    [ProtoMember(6, IsRequired = true)]
    public string Content { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier.
    /// </summary>
    [ProtoMember(7)]
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the causation.
    /// </summary>
    [ProtoMember(8, IsRequired = true)]
    public IList<Causation> Causation { get; set; }

    /// <summary>
    /// Gets or sets the caused by.
    /// </summary>
    [ProtoMember(9, IsRequired = true)]
    public Identity CausedBy { get; set; }
}
