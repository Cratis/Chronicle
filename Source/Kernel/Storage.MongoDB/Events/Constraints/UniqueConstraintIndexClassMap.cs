// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents the mapping for <see cref="UniqueConstraintIndex"/>.
/// </summary>
public class UniqueConstraintIndexClassMap : IBsonClassMapFor<UniqueConstraintIndex>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<UniqueConstraintIndex> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.EventSourceId);
    }
}
