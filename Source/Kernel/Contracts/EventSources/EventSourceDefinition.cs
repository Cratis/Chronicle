// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSources;

/// <summary>
/// Represents the definition of an event source, what it is and which event streams it has.
/// </summary>
[ProtoContract]
public class EventSourceDefinition
{
    /// <summary>
    /// Gets or sets the name of the event source, which becomes the event source type of appended events.
    /// </summary>
    [ProtoMember(1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the event source.
    /// </summary>
    [ProtoMember(2)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets who owns the definition.
    /// </summary>
    [ProtoMember(3)]
    public EventSourceOwner Owner { get; set; }

    /// <summary>
    /// Gets or sets the default dimensions taking part in concurrency checks for the event source.
    /// </summary>
    [ProtoMember(4)]
    public ConcurrencyDimensions Concurrency { get; set; }

    /// <summary>
    /// Gets or sets the streams of the event source.
    /// </summary>
    [ProtoMember(5, IsRequired = true)]
    public IList<EventStreamDefinition> Streams { get; set; } = [];
}
