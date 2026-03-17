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
    /// Gets or sets the original JSON content before any revisions were applied.
    /// Only populated when the event has been revised.
    /// </summary>
    [ProtoMember(3)]
    public string OriginalContent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the revisions applied to this event, if any.
    /// </summary>
    [ProtoMember(4)]
    public IList<EventRevision> Revisions { get; set; } = [];

    /// <summary>
    /// Gets or sets the content for each generation stored for this event.
    /// Keys are generation numbers; values are the JSON content for that generation.
    /// </summary>
    [ProtoMember(5, IsRequired = true)]
    public IDictionary<int, string> GenerationalContent { get; set; } = new Dictionary<int, string>();
}

