// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the request for getting the tail sequence number.
/// </summary>
[ProtoContract]
public class GetTailSequenceNumberRequest : IEventSequenceRequest
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
    /// Gets or sets the optional event types to filter get for.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public IList<EventType> EventTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the optional event source identifier to filter get for.
    /// </summary>
    [ProtoMember(5)]
    public string? EventSourceId { get; set; }

    /// <summary>
    /// Gets or sets the optional event source type to filter get for.
    /// </summary>
    [ProtoMember(6)]
    public string? EventSourceType { get; set; }

    /// <summary>
    /// Gets or sets the optional event stream id to filter get for.
    /// </summary>
    [ProtoMember(7)]
    public string? EventStreamId { get; set; }

    /// <summary>
    /// Gets or sets the optional event stream id to filter get for.
    /// </summary>
    [ProtoMember(8)]
    public string? EventStreamType { get; set; }
}
