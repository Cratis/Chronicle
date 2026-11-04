// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents a group of changes written under one applied event marker slot.
/// </summary>
/// <param name="Update">The classic update the group makes, or null when the group only records its slot.</param>
/// <param name="Slot">The path of the slot the group is guarded by.</param>
/// <param name="Stream">The identity of the event stream the group's event belongs to.</param>
/// <param name="SequenceNumber">The event sequence number of the group's event.</param>
/// <param name="OnProperty">The MongoDB property the group's join is on, or null when the group is the write of the key document itself.</param>
/// <param name="JoinKey">The value the join key is compared against, for a resolved join.</param>
internal sealed record AppliedEventMarkerGroup(UpdateDefinition<BsonDocument>? Update, string Slot, string Stream, long SequenceNumber, string? OnProperty = null, BsonValue? JoinKey = null)
{
    /// <summary>
    /// Gets a value indicating whether the group is a resolved join.
    /// </summary>
    public bool IsResolvedJoin => OnProperty is not null;
}
