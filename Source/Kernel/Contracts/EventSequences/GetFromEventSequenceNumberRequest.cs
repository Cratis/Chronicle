// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the payload for getting events from a specific event sequence number and optionally for an event source id and specific event types.
/// </summary>
[ProtoContract]
public class GetFromEventSequenceNumberRequest : IEventSequenceRequest
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
    /// Gets or sets the event sequence number to get events from.
    /// </summary>
    [ProtoMember(4)]
    public ulong FromEventSequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the event sequence number to get events from. This is optional.
    /// </summary>
    /// <remarks>If not set, it will get all events from the <see cref="FromEventSequenceNumber"/>.</remarks>
    [ProtoMember(5)]
    public ulong? ToEventSequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the event source identifier.
    /// </summary>
    [ProtoMember(6)]
    public string? EventSourceId { get; set; }

    /// <summary>
    /// Gets or sets the event types to get.
    /// </summary>
    [ProtoMember(7, IsRequired = true)]
    public IList<EventType> EventTypes { get; set; } = [];
}
