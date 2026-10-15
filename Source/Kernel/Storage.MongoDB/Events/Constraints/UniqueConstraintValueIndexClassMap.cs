// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents the mapping for retained unique values.
/// </summary>
public class UniqueConstraintValueIndexClassMap : IBsonClassMapFor<UniqueConstraintValueIndex>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<UniqueConstraintValueIndex> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.Value);
    }
}
