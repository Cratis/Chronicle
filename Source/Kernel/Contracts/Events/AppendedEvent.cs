// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Events;

/// <summary>
/// Represents an event that has been appended to an event log.
/// </summary>
[ProtoContract]
public class AppendedEvent
{
    /// <summary>
    /// Gets the context for the event.
    /// </summary>
    [ProtoMember(1)]
    public EventContext Context { get; set; }

    /// <summary>
    /// The JSON representation content of the event.
    /// </summary>
    [ProtoMember(2)]
    public string Content { get; set; }

    /// <summary>
    /// Gets or sets the original JSON content before any compensations were applied.
    /// Only populated when the event has been compensated.
    /// </summary>
    [ProtoMember(3)]
    public string OriginalContent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the compensations applied to this event, if any.
    /// </summary>
    [ProtoMember(4)]
    public IList<EventCompensation> Compensations { get; set; } = [];
}

