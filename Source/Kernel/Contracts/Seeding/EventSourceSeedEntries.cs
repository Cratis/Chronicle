// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Seeding;

/// <summary>
/// Represents seed entries grouped by event source.
/// </summary>
[ProtoContract]
public class EventSourceSeedEntries
{
    /// <summary>
    /// Gets or sets the event source identifier.
    /// </summary>
    [ProtoMember(1)]
    public string EventSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of seeding entries for this event source.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IList<SeedingEntry> Entries { get; set; } = [];
}
