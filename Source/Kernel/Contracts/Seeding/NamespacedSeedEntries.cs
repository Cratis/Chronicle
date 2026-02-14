// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Seeding;

/// <summary>
/// Represents seed entries for a specific namespace.
/// </summary>
[ProtoContract]
public class NamespacedSeedEntries
{
    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [ProtoMember(1)]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of seeding entries organized by event type.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IList<EventTypeSeedEntries> ByEventType { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of seeding entries organized by event source.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<EventSourceSeedEntries> ByEventSource { get; set; } = [];
}
