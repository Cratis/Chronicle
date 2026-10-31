// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Seeding;

/// <summary>
/// Represents a single entry for event seeding.
/// </summary>
[ProtoContract]
public class SeedingEntry
{
    /// <summary>
    /// Gets or sets the event source identifier.
    /// </summary>
    [ProtoMember(1)]
    public string EventSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event type identifier.
    /// </summary>
    [ProtoMember(2)]
    public string EventTypeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON content of the event.
    /// </summary>
    [ProtoMember(3)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tags associated with the event.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public IList<string> Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets the event source type. Empty means the default type.
    /// </summary>
    [ProtoMember(5)]
    public string EventSourceType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event stream type. Empty means all streams.
    /// </summary>
    [ProtoMember(6)]
    public string EventStreamType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event stream identifier. Empty means the default stream.
    /// </summary>
    [ProtoMember(7)]
    public string EventStreamId { get; set; } = string.Empty;
}
