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
    public string EventSourceId { get; set; }

    /// <summary>
    /// Gets or sets the event type identifier.
    /// </summary>
    [ProtoMember(2)]
    public string EventTypeId { get; set; }

    /// <summary>
    /// Gets or sets the JSON content of the event.
    /// </summary>
    [ProtoMember(3)]
    public string Content { get; set; }

    /// <summary>
    /// Gets or sets the tags associated with the event.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public IList<string> Tags { get; set; } = [];
}
