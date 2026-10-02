// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSources;

/// <summary>
/// Represents the definition of an event stream belonging to an event source.
/// </summary>
[ProtoContract]
public class EventStreamDefinition
{
    /// <summary>
    /// Gets or sets the name of the stream, which becomes the event stream type of appended events.
    /// </summary>
    [ProtoMember(1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the stream.
    /// </summary>
    [ProtoMember(2)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the dimensions taking part in concurrency checks for the stream.
    /// </summary>
    [ProtoMember(3)]
    public ConcurrencyDimensions Concurrency { get; set; }
}
