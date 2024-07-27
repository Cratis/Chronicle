// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the payload for getting events for an event source id and specific event types.
/// </summary>
[ProtoContract]
public class GetForEventSourceIdAndEventTypesRequest
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
    /// Gets or sets the event types to get.
    /// </summary>
    [ProtoMember(5, IsRequired = true)]
    public IList<EventType> EventTypes { get; set; } = [];
}
