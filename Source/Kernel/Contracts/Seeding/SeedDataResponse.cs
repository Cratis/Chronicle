// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Seeding;

/// <summary>
/// Represents the response for getting seed data.
/// </summary>
[ProtoContract]
public class SeedDataResponse
{
    /// <summary>
    /// Gets or sets the collection of seeding entries grouped by event type.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IList<EventTypeSeedEntries> ByEventType { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of seeding entries grouped by event source.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IList<EventSourceSeedEntries> ByEventSource { get; set; } = [];
}
