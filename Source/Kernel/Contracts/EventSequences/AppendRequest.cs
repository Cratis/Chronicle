// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Auditing;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Identities;
using Cratis.Chronicle.Contracts.Primitives;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the payload for appending an event.
/// </summary>
[ProtoContract]
public class AppendRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStoreName { get; set; }

    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the event sequence identifier.
    /// </summary>
    [ProtoMember(3)]
    public string EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets the event source identifier.
    /// </summary>
    [ProtoMember(4)]
    public string EventSourceId { get; set; }

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    [ProtoMember(5)]
    public EventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the content of the event - in the form of a JSON payload.
    /// </summary>
    [ProtoMember(6)]
    public string Content { get; set; }

    /// <summary>
    /// Gets or sets the causation.
    /// </summary>
    [ProtoMember(7)]
    public IEnumerable<Causation> Causation { get; set; }

    /// <summary>
    /// Gets or sets the caused by.
    /// </summary>
    [ProtoMember(8)]
    public Identity CausedBy { get; set; }

    /// <summary>
    /// Gets or sets the valid from.
    /// </summary>
    [ProtoMember(9)]
    public SerializableDateTimeOffset? ValidFrom { get; set; }
}
