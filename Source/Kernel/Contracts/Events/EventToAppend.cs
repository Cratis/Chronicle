// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Events;

/// <summary>
/// Represents an event to append.
/// </summary>
[ProtoContract]
public class EventToAppend
{
    /// <summary>
    /// Gets or sets the event source id.
    /// </summary>
    [ProtoMember(1)]
    public string EventSourceId { get; set; }

    /// <summary>
    /// Gets or sets the event stream type.
    /// </summary>
    [ProtoMember(2)]
    public string EventStreamType { get; set; }

    /// <summary>
    /// Gets or sets the event stream identifier.
    /// </summary>
    [ProtoMember(3)]
    public string EventStreamId { get; set; }

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    [ProtoMember(4)]
    public EventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the content of the event - in the form of a JSON payload.
    /// </summary>
    [ProtoMember(5)]
    public string Content { get; set; }
}
