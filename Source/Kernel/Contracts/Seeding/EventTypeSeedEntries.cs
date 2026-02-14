// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Seeding;

/// <summary>
/// Represents seed entries grouped by event type.
/// </summary>
[ProtoContract]
public class EventTypeSeedEntries
{
    /// <summary>
    /// Gets or sets the event type identifier.
    /// </summary>
    [ProtoMember(1)]
    public string EventTypeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of seeding entries for this event type.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IList<SeedingEntry> Entries { get; set; } = [];
}
