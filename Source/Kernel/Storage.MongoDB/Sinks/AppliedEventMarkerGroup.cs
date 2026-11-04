// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents a group of changes written under one applied event marker slot.
/// </summary>
/// <param name="Update">The classic update the group makes, or null when the group only records its slot.</param>
/// <param name="Marker">The slot the group is guarded by.</param>
/// <param name="Join">The property and key the group's join is on, or null when the group is the write of the key document itself.</param>
internal sealed record AppliedEventMarkerGroup(UpdateDefinition<BsonDocument>? Update, AppliedEventMarkerSlot Marker, JoinFilterTarget? Join = null)
{
    /// <summary>
    /// Gets the path of the slot the group is guarded by.
    /// </summary>
    public string Slot => Marker.Path;

    /// <summary>
    /// Gets the identity of the event stream the group's event belongs to.
    /// </summary>
    public string Stream => Marker.Stream;

    /// <summary>
    /// Gets the event sequence number of the group's event.
    /// </summary>
    public long SequenceNumber => Marker.SequenceNumber;

    /// <summary>
    /// Gets the MongoDB property the group's join is on, or null when the group is the write of the key document itself.
    /// </summary>
    public string? OnProperty => Join?.Property;

    /// <summary>
    /// Gets the value the join key is compared against, for a resolved join.
    /// </summary>
    public BsonValue? JoinKey => Join?.Value;

    /// <summary>
    /// Gets a value indicating whether the group is a resolved join.
    /// </summary>
    public bool IsResolvedJoin => Join is not null;
}
