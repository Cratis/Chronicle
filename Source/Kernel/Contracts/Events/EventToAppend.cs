// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Events;

/// <summary>
/// Represents an event to append.
/// </summary>
[ProtoContract]
public class EventToAppend
{
    /// <summary>
    /// Gets or sets the event source type.
    /// </summary>
    [ProtoMember(1)]
    public string EventSourceType { get; set; }

    /// <summary>
    /// Gets or sets the event source id.
    /// </summary>
    [ProtoMember(2)]
    public string EventSourceId { get; set; }

    /// <summary>
    /// Gets or sets the event stream type.
    /// </summary>
    [ProtoMember(3)]
    public string EventStreamType { get; set; }

    /// <summary>
    /// Gets or sets the event stream identifier.
    /// </summary>
    [ProtoMember(4)]
    public string EventStreamId { get; set; }

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
    /// Gets or sets the tags associated with the event.
    /// </summary>
    [ProtoMember(7, IsRequired = true)]
    public IEnumerable<string> Tags { get; set; } = [];
}
