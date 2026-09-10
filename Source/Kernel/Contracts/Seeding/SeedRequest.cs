// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Seeding;

/// <summary>
/// Represents the request for seeding events.
/// </summary>
[ProtoContract]
public class SeedRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of global seeding entries organized by event type.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IList<EventTypeSeedEntries> GlobalByEventType { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of global seeding entries organized by event source.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<EventSourceSeedEntries> GlobalByEventSource { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of namespaced seeding entries.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public IList<NamespacedSeedEntries> NamespacedEntries { get; set; } = [];
}
